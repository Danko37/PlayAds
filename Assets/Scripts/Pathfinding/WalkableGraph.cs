using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// Замена NavMesh (Luna его не тянет): граф путевых точек запекается в редакторе,
/// а рантайм-поиск (A* + сглаживание) — чистая математика без физики.
/// </summary>
public class WalkableGraph : MonoBehaviour
{
    [Header("Область и шаг")]
    [Tooltip("Область заполнения точками (XZ). По Y берётся верх — откуда пускаем луч вниз.")]
    [SerializeField] private Bounds area = new(Vector3.zero, new Vector3(20f, 5f, 20f));
    [Tooltip("Шаг сетки точек по полу.")]
    [SerializeField] private float cellSize = 0.75f;
    [Tooltip("Высота над областью, с которой пускаем луч вниз при бейке.")]
    [SerializeField] private float sampleHeight = 10f;

    [Header("Слои (только для бейка в редакторе)")]
    [Tooltip("Слой пола — по нему кладём точки лучом вниз.")]
    [SerializeField] private LayerMask groundMask;
    [Tooltip("Слой препятствий — их клетки помечаются непроходимыми.")]
    [SerializeField] private LayerMask obstacleMask;
    [Tooltip("Зазор от препятствий: клетка отбраковывается, если препятствие ближе.")]
    [SerializeField] private float agentRadius = 0.4f;

    [Header("Поиск пути")]
    [Tooltip("Срезать лишние углы по карте проходимости (чистая математика, безопасно для Luna).")]
    [SerializeField] private bool smoothPath = true;

    // --- Запечённые данные (сериализуются) ---
    [SerializeField, HideInInspector] private Vector3[] _nodes;
    [SerializeField, HideInInspector] private int[] _neighborOffsets; // длина _nodes.Length + 1
    [SerializeField, HideInInspector] private int[] _neighbors;       // плоский список смежности

    // Булева карта проходимости над областью (XZ). true = есть пол и нет препятствия.
    // По ней в рантайме проверяем линию видимости без физики.
    [SerializeField, HideInInspector] private bool[] _walkable;
    [SerializeField, HideInInspector] private int _cols;
    [SerializeField, HideInInspector] private int _rows;
    [SerializeField, HideInInspector] private float _originX;
    [SerializeField, HideInInspector] private float _originZ;

    // --- Рабочие буферы A* (переиспользуются между вызовами, без аллокаций на клик) ---
    private float[] _g;
    private float[] _f;
    private int[] _cameFrom;
    private bool[] _closed;
    private bool[] _inOpen;
    private List<int> _open;
    private readonly List<int> _pathNodes = new();
    private readonly List<Vector3> _smoothBuffer = new();

    public bool IsBaked => _nodes != null && _nodes.Length > 0;

    private bool HasGrid => _walkable != null && _cols > 0 && _walkable.Length == _cols * _rows;

    // ---------------------------------------------------------------------
    // Рантайм: поиск пути (чистая математика, без физики)
    // ---------------------------------------------------------------------

    /// <summary>Индекс ближайшего узла к точке (по горизонтали). -1, если граф пуст.</summary>
    public int GetNearestNode(Vector3 p) => GetNearestNode(p, false);

    /// <summary>
    /// Ближайший узел к точке (по горизонтали). requireConnected пропускает изолированные
    /// узлы (0 рёбер) — из них A* никуда не уйдёт.
    /// </summary>
    public int GetNearestNode(Vector3 p, bool requireConnected)
    {
        if (!IsBaked)
        {
            return -1;
        }

        var best = -1;
        var bestSqr = float.MaxValue;
        for (var i = 0; i < _nodes.Length; i++)
        {
            if (requireConnected && _neighborOffsets[i + 1] - _neighborOffsets[i] == 0)
            {
                continue;
            }

            var d = _nodes[i] - p;
            d.y = 0f;
            var sqr = d.sqrMagnitude;
            if (sqr < bestSqr)
            {
                bestSqr = sqr;
                best = i;
            }
        }

        return best;
    }

    /// <summary>
    /// Строит путь from→to по графу в <paramref name="result"/>: фактическая точка старта,
    /// узлы графа (A*), фактическая точка цели. Возвращает false, если пути нет.
    /// </summary>
    public bool FindPath(Vector3 from, Vector3 to, List<Vector3> result)
    {
        result.Clear();
        if (!IsBaked)
        {
            return false;
        }

        // Изолированные узлы (0 рёбер) пропускаем — из такого узла A* никуда не уйдёт.
        var start = GetNearestNode(from, requireConnected: true);
        var goal = GetNearestNode(to, requireConnected: true);
        if (start < 0 || goal < 0)
        {
            return false;
        }

        if (!RunAStar(start, goal))
        {
            return false;
        }

        // Восстанавливаем цепочку узлов start→goal.
        _pathNodes.Clear();
        for (var n = goal; n != -1; n = _cameFrom[n])
        {
            _pathNodes.Add(n);
        }
        _pathNodes.Reverse();

        // Итог: фактический старт + позиции узлов + фактическая цель.
        result.Add(from);
        foreach (var n in _pathNodes)
        {
            var pos = _nodes[n];
            // Стартовый узел под ногами героя пропускаем, чтобы не было рывка назад.
            if (result.Count == 1 && HorizontalDistance(pos, from) < cellSize * 0.5f)
            {
                continue;
            }
            result.Add(pos);
        }
        result.Add(to);

        if (smoothPath && HasGrid)
        {
            Smooth(result);
        }

        return result.Count >= 2;
    }

    private bool RunAStar(int start, int goal)
    {
        EnsureBuffers();

        for (var i = 0; i < _nodes.Length; i++)
        {
            _g[i] = float.MaxValue;
            _f[i] = float.MaxValue;
            _cameFrom[i] = -1;
            _closed[i] = false;
            _inOpen[i] = false;
        }
        _open.Clear();

        _g[start] = 0f;
        _f[start] = Heuristic(start, goal);
        _open.Add(start);
        _inOpen[start] = true;

        while (_open.Count > 0)
        {
            // Узел с минимальным f (линейно — узлов немного).
            var ci = 0;
            for (var i = 1; i < _open.Count; i++)
            {
                if (_f[_open[i]] < _f[_open[ci]])
                {
                    ci = i;
                }
            }

            var current = _open[ci];
            if (current == goal)
            {
                return true;
            }

            _open[ci] = _open[_open.Count - 1];
            _open.RemoveAt(_open.Count - 1);
            _inOpen[current] = false;
            _closed[current] = true;

            var edgeFrom = _neighborOffsets[current];
            var edgeTo = _neighborOffsets[current + 1];
            for (var k = edgeFrom; k < edgeTo; k++)
            {
                var next = _neighbors[k];
                if (_closed[next])
                {
                    continue;
                }

                var tentative = _g[current] + HorizontalDistance(_nodes[current], _nodes[next]);
                if (tentative >= _g[next])
                {
                    continue;
                }

                _cameFrom[next] = current;
                _g[next] = tentative;
                _f[next] = tentative + Heuristic(next, goal);
                if (!_inOpen[next])
                {
                    _open.Add(next);
                    _inOpen[next] = true;
                }
            }
        }

        return false;
    }

    private void EnsureBuffers()
    {
        var n = _nodes.Length;
        if (_g != null && _g.Length == n)
        {
            return;
        }

        _g = new float[n];
        _f = new float[n];
        _cameFrom = new int[n];
        _closed = new bool[n];
        _inOpen = new bool[n];
        _open = new List<int>(n);
    }

    private float Heuristic(int a, int b) => HorizontalDistance(_nodes[a], _nodes[b]);

    private static float HorizontalDistance(Vector3 a, Vector3 b)
    {
        a.y = 0f;
        b.y = 0f;
        return Vector3.Distance(a, b);
    }

    // ---------------------------------------------------------------------
    // Сглаживание (жадный проход по линии видимости) — по карте, без физики
    // ---------------------------------------------------------------------

    private void Smooth(List<Vector3> path)
    {
        if (path.Count < 3)
        {
            return;
        }

        _smoothBuffer.Clear();
        _smoothBuffer.Add(path[0]);

        var anchor = 0;
        for (var i = 2; i < path.Count; i++)
        {
            // От опорной точки не видно path[i] — фиксируем предыдущую как новый угол.
            if (IsBlocked(path[anchor], path[i]))
            {
                _smoothBuffer.Add(path[i - 1]);
                anchor = i - 1;
            }
        }
        _smoothBuffer.Add(path[path.Count - 1]);

        path.Clear();
        path.AddRange(_smoothBuffer);
    }

    /// <summary>
    /// true, если прямой отрезок a→b проходит через непроходимую клетку (пустота/вода или
    /// препятствие). Проверка по запечённой карте — чистая математика, без физики (Luna-safe).
    /// </summary>
    private bool IsBlocked(Vector3 a, Vector3 b)
    {
        var dist = HorizontalDistance(a, b);
        var steps = Mathf.Max(1, Mathf.CeilToInt(dist / (cellSize * 0.5f)));

        for (var s = 0; s <= steps; s++)
        {
            var t = (float)s / steps;
            var p = Vector3.Lerp(a, b, t);
            if (!CellWalkable(p.x, p.z))
            {
                return true;
            }
        }

        return false;
    }

    private bool CellWalkable(float worldX, float worldZ)
    {
        var ix = Mathf.FloorToInt((worldX - _originX) / cellSize);
        var iz = Mathf.FloorToInt((worldZ - _originZ) / cellSize);
        if (ix < 0 || ix >= _cols || iz < 0 || iz >= _rows)
        {
            return false;
        }

        return _walkable[iz * _cols + ix];
    }

#if UNITY_EDITOR
    // ---------------------------------------------------------------------
    // Бейк (ТОЛЬКО редактор): раскладываем точки на полу, строим карту и рёбра.
    // Вся физика живёт здесь и в билд под Luna не попадает.
    // ---------------------------------------------------------------------

    [ContextMenu("Bake")]
    public void Bake()
    {
        var top = area.max.y + sampleHeight;          // старт луча заведомо выше пола
        var rayLen = sampleHeight + area.size.y + 1f; // хватит, чтобы достать пол

        _originX = area.min.x;
        _originZ = area.min.z;
        _cols = Mathf.FloorToInt(area.size.x / cellSize) + 1;
        _rows = Mathf.FloorToInt(area.size.z / cellSize) + 1;
        _walkable = new bool[_cols * _rows];

        var nodes = new List<Vector3>();

        // Заполняем область сеткой; клетка проходима (и рождает узел) там, где луч попал
        // в пол и рядом нет препятствия. Так узлы и карта повторяют форму невыпуклого пола.
        for (var iz = 0; iz < _rows; iz++)
        {
            for (var ix = 0; ix < _cols; ix++)
            {
                var x = _originX + ix * cellSize;
                var z = _originZ + iz * cellSize;
                var origin = new Vector3(x, top, z);

                if (!Physics.Raycast(origin, Vector3.down, out var hit, rayLen, groundMask))
                {
                    continue;
                }

                if (Physics.CheckSphere(hit.point + Vector3.up * agentRadius, agentRadius, obstacleMask))
                {
                    continue;
                }

                _walkable[iz * _cols + ix] = true;
                nodes.Add(hit.point);
            }
        }

        BuildEdges(nodes);
        Debug.Log($"[WalkableGraph] Запечено узлов: {_nodes.Length}, рёбер (полусумма): {_neighbors.Length / 2}, клеток карты: {_cols}x{_rows}");

        if (!Application.isPlaying)
        {
            UnityEditor.EditorUtility.SetDirty(this);
            UnityEditor.SceneManagement.EditorSceneManager.MarkSceneDirty(gameObject.scene);
        }
    }

    private void BuildEdges(List<Vector3> nodes)
    {
        var count = nodes.Count;
        var connectRadius = cellSize * 1.5f; // 8-связность: прямые и диагональные соседи
        var radiusSqr = connectRadius * connectRadius;

        var offsets = new int[count + 1];
        var flat = new List<int>(count * 8);

        for (var i = 0; i < count; i++)
        {
            offsets[i] = flat.Count;
            for (var j = 0; j < count; j++)
            {
                if (i == j)
                {
                    continue;
                }

                var d = nodes[j] - nodes[i];
                d.y = 0f;
                if (d.sqrMagnitude > radiusSqr)
                {
                    continue;
                }

                // Ортогональных соседей связываем безусловно (воды между смежными клетками нет);
                // проверка «под отрезком есть пол» нужна только диагоналям (могут срезать угол).
                var orthogonal = Mathf.Abs(d.x) < cellSize * 0.5f || Mathf.Abs(d.z) < cellSize * 0.5f;
                if (!orthogonal && IsBlocked(nodes[i], nodes[j]))
                {
                    continue;
                }

                flat.Add(j);
            }
        }
        offsets[count] = flat.Count;

        _nodes = nodes.ToArray();
        _neighborOffsets = offsets;
        _neighbors = flat.ToArray();

        // Буферы A* пересоздадутся под новый размер при следующем поиске.
        _g = null;
    }

    // ---------------------------------------------------------------------
    // Визуализация (редактор)
    // ---------------------------------------------------------------------

    private void OnDrawGizmosSelected()
    {
        if (!IsBaked)
        {
            Gizmos.color = new Color(0f, 1f, 1f, 0.25f);
            Gizmos.DrawWireCube(area.center, area.size);
            return;
        }

        Gizmos.color = new Color(0.2f, 0.9f, 1f, 0.9f);
        foreach (var n in _nodes)
        {
            Gizmos.DrawSphere(n, cellSize * 0.12f);
        }

        Gizmos.color = new Color(0.2f, 0.9f, 1f, 0.25f);
        for (var i = 0; i < _nodes.Length; i++)
        {
            var edgeFrom = _neighborOffsets[i];
            var edgeTo = _neighborOffsets[i + 1];
            for (var k = edgeFrom; k < edgeTo; k++)
            {
                var j = _neighbors[k];
                if (j > i) // каждое ребро рисуем один раз
                {
                    Gizmos.DrawLine(_nodes[i], _nodes[j]);
                }
            }
        }
    }
#endif
}

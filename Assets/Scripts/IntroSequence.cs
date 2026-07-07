using System.Collections;
using System.Collections.Generic;
using Characters;
using DG.Tweening;
using UnityEngine;
using UnityEngine.AI;

public class IntroSequence : MonoBehaviour
{
    [Header("Ссылки")]
    [SerializeField] private Camera sceneCamera;
    [Tooltip("Следящая камера — отключается на время катсцены, включается в конце.")]
    [SerializeField] private CameraScript cameraScript;
    [SerializeField] private HeroView heroView;
    [SerializeField] private Transform enemy;
    [SerializeField] private Transform chest;
    [SerializeField] private EventsSO events;

    [Header("Катсцена")]
    [Tooltip("Стартовое (нижнее) положение камеры — показывает море.")]
    [SerializeField] private Transform cameraStartPoint;
    [Tooltip("Точка появления героя сверху (вне кадра), откуда он прибегает.")]
    [SerializeField] private Transform heroSpawnPoint;
    [Tooltip("Оффсет следящей камеры (как в CameraScript) — для финальной точки пана.")]
    [SerializeField] private Vector3 cameraFollowOffset = new(0f, 11f, 0f);
    [SerializeField] private float cameraPanDuration = 3f;
    [Tooltip("Доля пана камеры, после которой герой начинает вбегать (0..1).")]
    [SerializeField, Range(0f, 1f)] private float heroRunStartFraction = 0.6f;
    [SerializeField] private float heroRunDuration = 1.1f;

    [Header("Туториал")]
    [Tooltip("Жёлтый круг (UI Image). Изначально выключен.")]
    [SerializeField] private RectTransform tutorialCircle;
    [SerializeField] private GameObject pointPrefab;
    [SerializeField] private Transform pathPointsParent;
    [SerializeField] private float dotSpacing = 0.9f;
    [SerializeField, Range(0f, 1f)] private float dotAlpha = 0.5f;
    [Tooltip("За сколько секунд круг проезжает весь маршрут герой→враг→сундук.")]
    [SerializeField] private float circleTravelDuration = 3f;

    private readonly List<PathDot> _dots = new();
    private Canvas _canvas;

    private void OnEnable()
    {
        if (events != null)
            events.OnChestOpenStart += HideCircle;
    }

    private void OnDisable()
    {
        if (events != null)
            events.OnChestOpenStart -= HideCircle;
    }

    private void Start()
    {
        if (tutorialCircle != null)
        {
            _canvas = tutorialCircle.GetComponentInParent<Canvas>();
            tutorialCircle.gameObject.SetActive(false);
        }

        StartCoroutine(Run());
    }

    private IEnumerator Run()
    {
        // Стартовая (игровая) позиция героя — куда он прибежит и вокруг чего встанет камера.
        Vector3 heroStart = heroView.transform.position;

        yield return Cutscene(heroStart);
        yield return Tutorial(heroStart);

        // Управление возвращается игроку.
        events.RaiseIntroFinished();
    }

    // --- Катсцена: пан камеры + вбегание героя ---
    private IEnumerator Cutscene(Vector3 heroStart)
    {
        // Камерой управляем сами: следящий скрипт мешал бы пану.
        if (cameraScript != null)
            cameraScript.enabled = false;

        // Агент навмеша не должен «тянуть» героя, пока мы двигаем его трансформ вручную.
        if (heroView.NavMeshAgent != null)
            heroView.NavMeshAgent.enabled = false;

        sceneCamera.transform.position = cameraStartPoint.position;
        heroView.transform.position = heroSpawnPoint.position;

        Vector3 cameraEnd = heroStart + cameraFollowOffset;
        sceneCamera.transform
            .DOMove(cameraEnd, cameraPanDuration)
            .SetEase(Ease.OutSine);

        // Ждём, пока камера пройдёт часть пути, и запускаем вбегание героя.
        yield return new WaitForSeconds(cameraPanDuration * heroRunStartFraction);

        heroView.SetRun(true);
        heroView.FaceDirection(heroStart - heroView.transform.position);
        heroView.transform
            .DOMove(heroStart, heroRunDuration)
            .SetEase(Ease.Linear);

        // Дожидаемся, пока завершатся оба движения (пан камеры и вбегание героя):
        // остаток пана = D*(1-f), вбегание = R.
        float remainingPan = cameraPanDuration * (1f - heroRunStartFraction);
        yield return new WaitForSeconds(Mathf.Max(remainingPan, heroRunDuration));

        heroView.SetRun(false);
        heroView.ResetFacing();

        // Камера уже в heroStart+offset — включение следящего скрипта проходит без скачка.
        if (cameraScript != null)
            cameraScript.enabled = true;
    }

    // --- Туториал: круг по маршруту + точки навмеша ---
    private IEnumerator Tutorial(Vector3 heroStart)
    {
        var path = BuildTutorialPath(heroStart);
        if (path.Count < 2)
            yield break;

        float total = PathLength(path);
        if (total < 0.0001f)
            yield break;

        if (tutorialCircle != null)
            tutorialCircle.gameObject.SetActive(true);

        float nextDotAt = dotSpacing;
        float t = 0f;

        while (t < circleTravelDuration)
        {
            t += Time.deltaTime;
            float s = Mathf.Clamp01(t / circleTravelDuration) * total;

            PositionCircle(PointAtDistance(path, s));

            // Точки навмеша выкладываются от героя до текущей позиции круга.
            while (nextDotAt <= s)
            {
                SpawnDot(PointAtDistance(path, nextDotAt));
                nextDotAt += dotSpacing;
            }

            yield return null;
        }

        // Круг точно на сундуке; точки-подсказки убираем.
        PositionCircle(path[path.Count - 1]);
        ClearDots();
    }

    /// <summary>
    /// Маршрут герой→враг→сундук по навмешу: две CalculatePath, склеенные в одну ломаную.
    /// Точки-цели снимаются на навмеш (SamplePosition), т.к. объекты стоят над землёй.
    /// </summary>
    private List<Vector3> BuildTutorialPath(Vector3 heroStart)
    {
        var result = new List<Vector3>();

        Vector3 a = SampleOnNavMesh(heroStart);
        Vector3 b = SampleOnNavMesh(enemy.position);
        Vector3 c = SampleOnNavMesh(chest.position);

        AppendSegment(a, b, result, includeStart: true);
        AppendSegment(b, c, result, includeStart: false);

        return result;
    }

    private static void AppendSegment(Vector3 from, Vector3 to, List<Vector3> outList, bool includeStart)
    {
        var np = new NavMeshPath();
        if (!NavMesh.CalculatePath(from, to, NavMesh.AllAreas, np))
        {
            // Нет пути по навмешу — берём прямой отрезок как запасной вариант.
            if (includeStart) outList.Add(from);
            outList.Add(to);
            return;
        }

        var corners = np.corners;
        for (int i = includeStart ? 0 : 1; i < corners.Length; i++)
            outList.Add(corners[i]);
    }

    private static Vector3 SampleOnNavMesh(Vector3 p)
    {
        return NavMesh.SamplePosition(p, out var hit, 3f, NavMesh.AllAreas) ? hit.position : p;
    }

    private void SpawnDot(Vector3 worldPos)
    {
        if (pointPrefab == null)
            return;

        var obj = Instantiate(pointPrefab, worldPos, Quaternion.identity, pathPointsParent);
        var dot = obj.GetComponent<PathDot>();

        dot.transform.rotation = Quaternion.Euler(dot.InitRotation);
        dot.Show(0);
        dot.SetAlpha(dotAlpha);

        _dots.Add(dot);
    }

    private void ClearDots()
    {
        foreach (var dot in _dots)
        {
            if (dot != null)
                Destroy(dot.gameObject);
        }

        _dots.Clear();
    }

    // Ставит UI-круг в экранную проекцию мировой точки (камера в туториале статична).
    private void PositionCircle(Vector3 worldPos)
    {
        if (tutorialCircle == null)
            return;

        Vector3 screen = sceneCamera.WorldToScreenPoint(worldPos);

        var parent = tutorialCircle.parent as RectTransform;
        if (parent == null)
        {
            tutorialCircle.position = screen;
            return;
        }

        // Для Screen Space - Overlay камера канваса = null, для Camera — её камера.
        Camera canvasCam = (_canvas != null && _canvas.renderMode != RenderMode.ScreenSpaceOverlay)
            ? _canvas.worldCamera
            : null;

        if (RectTransformUtility.ScreenPointToLocalPointInRectangle(parent, screen, canvasCam, out var local))
            tutorialCircle.anchoredPosition = local;
    }

    private void HideCircle()
    {
        if (tutorialCircle != null)
            tutorialCircle.gameObject.SetActive(false);
    }

    private static float PathLength(List<Vector3> path)
    {
        float len = 0f;
        for (int i = 0; i < path.Count - 1; i++)
            len += Vector3.Distance(path[i], path[i + 1]);
        return len;
    }

    // Точка на ломаной на дистанции distance от начала.
    private static Vector3 PointAtDistance(List<Vector3> path, float distance)
    {
        if (path.Count == 0)
            return Vector3.zero;
        if (distance <= 0f)
            return path[0];

        float traveled = 0f;
        for (int i = 0; i < path.Count - 1; i++)
        {
            float segLen = Vector3.Distance(path[i], path[i + 1]);
            if (segLen < 0.0001f)
                continue;

            if (traveled + segLen >= distance)
            {
                float f = (distance - traveled) / segLen;
                return Vector3.Lerp(path[i], path[i + 1], f);
            }

            traveled += segLen;
        }

        return path[path.Count - 1];
    }
}

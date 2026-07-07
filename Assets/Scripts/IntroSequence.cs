using System.Collections;
using System.Collections.Generic;
using Characters;
using DG.Tweening;
using UnityEngine;
using UnityEngine.AI;
using UnityEngine.InputSystem;
using UnityEngine.UI;

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
    [Tooltip("Зазор в конце пути, где точки не ставятся — чтобы не лезли под круг.")]
    [SerializeField] private float dotEndClearance = 0.6f;
    [SerializeField, Range(0f, 1f)] private float dotAlpha = 0.5f;
    [Tooltip("За сколько секунд круг проезжает весь маршрут герой→враг→сундук.")]
    [SerializeField] private float circleTravelDuration = 3f;

    [Header("Круг-подсказка на сундуке")]
    [Tooltip("Во что упирается скейл при пульсации (доля от оригинала).")]
    [SerializeField, Range(0.1f, 1f)] private float circlePulseScale = 0.8f;
    [Tooltip("Длительность одной фазы пульса (в сторону и обратно).")]
    [SerializeField] private float circlePulseDuration = 0.3f;
    [Tooltip("Пауза между циклами пульса.")]
    [SerializeField] private float circlePulsePause = 0.5f;
    [Tooltip("До какого размера круг разрастается при исчезновении (доля от оригинала).")]
    [SerializeField] private float circleDismissScale = 1.3f;
    [SerializeField] private float circleDismissDuration = 0.3f;

    private readonly List<PathDot> _dots = new();
    private Canvas _canvas;

    private Image _circleImage;
    private Vector3 _circleBaseScale;
    private Sequence _pulseSeq;
    private bool _awaitingChestClick;
    // Мировой объект, над которым «висит» круг-подсказка — держим над ним каждый кадр,
    // т.к. в фазе врага камера едет за героем. null — круг не привязан (фазу A ведёт корутина).
    private Transform _circleTarget;

    // Переиспользуемые буферы, чтобы не аллоцировать каждый кадр при перестройке пути.
    private readonly List<Vector3> _circlePath = new();
    private readonly List<Vector3> _samples = new();
    private NavMeshPath _navPath;

    private void OnEnable()
    {
        if (events == null)
            return;

        // Дошли до сундука — круг перескакивает на врага; начался бой — круг исчезает.
        events.OnChestOpenStart += ShowEnemyHint;
        events.OnBattleStart += DismissCircle;
    }

    private void OnDisable()
    {
        if (events == null)
            return;

        events.OnChestOpenStart -= ShowEnemyHint;
        events.OnBattleStart -= DismissCircle;
    }

    private void Start()
    {
        if (tutorialCircle != null)
        {
            _canvas = tutorialCircle.GetComponentInParent<Canvas>();
            _circleBaseScale = tutorialCircle.localScale;

            // Фейд при исчезновении делаем через альфу Image.color. raycastTarget = false —
            // чтобы клик по кругу проходил в геймплей (герой уходит к сундуку), а сам клик
            // по кругу мы ловим вручную в Update.
            _circleImage = tutorialCircle.GetComponent<Image>();
            if (_circleImage != null)
                _circleImage.raycastTarget = false;

            tutorialCircle.gameObject.SetActive(false);
        }

        StartCoroutine(Run());
    }

    private void Update()
    {
        if (tutorialCircle == null || !tutorialCircle.gameObject.activeSelf)
            return;

        // Круг-подсказка держится над своим мировым объектом (камера следует за героем).
        if (_circleTarget != null)
            PositionCircle(_circleTarget.position);

        // Фаза сундука: ждём клик именно по кругу — тогда круг исчезает.
        if (!_awaitingChestClick)
            return;

        var mouse = Mouse.current;
        if (mouse == null || !mouse.leftButton.wasPressedThisFrame)
            return;

        var mp = mouse.position.ReadValue();
        if (RectTransformUtility.RectangleContainsScreenPoint(tutorialCircle, mp, CanvasCamera()))
            DismissCircle();
    }

    private IEnumerator Run()
    {
        // Стартовая (игровая) позиция героя — куда он прибежит и вокруг чего встанет камера.
        var heroStart = heroView.transform.position;

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

        var cameraEnd = heroStart + cameraFollowOffset;
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
        var remainingPan = cameraPanDuration * (1f - heroRunStartFraction);
        yield return new WaitForSeconds(Mathf.Max(remainingPan, heroRunDuration));

        heroView.SetRun(false);
        heroView.ResetFacing();

        // Камера уже в heroStart+offset — включение следящего скрипта проходит без скачка.
        if (cameraScript != null)
            cameraScript.enabled = true;
    }

    // --- Туториал: круг едет герой→сундук, точки — живой путь герой→круг ---
    private IEnumerator Tutorial(Vector3 heroStart)
    {
        // Маршрут, по которому движется сам круг (герой→сундук).
        var route = BuildTutorialPath(heroStart);
        if (route.Count < 2)
            yield break;

        var total = PathLength(route);
        if (total < 0.0001f)
            yield break;

        var heroNav = SampleOnNavMesh(heroStart);

        if (tutorialCircle != null)
            tutorialCircle.gameObject.SetActive(true);

        var t = 0f;
        while (t < circleTravelDuration)
        {
            t += Time.deltaTime;
            var s = Mathf.Clamp01(t / circleTravelDuration) * total;
            var circlePos = PointAtDistance(route, s);

            PositionCircle(circlePos);

            // Точки — кратчайший путь по навмешу от героя до ТЕКУЩЕГО положения круга.
            // Каждый кадр пересчитываем и перестраиваем (точки репозиционируются, не мерцают).
            ComputeNavPath(heroNav, circlePos, _circlePath);
            RebuildDots(_circlePath);

            yield return null;
        }

        // Круг доехал; точки-подсказки убираем и оставляем круг пульсировать над сундуком.
        ClearDots();
        ShowCircleOver(chest, awaitClick: true);
    }

    /// <summary>
    /// Кратчайший путь по навмешу from→to в переиспользуемый список (fallback — прямой отрезок).
    /// </summary>
    private void ComputeNavPath(Vector3 from, Vector3 to, List<Vector3> outList)
    {
        outList.Clear();
        _navPath ??= new NavMeshPath();

        if (NavMesh.CalculatePath(from, to, NavMesh.AllAreas, _navPath) && _navPath.corners.Length >= 2)
            outList.AddRange(_navPath.corners);
        else
        {
            outList.Add(from);
            outList.Add(to);
        }
    }

    /// <summary>
    /// Раскладывает точки равномерно вдоль пути (шаг dotSpacing, зазор в конце под круг),
    /// переиспользуя уже существующие точки: лишние удаляет, недостающие создаёт. Так путь
    /// плавно перестраивается за кругом без пере-инстанцирования и мерцания.
    /// </summary>
    private void RebuildDots(List<Vector3> path)
    {
        _samples.Clear();

        var total = PathLength(path);
        var limit = total - dotEndClearance;
        for (var d = dotSpacing; d <= limit; d += dotSpacing)
            _samples.Add(PointAtDistance(path, d));

        for (var i = 0; i < _samples.Count; i++)
        {
            if (i < _dots.Count)
                _dots[i].transform.position = _samples[i];
            else
                CreateDot(_samples[i]);
        }

        for (var i = _dots.Count - 1; i >= _samples.Count; i--)
        {
            if (_dots[i] != null)
                Destroy(_dots[i].gameObject);
            _dots.RemoveAt(i);
        }
    }

    /// <summary>
    /// Показывает круг-подсказку над мировым объектом target и запускает пульсацию. Круг
    /// держится над объектом каждый кадр (Update), т.к. камера следует за героем.
    /// awaitClick=true — круг исчезнет по клику по нему (фаза сундука); false — по внешнему
    /// событию (фаза врага: исчезает при начале боя, OnBattleStart).
    /// </summary>
    private void ShowCircleOver(Transform target, bool awaitClick)
    {
        if (tutorialCircle == null || target == null)
            return;

        // Снимаем твины прошлой фазы (в т.ч. незавершённое исчезновение) и восстанавливаем вид.
        tutorialCircle.DOKill();
        if (_circleImage != null)
        {
            _circleImage.DOKill();
            SetCircleAlpha(1f);
        }

        tutorialCircle.localScale = _circleBaseScale;
        tutorialCircle.gameObject.SetActive(true);

        _circleTarget = target;
        _awaitingChestClick = awaitClick;

        PositionCircle(target.position);
        BeginPulse();
    }

    // Пульс скейлом: оригинал ⇄ circlePulseScale с паузой, зациклено.
    private void BeginPulse()
    {
        _pulseSeq?.Kill();
        _pulseSeq = DOTween.Sequence()
            .Append(tutorialCircle.DOScale(_circleBaseScale * circlePulseScale, circlePulseDuration).SetEase(Ease.InOutSine))
            .Append(tutorialCircle.DOScale(_circleBaseScale, circlePulseDuration).SetEase(Ease.InOutSine))
            .AppendInterval(circlePulsePause)
            .SetLoops(-1);
    }

    // Дошли до сундука — та же подсказка перескакивает на врага (исчезнет при начале боя).
    private void ShowEnemyHint() => ShowCircleOver(enemy, awaitClick: false);

    /// <summary>
    /// Круг исчезает: гаснет по альфе (Image.color) и одновременно разрастается до
    /// circleDismissScale. Клик по сундуку (Update) и начало боя (OnBattleStart) ведут сюда.
    /// </summary>
    private void DismissCircle()
    {
        if (tutorialCircle == null || !tutorialCircle.gameObject.activeSelf)
            return;

        _awaitingChestClick = false;
        _circleTarget = null;
        _pulseSeq?.Kill();
        _pulseSeq = null;

        var seq = DOTween.Sequence();
        seq.Append(tutorialCircle.DOScale(_circleBaseScale * circleDismissScale, circleDismissDuration).SetEase(Ease.OutQuad));
        if (_circleImage != null)
            seq.Join(_circleImage.DOFade(0f, circleDismissDuration));
        seq.OnComplete(() =>
        {
            if (tutorialCircle != null)
                tutorialCircle.gameObject.SetActive(false);
        });
    }

    // Ставит альфу круга через альфа-канал Image.color.
    private void SetCircleAlpha(float alpha)
    {
        var c = _circleImage.color;
        c.a = alpha;
        _circleImage.color = c;
    }

    // Камера канваса: null для Overlay, worldCamera для Screen Space - Camera.
    private Camera CanvasCamera()
    {
        return (_canvas != null && _canvas.renderMode != RenderMode.ScreenSpaceOverlay)
            ? _canvas.worldCamera
            : null;
    }

    /// <summary>
    /// Маршрут герой→сундук по навмешу. Точки-цели снимаются на навмеш (SamplePosition),
    /// т.к. объекты стоят над землёй.
    /// </summary>
    private List<Vector3> BuildTutorialPath(Vector3 heroStart)
    {
        var result = new List<Vector3>();

        var a = SampleOnNavMesh(heroStart);
        var c = SampleOnNavMesh(chest.position);

        AppendSegment(a, c, result, includeStart: true);

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
        for (var i = includeStart ? 0 : 1; i < corners.Length; i++)
            outList.Add(corners[i]);
    }

    private static Vector3 SampleOnNavMesh(Vector3 p)
    {
        return NavMesh.SamplePosition(p, out var hit, 3f, NavMesh.AllAreas) ? hit.position : p;
    }

    private void CreateDot(Vector3 worldPos)
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

        var screen = sceneCamera.WorldToScreenPoint(worldPos);

        var parent = tutorialCircle.parent as RectTransform;
        if (parent == null)
        {
            tutorialCircle.position = screen;
            return;
        }

        if (RectTransformUtility.ScreenPointToLocalPointInRectangle(parent, screen, CanvasCamera(), out var local))
            tutorialCircle.anchoredPosition = local;
    }

    private static float PathLength(List<Vector3> path)
    {
        var len = 0f;
        for (var i = 0; i < path.Count - 1; i++)
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

        var traveled = 0f;
        for (var i = 0; i < path.Count - 1; i++)
        {
            var segLen = Vector3.Distance(path[i], path[i + 1]);
            if (segLen < 0.0001f)
                continue;

            if (traveled + segLen >= distance)
            {
                var f = (distance - traveled) / segLen;
                return Vector3.Lerp(path[i], path[i + 1], f);
            }

            traveled += segLen;
        }

        return path[path.Count - 1];
    }
}

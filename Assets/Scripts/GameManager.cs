using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.SceneManagement;
using System.Collections;
using System.Collections.Generic;
using Characters;
using DG.Tweening;

public enum PlayerState
{
    Intro,
    Idle,
    Moving,
    Fighting,
    Dead,
    Win
}

public class GameManager : MonoBehaviour
{
    [Header("References")]
    [SerializeField] 
    private Camera mainCamera;
    [SerializeField] 
    private Transform pathPointsParent;
    
    [SerializeField]
    private EventsSO events;
    
    [Header("Settings")]
    [SerializeField] private 
        GameObject pointPrefab;
    [Tooltip("Префаб круга в конце пути (маркер точки назначения).")]
    [SerializeField] private 
        GameObject destinationMarkerPrefab;
    [Tooltip("Слои интерактивных сущностей (враг/сундук) для детекции клика.")]
    [SerializeField] private 
        LayerMask interactableMask;
    
    [SerializeField] 
    private float moveSpeed = 8f;
    [SerializeField] 
    private float dotSpacing = 50f;
    [SerializeField] 
    private float dotFadeDistance = 0.6f;
    [Tooltip("Зазор в конце пути, где точки не ставятся — чтобы не накладывались на круг.")]
    [SerializeField] 
    private float dotEndClearance = 0.6f;
    [Tooltip("Базовая (максимальная) прозрачность точек маршрута.")]
    [SerializeField, Range(0f, 1f)] 
    private float maxDotAlpha = 0.5f;
    [SerializeField] 
    private HeroView heroView;

    [Tooltip("Длительность анимации удара героя (сек) до возврата в Idle.")]
    [SerializeField] private float 
        attackDuration = 1.1f;

    private Coroutine moveCoroutine;

    private readonly List<Vector3> currentPath = new();
    private readonly List<PathDot> activeDots = new();

    private GameObject destinationMarker;

    private PathDotPool dotPool;

    [Tooltip("Граф проходимых точек — источник маршрута (замена NavMesh).")]
    [SerializeField]
    private WalkableGraph graph;

    // Игра стартует в Intro: ввод заблокирован с первого кадра, пока IntroSequence
    // (катсцена + туториал) не поднимет OnIntroFinished.
    public PlayerState PlayerState { get; private set; } = PlayerState.Intro;


    private void Awake()
    {
        DOTween.useSafeMode = false;
        dotPool = new PathDotPool(pointPrefab, pathPointsParent);
    }

    private void OnEnable()
    {
        if (events == null)
            return;

        events.OnBattleStart += HandleBattleStart;
        events.OnBattleWin += HandleBattleWin;
        events.OnBattleLose += HandleBattleLose;
        events.OnHeroWin += HandleHeroWin;
        events.OnChestOpenStart += HandleChestOpenStart;
        events.OnChestOpened += HandleChestOpened;
        events.OnRestart += HandleRestart;
        events.OnIntroFinished += HandleIntroFinished;
    }

    private void OnDisable()
    {
        if (events == null)
            return;

        events.OnBattleStart -= HandleBattleStart;
        events.OnBattleWin -= HandleBattleWin;
        events.OnBattleLose -= HandleBattleLose;
        events.OnHeroWin -= HandleHeroWin;
        events.OnChestOpenStart -= HandleChestOpenStart;
        events.OnChestOpened -= HandleChestOpened;
        events.OnRestart -= HandleRestart;
        events.OnIntroFinished -= HandleIntroFinished;
    }

    /// <summary>
    /// Интро (катсцена + туториал) завершено — возвращаем управление игроку.
    /// </summary>
    private void HandleIntroFinished()
    {
        PlayerState = PlayerState.Idle;
    }

    /// <summary>
    /// Рестарт: возвращает всё в исходное состояние перезагрузкой активной сцены.
    /// KillAll обязателен — иначе активные твины (DOTween.useSafeMode = false) в кадре
    /// выгрузки обратятся к уничтоженным объектам и кинут NullReferenceException.
    /// </summary>
    private void HandleRestart()
    {
        DOTween.KillAll();
        SceneManager.LoadScene(SceneManager.GetActiveScene().buildIndex);
    }

    private void HandleBattleStart()
    {
        PlayerState = PlayerState.Fighting;
        heroView.SetRun(false);
    }

    private void HandleBattleWin(EnemyView enemy)
    {
        // Победа: маршрут прерываем и запускаем сцену удара по врагу.
        heroView.SetRun(false);

        if (moveCoroutine != null)
        {
            StopCoroutine(moveCoroutine);
            moveCoroutine = null;
        }

        ClearDots();

        StartCoroutine(AttackRoutine(enemy));
    }

    /// <summary>
    /// Финальная победа героя. UIManager по этому же событию показывает окно итога, а здесь
    /// блокируем игровой ввод терминальным состоянием Win — иначе клики продолжали бы
    /// управлять героем «сквозь» окно (HandleClick/Update реагируют только в Idle/Moving).
    /// </summary>
    private void HandleHeroWin()
    {
        PlayerState = PlayerState.Win;
        heroView.SetRun(false);

        if (moveCoroutine != null)
        {
            StopCoroutine(moveCoroutine);
            moveCoroutine = null;
        }

        ClearDots();
    }

    private IEnumerator AttackRoutine(EnemyView enemy)
    {
        // Остаёмся в Fighting: ввод заблокирован, пока герой бьёт.
        // 1) Поворот в сторону врага (та же логика поворота, что и при движении).
        Vector3 dir = enemy.transform.position - heroView.transform.position;
        dir.y = 0;
        CharacterRotate(dir);
        yield return null;
        // 2) Анимация удара. Враг умирает от animation event'а в середине удара
        //    (event дёргает HeroView.OnAttackHit -> enemy.Die()).
        heroView.PlayAttack(enemy);

        yield return new WaitForSeconds(attackDuration);

        // 3) После удара герой возвращается в InitRotation и в Idle (как при обычной остановке).
        heroView.ResetFacing();

        // Не перетираем терминальное состояние: при финальной победе OnHeroWin уже выставил
        // Win (окно итога). Возврат в Idle только если бой действительно завершился обычным.
        if (PlayerState == PlayerState.Fighting)
        {
            PlayerState = PlayerState.Idle;
        }
    }

    private void HandleBattleLose(EnemyView enemy)
    {
        // Игра окончена: ввод заблокирован. Смерть героя проиграется на strike-евенте врага.
        PlayerState = PlayerState.Dead;
        heroView.SetRun(false);

        if (moveCoroutine != null)
        {
            StopCoroutine(moveCoroutine);
            moveCoroutine = null;
        }

        ClearDots();

        // Герой встаёт в initial поворот и idle (как при обычной остановке).
        heroView.ResetFacing();

        // Враг мгновенно поворачивается к герою и бьёт; смерть героя — на strike-евенте
        // (relay -> EnemyView.OnAttackHit -> HeroView.OnKilled -> SetDie + RaiseHeroLose).
        enemy.FaceInstant(heroView.transform.position);
        enemy.Attack(heroView);
    }

    private void HandleChestOpenStart()
    {
        // Герой встаёт у сундука и ждёт: маршрут прерываем, ввод блокируем.
        heroView.SetRun(false);

        if (moveCoroutine != null)
        {
            StopCoroutine(moveCoroutine);
            moveCoroutine = null;
        }

        ClearDots();

        // «Занят»: HandleClick срабатывает только в Idle, поэтому ввод заблокирован.
        PlayerState = PlayerState.Fighting;

        // Встаём в initial поворот (как при обычной остановке).
        heroView.ResetFacing();
    }

    private void HandleChestOpened()
    {
        // Меч получен — управление возвращается.
        PlayerState = PlayerState.Idle;
    }

    private void MoveToPoint(Vector3 target)
    {
        if (moveCoroutine != null)
        {
            StopCoroutine(moveCoroutine);
            moveCoroutine = null;
        }

        ClearDots();

        // Путь строим по графу проходимых точек (замена NavMesh — Luna его не поддерживает).
        currentPath.Clear();
        if (!graph.FindPath(heroView.transform.position, target, currentPath) || currentPath.Count < 2)
        {
            // Путь не построен (клик в отрезанную/недостижимую зону — напр. эрозия отступа
            // разорвала связность). Нельзя оставлять героя в подвисшем Moving с вечной
            // анимацией бега: аккуратно гасим бег и возвращаемся в Idle.
            heroView.SetRun(false);
            heroView.ResetFacing();
            if (PlayerState == PlayerState.Moving)
            {
                PlayerState = PlayerState.Idle;
            }
            return;
        }

        BuildDots();

        moveCoroutine = StartCoroutine(MoveCoroutine());
    }
    
    private IEnumerator MoveCoroutine()
    {
        PlayerState = PlayerState.Moving;
        heroView.SetRun(true);
        
        for (int i = 1; i < currentPath.Count; i++)
        {
            Vector3 target = currentPath[i];

            Vector3 dir = target - heroView.transform.position;
            dir.y = 0;
            
            CharacterRotate(dir);

            while (Vector3.Distance(heroView.transform.position, target) > 0.03f)
            {
                if (PlayerState == PlayerState.Dead)
                    yield break;

                // Пауза движения на время боя; после победы состояние снова Moving.
                while (PlayerState == PlayerState.Fighting)
                    yield return null;

                if (PlayerState == PlayerState.Dead)
                    yield break;

                heroView.transform.position = Vector3.MoveTowards(
                    heroView.transform.position,
                    target,
                    moveSpeed * Time.deltaTime);

                UpdateDots(target);

                yield return null;
            }

            heroView.transform.position = target;
        }

        ClearDots();

        moveCoroutine = null;
        
        heroView.ResetFacing();
        heroView.SetRun(false);
        PlayerState = PlayerState.Idle;
    }

    /// <summary>
    /// Метод врощает персонажа в направлении движения. поворот мирового перемещения конвертирвем в локальный поворот.
    /// </summary>
    /// <param name="dir"></param>
    private void CharacterRotate(Vector3 dir)
    {
        // Изометрический поворот вынесен в HeroView.FaceDirection (переиспользуется в интро).
        heroView.FaceDirection(dir);
    }


    private void ClearDots()
    {
        foreach (var dot in activeDots)
            dotPool.Release(dot);

        activeDots.Clear();

        // Круг конца пути живёт вместе с точками маршрута.
        if (destinationMarker != null)
        {
            Destroy(destinationMarker);
            destinationMarker = null;
        }
    }
    
    private void BuildDots()
    {
        if (currentPath.Count < 2)
            return;

        // Общая длина пути — чтобы не ставить точки вплотную к кругу в конце.
        float totalLen = 0f;
        for (int i = 0; i < currentPath.Count - 1; i++)
            totalLen += Vector3.Distance(currentPath[i], currentPath[i + 1]);

        // Дальше этой отметки точки не ставим — оставляем зазор под маркер конца пути.
        float lastDotAt = totalLen - dotEndClearance;

        // Раскладываем точки равномерно вдоль ВСЕЙ ломаной пути (общий шаг dotSpacing),
        // а не посегментно с рестартом в каждой вершине — иначе на коротких сегментах
        // точки соседних углов накладываются друг на друга.
        float traveled = 0f;       // накопленная длина пройденной части ломаной
        float nextAt = dotSpacing; // дистанция до следующей точки (не спавним под ногами)

        for (int i = 0; i < currentPath.Count - 1; i++)
        {
            var start = currentPath[i];
            var end = currentPath[i + 1];

            float segLen = Vector3.Distance(start, end);
            if (segLen < 0.0001f)
                continue;

            var direction = (end - start) / segLen;

            // Все отметки шага, попавшие в этот сегмент (и не ближе зазора к концу).
            while (nextAt <= traveled + segLen && nextAt <= lastDotAt)
            {
                var position = start + direction * (nextAt - traveled);

                activeDots.Add(dotPool.Get(position, maxDotAlpha));

                nextAt += dotSpacing;
            }

            traveled += segLen;
        }

        // Круг в конце пути (точка, куда придёт герой). Плашка лежит на земле — поворот 90° по X.
        if (destinationMarkerPrefab != null)
            destinationMarker = Instantiate(
                destinationMarkerPrefab,
                currentPath[currentPath.Count - 1],
                Quaternion.Euler(90f, 0f, 0f),
                pathPointsParent);
    }
    
    private void UpdateDots(Vector3 target)
    {
        Vector3 heroPos = heroView.transform.position;
        Vector3 moveDir = target - heroPos;
        moveDir.y = 0;

        for (int i = activeDots.Count - 1; i >= 0; i--)
        {
            var dot = activeDots[i];

            // Считаем по горизонтали — не зависим от высоты пивота/навмеша.
            Vector3 delta = dot.transform.position - heroPos;
            delta.y = 0;
            float d = delta.magnitude;

            // Гаснем по мере приближения героя (в пределах fade-радиуса).
            if (d < dotFadeDistance)
                dot.SetAlpha((d / dotFadeDistance) * maxDotAlpha);

            // Точка пройдена: она близко и уже позади направления движения — в пул.
            if (d < dotFadeDistance && Vector3.Dot(delta, moveDir) < 0f)
            {
                dotPool.Release(dot);
                activeDots.RemoveAt(i);
            }
        }
    }
    
    void HandleClick()
    {
        // Кликать можно и стоя (Idle), и уже в движении (Moving) — путь перестроится
        // от текущей позиции персонажа. Во время боя/сундука (Fighting), смерти (Dead)
        // и победы (Win) ввод остаётся заблокированным.
        if (PlayerState != PlayerState.Idle && PlayerState != PlayerState.Moving)
            return;

        // Клик по UI (окно итога, кнопка рестарта) не должен уводить героя бежать «под окном».
        if (EventSystem.current != null && EventSystem.current.IsPointerOverGameObject())
        {
            return;
        }

        // Позиция указателя (мышь на ПК / палец на тач-устройстве).
        var mousePosition = PointerInput.GetPosition();
        
        // Создаем луч из камеры
        Ray ray = mainCamera.ScreenPointToRay(new Vector3(mousePosition.x, mousePosition.y, 0));
        RaycastHit hit;

        // Клик по интерактивной сущности (враг/сундук) — пульс объекта.
        // Отдельный луч: сущности не на слое NavMesh, а их коллайдеры — триггеры.
        Characters.EntityBase clickedEntity = null;
        if (Physics.Raycast(ray, out var entityHit, 100f, interactableMask, QueryTriggerInteraction.Collide))
        {
            clickedEntity = entityHit.collider.GetComponentInParent<Characters.EntityBase>();
            if (clickedEntity != null)
                clickedEntity.PulseClick();
        }

        // Цель движения: по земле (навмешу). Если по земле не попали, но кликнули по
        // интерактиву — бежим к позиции самого объекта (как будто кликнули туда).
        Vector3 destination;
        if (Physics.Raycast(ray, out hit, 100f, 1 << 3))
            destination = hit.point;
        else if (clickedEntity != null)
            destination = clickedEntity.transform.position;
        else
            return;

        // Привязку к проходимой зоне делает сам граф (снап к ближайшему узлу).
        MoveToPoint(destination);
    }
    private void Update()
    {
        // Ввод активен только когда игрок реально управляет героем. Во время интро,
        // боя, смерти и победы клик не звучит и не обрабатывается.
        if (PlayerState != PlayerState.Idle && PlayerState != PlayerState.Moving)
        {
            return;
        }

        if (PointerInput.PressedThisFrame())
        {
            // Звук тапа во время игры (не зависит от попадания в навмеш).
            // Не дублируем на UI — там свой звук (UiButtonSound).
            if (EventSystem.current == null || !EventSystem.current.IsPointerOverGameObject())
            {
                AudioManager.Instance?.PlayGameClick();
            }

            HandleClick();
        }
    }
    
}
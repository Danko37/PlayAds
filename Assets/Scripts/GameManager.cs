using System;
using UnityEngine;
using UnityEngine.AI;
using System.Collections;
using System.Collections.Generic;
using DG.Tweening;

public enum PlayerState
{
    Idle,
    Moving,
    Fighting,
    Dead,
    Win
}

public class GameManager : MonoBehaviour
{
    [Header("References")]
    [SerializeField] private Camera mainCamera;
    [SerializeField] private Transform pathPointsParent;
    
    [Header("Settings")]
    [SerializeField] private float pointHeight = 0.1f;
    [SerializeField] private GameObject pointPrefab;
    
    [SerializeField] private float moveSpeed = 8f;
    [SerializeField] private float dotSpacing = 50f;
    [SerializeField] private float dotFadeDistance = 0.6f;
    [SerializeField] private HeroView heroView;

    private Coroutine moveCoroutine;

    private readonly List<Vector3> currentPath = new();
    private readonly List<PathDot> activeDots = new();

    private NavMeshPath navPath;
    
    public PlayerState PlayerState { get; private set; } = PlayerState.Idle;


    private void Awake()
    {
        DOTween.useSafeMode = false;
    }

    private void MoveToPoint(Vector3 target)
    {
        if (moveCoroutine != null)
            StopCoroutine(moveCoroutine);

        ClearDots();

        navPath ??= new NavMeshPath();
        
        heroView.NavMeshAgent.enabled = true;

        if (!heroView.NavMeshAgent.CalculatePath(target, navPath))
            return;

        if (navPath.status != NavMeshPathStatus.PathComplete)
            return;

        currentPath.Clear();
        currentPath.AddRange(navPath.corners);

        BuildDots();

        heroView.NavMeshAgent.enabled = false;

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
                heroView.transform.position = Vector3.MoveTowards(
                    heroView.transform.position,
                    target,
                    moveSpeed * Time.deltaTime);

                UpdateDots();

                yield return null;
            }

            heroView.transform.position = target;
        }

        ClearDots();

        moveCoroutine = null;
        
        heroView.HeroVisualTransform.localRotation = Quaternion.Euler(0, heroView.InitialYRotation, 0);
        heroView.SetRun(false);
        PlayerState = PlayerState.Idle;
    }

    /// <summary>
    /// Метод врощает персонажа в направлении движения. поворот мирового перемещения конвертирвем в локальный поворот.
    /// </summary>
    /// <param name="dir"></param>
    private void CharacterRotate(Vector3 dir)
    {
        //направление по оси Y
        var worldDir = Quaternion.FromToRotation(Vector3.forward, dir.normalized).eulerAngles.y;
        
        //135 - разница между мировым поворотом и локальным поворотом персонажа в изометрии (магия)
        var res = worldDir - 135;
        
        heroView.HeroVisualTransform.localRotation = res < 0 ? Quaternion.Euler(0, 360 - Math.Abs(res), 0) : Quaternion.Euler(0, res, 0);
    }


    private void ClearDots()
    {
        foreach (var dot in activeDots)
        {
            if (dot != null)
                Destroy(dot.gameObject);
        }

        activeDots.Clear();
    }
    
    private void BuildDots()
    {
        if (currentPath.Count < 2)
            return;

        float delay = 0f;

        for (int i = 0; i < currentPath.Count - 1; i++)
        {
            var start = currentPath[i];
            var end = currentPath[i + 1];

            var distance = Vector3.Distance(start, end);

            var dotCount = Mathf.FloorToInt(distance / dotSpacing);

            var direction = (end - start).normalized;

            for (int j = 0; j <= dotCount; j++)
            {
                var position = start + direction * (j * dotSpacing);

                var obj = Instantiate(
                    pointPrefab,
                    position,
                    Quaternion.identity,
                    pathPointsParent);
                
                var dot = obj.GetComponent<PathDot>();
                
                dot.transform.rotation = Quaternion.Euler(dot.InitRotation);
                
                activeDots.Add(dot);

                dot.Show(0);

                //delay += 0.02f;
            }
        }
    }
    
    private void UpdateDots()
    {
        for (int i = activeDots.Count - 1; i >= 0; i--)
        {
            var dot = activeDots[i];

            var d = Vector3.Distance(
                heroView.transform.position,
                dot.transform.position);

            if (d < dotFadeDistance)
            {
                dot.SetAlpha(d / dotFadeDistance);

                if (d < 0.1f)
                {
                    Destroy(dot.gameObject);
                    activeDots.RemoveAt(i);
                }
            }
        }
    }
    
    void HandleClick()
    {
        if (PlayerState != PlayerState.Idle)
            return;
        
        if (UnityEngine.InputSystem.Mouse.current == null)
            return;
            
        // Получаем позицию мыши
        Vector2 mousePosition = UnityEngine.InputSystem.Mouse.current.position.ReadValue();
        
        // Создаем луч из камеры
        Ray ray = mainCamera.ScreenPointToRay(new Vector3(mousePosition.x, mousePosition.y, 0));
        RaycastHit hit;
        
        if (Physics.Raycast(ray, out hit, 100f))
        {
            NavMeshHit navHit;
            if (NavMesh.SamplePosition(hit.point, out navHit, 2f, NavMesh.AllAreas))
            {
                MoveToPoint(navHit.position);
            }
        }
    }
    private void Update()
    {
        if (UnityEngine.InputSystem.Mouse.current == null)
            return;

        if (UnityEngine.InputSystem.Mouse.current.leftButton.wasPressedThisFrame)
        {
            HandleClick();
        }
    }
    
}
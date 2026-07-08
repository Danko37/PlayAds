using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// GoF Object Pool для точек маршрута (PathDot): переиспользуем объекты вместо
/// Instantiate/Destroy на каждый клик. Свободные точки лежат в стеке неактивными,
/// активные выдаёт Get, возврат — через Release.
/// </summary>
public class PathDotPool
{
    private readonly GameObject _prefab;
    private readonly Transform _parent;
    private readonly Stack<PathDot> _idle = new();

    public PathDotPool(GameObject prefab, Transform parent)
    {
        _prefab = prefab;
        _parent = parent;
    }

    /// <summary>
    /// Берёт точку из пула (или создаёт новую, если пул пуст), ставит в позицию с её
    /// собственным разворотом и показывает с заданной прозрачностью.
    /// </summary>
    public PathDot Get(Vector3 position, float alpha)
    {
        var dot = _idle.Count > 0
            ? _idle.Pop()
            : Object.Instantiate(_prefab, _parent).GetComponent<PathDot>();

        dot.transform.SetPositionAndRotation(position, Quaternion.Euler(dot.InitRotation));
        dot.gameObject.SetActive(true);
        dot.Show(0);
        dot.SetAlpha(alpha);

        return dot;
    }

    /// <summary>Возвращает точку в пул: гасит твин появления и прячет объект.</summary>
    public void Release(PathDot dot)
    {
        if (dot == null)
            return;

        dot.ResetForPool();
        dot.gameObject.SetActive(false);
        _idle.Push(dot);
    }
}

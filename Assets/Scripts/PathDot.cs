using System;
using DG.Tweening;
using DG.Tweening.Core;
using DG.Tweening.Plugins.Options;
using UnityEngine;

public class PathDot : MonoBehaviour
{
    [SerializeField] private Vector3 initRotation = new(38.4044266f, 3.1675055f, 44.5107918f);
    [SerializeField] private SpriteRenderer sprite;
    [Tooltip("Итоговый размер точки после появления.")]
    [SerializeField] private float shownScale = 1.6f;
    private TweenerCore<Vector3, Vector3, VectorOptions> _tween;
    public Vector3 InitRotation => initRotation;

    public void Show(float delay)
    {
        transform.localScale = Vector3.zero;

        _tween = transform
            .DOScale(shownScale, 0.25f)
            .SetDelay(delay)
            .SetEase(Ease.OutBack);
    }

    public void SetAlpha(float alpha)
    {
        var c = sprite.color;
        c.a = alpha;
        sprite.color = c;
    }

    private void OnDestroy()
    {
        _tween?.Kill();
        _tween =  null;
    }
}
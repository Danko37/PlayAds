using DG.Tweening;
using UnityEngine;

public class PathDot : MonoBehaviour
{
    public static Vector3 InitRotation = new(38.4044266f, 3.1675055f, 44.5107918f);
    
    [SerializeField] private SpriteRenderer sprite;

    public void Show(float delay)
    {
        transform.localScale = Vector3.zero;

        transform
            .DOScale(0.3f, 0.25f)
            .SetDelay(delay)
            .SetEase(Ease.OutBack);
    }

    public void SetAlpha(float alpha)
    {
        Color c = sprite.color;
        c.a = alpha;
        sprite.color = c;
    }
}
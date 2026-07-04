using System;
using Characters;
using DG.Tweening;
using UnityEngine;

public class Chest : EntityBase
{
    [SerializeField]
    private GameObject  chestOpen;
    [SerializeField]
    private GameObject  chestClose;
    [SerializeField]
    private SpriteAnimator openEffect;
    [SerializeField]
    private GameObject SwordItem;

    [Header("Открытие")]
    [Tooltip("Узел, который масштабируем при открытии (ChestRoot). Не весь Chest — иначе заскейлится Canvas/Score.")]
    [SerializeField]
    private Transform chestRoot;
    [Tooltip("Триггер-коллайдер сундука. Отключается при открытии, чтобы не было повторных срабатываний.")]
    [SerializeField]
    private Collider bodyCollider;
    [Tooltip("Имя клипа эффекта открытия в SpriteAnimator на узле Effect.")]
    [SerializeField]
    private string openEffectClip;

    [SerializeField, Range(0.1f, 1f)]
    private float shrinkFactor = 0.8f;
    [Tooltip("Половина длительности пружинки сундука (уменьшение + возврат). Итого = x2 = 0.3с.")]
    [SerializeField]
    private float scaleHalfDuration = 0.15f;
    [Tooltip("На сколько единиц по локальной оси Z меч поднимается вверх.")]
    [SerializeField]
    private float swordRiseZ = 1f;
    [SerializeField]
    private float swordRiseDuration = 0.25f;
    [SerializeField]
    private float swordFlyDuration = 0.4f;

    private bool _opened;

    /// <summary>
    /// Полная анимация открытия сундука и выдачи меча. Когда меч долетает до <paramref name="heroTarget"/>,
    /// вызывается <paramref name="onGetSword"/> (экипировка меча у героя + возврат управления).
    /// </summary>
    public void OpenChest(Transform heroTarget, Action onGetSword)
    {
        if (_opened)
            return;
        _opened = true;

        // Больше не реагируем на триггер.
        if (bodyCollider != null)
            bodyCollider.enabled = false;

        var rootScale = chestRoot.localScale;
        var swordScale = SwordItem.transform.localScale;
        var swordStartLocalPos = SwordItem.transform.localPosition;
        
        openEffect.gameObject.SetActive(true);
        
        // Эффект открытия из спрайт-рендерера.
        if (openEffect != null)
            openEffect.Play(openEffectClip);

        var seq = DOTween.Sequence();

        // 1) Сундук уменьшается до минимума.
        seq.Append(chestRoot.DOScale(rootScale * shrinkFactor, scaleHalfDuration).SetEase(Ease.InQuad));

        // 2) В момент минимума скейла: меняем закрытый сундук на открытый и активируем меч (в уменьшенном виде).
        seq.AppendCallback(() =>
        {
            chestClose.SetActive(false);
            chestOpen.SetActive(true);

            SwordItem.transform.localScale = swordScale * shrinkFactor;
            SwordItem.SetActive(true);
        });

        // 3) Сундук возвращается в исходный скейл.
        seq.Append(chestRoot.DOScale(rootScale, scaleHalfDuration).SetEase(Ease.OutQuad));

        // 4) Одновременно (с момента минимума): меч поднимается вверх на +swordRiseZ по Z и дорастает до нормы.
        seq.Join(SwordItem.transform.DOLocalMoveZ(swordStartLocalPos.z + swordRiseZ, swordRiseDuration));
        seq.Join(SwordItem.transform.DOScale(swordScale, swordRiseDuration));

        // 5) Меч летит к герою.
        seq.Append(SwordItem.transform.DOMove(heroTarget.position, swordFlyDuration).SetEase(Ease.InBack));

        // 6) Долетел — экипируем меч у героя и убираем «летающий» меч (его заменяет модель героя с мечом).
        seq.OnComplete(() =>
        {
            onGetSword?.Invoke();
            SwordItem.SetActive(false);
        });
    }
}

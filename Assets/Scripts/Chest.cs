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
    private SpriteAnimator openEffect2;
    [SerializeField]
    private GameObject SwordItem;
    [SerializeField]
    private CanvasGroup scoreCanvasGroup;

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
    [SerializeField]
    private string openEffectClip2;

    [Tooltip("До какого размера уменьшается меч в анимации.")]
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
    [Tooltip("Длительность схлопывания сундука в ноль после того, как меч долетел до героя.")]
    [SerializeField]
    private float chestDisappearDuration = 0.2f;

    private bool _opened;

    private void Awake()
    {
        openEffect.onAnimationFinished += OnAnimationEffectFinished;
        openEffect2.onAnimationFinished += OnAnimationEffect2Finished;
    }

    private void OnDestroy()
    {
        openEffect.onAnimationFinished -= OnAnimationEffectFinished;
        openEffect2.onAnimationFinished -= OnAnimationEffect2Finished;
    }

    private void OnAnimationEffectFinished()
    {
        if(!openEffect.gameObject.activeSelf) return;
        openEffect.gameObject.SetActive(false);
    }
    private void OnAnimationEffect2Finished()
    {
        if(!openEffect2.gameObject.activeSelf) return;
        openEffect2.gameObject.SetActive(false);
    }

    /// <summary>
    /// Полная анимация открытия сундука и выдачи меча. Когда меч долетает до <paramref name="hero"/>,
    /// вызывается <paramref name="onGetSword"/> (экипировка меча у героя + возврат управления).
    /// </summary>
    public void OpenChest(HeroView hero, Action onGetSword)
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

        var seq = DOTween.Sequence();

        seq.AppendCallback(() =>
        {
            scoreCanvasGroup.DOFade(0f, 0.4f);
        });
        seq.AppendInterval(0.1f);
        // 1) Сундук уменьшается до минимума.
        seq.Append(chestRoot.DOScale(rootScale * shrinkFactor, scaleHalfDuration).SetEase(Ease.InQuad));

        // 2) В момент минимума скейла: меняем закрытый сундук на открытый и активируем меч (в уменьшенном виде).
        seq.AppendCallback(() =>
        {
            openEffect.gameObject.SetActive(true);
            openEffect2.gameObject.SetActive(true);
            // Эффект открытия из спрайт-рендерера.
            if (openEffect != null)
                openEffect.Play(openEffectClip);
        
            if (openEffect2 != null)
                openEffect2.Play(openEffectClip);
            
            chestClose.SetActive(false);
            chestOpen.SetActive(true);

            SwordItem.transform.localScale = swordScale * shrinkFactor;
            SwordItem.SetActive(true);
        });

        // 3) Сундук возвращается в исходный скейл.
        seq.Append(chestRoot.DOScale(rootScale, scaleHalfDuration).SetEase(Ease.OutQuad));
        seq.Join(SwordItem.transform.DOLocalRotate(new Vector3(90f, 0f, 0f), swordRiseDuration)).SetEase(Ease.InSine);
        // 4) Одновременно (с момента минимума): меч поднимается вверх на +swordRiseZ по Z и дорастает до нормы.
        seq.Join(SwordItem.transform.DOLocalMoveZ(swordStartLocalPos.z + swordRiseZ, swordRiseDuration));
        seq.Join(SwordItem.transform.DOScale(swordScale, swordRiseDuration));
        // 5) Меч летит к герою.
        seq.Append(SwordItem.transform.DOMove(hero.SwordTarget.position, swordFlyDuration).SetEase(Ease.InSine));

        seq.AppendCallback(() =>
        {
            onGetSword?.Invoke();
            SwordItem.SetActive(false);
        });
        // 6) Долетел — экипируем меч у героя и убираем «летающий» меч (его заменяет модель героя с мечом).
        seq.AppendInterval(0.8f);

        seq.AppendCallback(() =>
        {
            chestRoot.DOScale(0f, chestDisappearDuration).SetEase(Ease.InBack);
        });
    }
}

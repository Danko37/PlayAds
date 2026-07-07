using DG.Tweening;
using UnityEngine;
using UnityEngine.Events;

namespace Ui
{
    public class GameResultUI : MonoBehaviour
    {
        [SerializeField] private RectTransform titleTransform;

        [SerializeField] private RectTransform imageTransform;

        [SerializeField] private RectTransform buttonTransform;
        [SerializeField] private UnityEvent onClickEvent;
        
        private Sequence _sequence = null;
        
        private void OnEnable()
        {
            titleTransform.localScale = Vector3.zero;
            imageTransform.localScale = Vector3.zero;
            buttonTransform.localScale = Vector3.zero;
            
            _sequence?.Kill();
            _sequence = null;
            
            _sequence = DOTween.Sequence()
                .Insert(0f,titleTransform.DOScale(Vector3.one, 0.4f).SetEase(Ease.OutBack))
                .Insert(0.2f,imageTransform.DOScale(Vector3.one, 0.4f).SetEase(Ease.OutBack))
                .Insert(0.4f,buttonTransform.DOScale(Vector3.one, 0.4f).SetEase(Ease.OutBack));
        }

        private void OnDisable()
        {
            _sequence?.Kill();
            _sequence = null; 
        }

        public void OnClick()
        {
            onClickEvent?.Invoke();
        }
    }
}
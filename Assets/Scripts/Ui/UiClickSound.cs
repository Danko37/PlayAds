using UnityEngine;
using UnityEngine.EventSystems;

namespace Ui
{
    public class UiClickSound : MonoBehaviour, IPointerClickHandler
    {
        public void OnPointerClick(PointerEventData eventData)
        {
            AudioManager.Instance?.PlayUiClick();
        }
    }
}

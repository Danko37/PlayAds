using UnityEngine;
using UnityEngine.EventSystems;

/// <summary>
/// Вешается на любой UI-элемент (кнопку/панель): проигрывает звук клика по UI.
/// Для «клика где угодно в UI» можно повесить на корневой Canvas с Raycast Target.
/// </summary>
public class UiButtonSound : MonoBehaviour, IPointerClickHandler
{
    public void OnPointerClick(PointerEventData eventData)
    {
        AudioManager.Instance?.PlayUiClick();
    }
}

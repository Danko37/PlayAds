using UnityEngine;

/// <summary>
/// Унифицированный ввод указателя на legacy Input Manager. Новый Input System не
/// поддерживается Playworks/Luna, поэтому читаем ввод через старый <see cref="Input"/>:
/// работает и с мышью на ПК, и с тапами на тач-устройствах.
/// Если есть касания — читаем первое касание, иначе мышь. Так избегаем двойной
/// обработки на платформах, где касание дублируется в события мыши.
/// </summary>
public static class PointerInput
{
    /// <summary>Было ли нажатие (клик мышью или начало касания) в этом кадре.</summary>
    public static bool PressedThisFrame()
    {
        if (Input.touchCount > 0)
        {
            return Input.GetTouch(0).phase == TouchPhase.Began;
        }

        return Input.GetMouseButtonDown(0);
    }

    /// <summary>Текущая экранная позиция указателя (палец или курсор мыши).</summary>
    public static Vector2 GetPosition()
    {
        if (Input.touchCount > 0)
        {
            return Input.GetTouch(0).position;
        }

        return Input.mousePosition;
    }
}

using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.EventSystems;

public class MaterialSlider : MonoBehaviour,
    ISelectHandler, IPointerEnterHandler, IMoveHandler
{
    public void OnPointerEnter(PointerEventData eventData)
    {
        MenuSfx.instance.PlaySelectSound();
    }

    public void OnSelect(BaseEventData eventData)
    {
        if (eventData is AxisEventData)
        {
            // Only play sound if selected with keyboard navigation.
            MenuSfx.instance.PlaySelectSound();
        }
    }

    public void OnMove(AxisEventData eventData)
    {
        if (eventData is AxisEventData)
        {
            MoveDirection dir = (eventData as AxisEventData).moveDir;
            if (dir == MoveDirection.Left || dir == MoveDirection.Right)
            {
                MenuSfx.instance.PlaySelectSound();
            }
        }
    }
}

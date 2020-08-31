using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.EventSystems;

public class MaterialDropdownItem : MonoBehaviour,
    ISelectHandler, IPointerEnterHandler
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
}

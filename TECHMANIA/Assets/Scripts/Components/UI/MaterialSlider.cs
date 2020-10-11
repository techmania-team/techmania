using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

public class MaterialSlider : MonoBehaviour,
    ISelectHandler, IPointerEnterHandler, IMoveHandler
{
    private Slider slider;

    private void Start()
    {
        slider = GetComponent<Slider>();
    }

    public void OnPointerEnter(PointerEventData eventData)
    {
        if (TouchInducedPointer.EventIsFromActualMouse(eventData) &&
            slider.interactable)
        {
            MenuSfx.instance.PlaySelectSound();
        }
    }

    public void OnSelect(BaseEventData eventData)
    {
        if (eventData is AxisEventData &&
            slider.interactable)
        {
            // Only play sound if selected with keyboard navigation.
            MenuSfx.instance.PlaySelectSound();
        }
    }

    public void OnMove(AxisEventData eventData)
    {
        MoveDirection dir = eventData.moveDir;
        if (dir == MoveDirection.Left || dir == MoveDirection.Right)
        {
            MenuSfx.instance.PlaySelectSound();
        }
    }
}

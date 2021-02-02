using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

public class MaterialSlider : MonoBehaviour,
    ISelectHandler, IPointerEnterHandler, IMoveHandler,
    IEndDragHandler, IPointerDownHandler
{
    public float step;

    private Slider slider;
    private float previousValue;

    private void Start()
    {
        slider = GetComponent<Slider>();
        previousValue = slider.value;
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

        switch (dir)
        {
            case MoveDirection.Left:
                slider.value = previousValue - step;
                break;
            case MoveDirection.Right:
                slider.value = previousValue + step;
                break;
        }
        previousValue = slider.value;
    }

    public void OnEndDrag(PointerEventData eventData)
    {
        // So that the next move starts from the correct value.
        previousValue = slider.value;
    }

    public void OnPointerDown(PointerEventData eventData)
    {
        // So that the next move starts from the correct value.
        previousValue = slider.value;
    }
}

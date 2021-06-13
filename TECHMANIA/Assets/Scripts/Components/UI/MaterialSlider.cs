using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Events;
using UnityEngine.EventSystems;
using UnityEngine.UI;

[Serializable]
public class SliderEndEditEvent : UnityEvent<float> { }

public class MaterialSlider : MonoBehaviour,
    ISelectHandler, IPointerEnterHandler, IMoveHandler,
    IPointerUpHandler
{
    public float step;
    // Prefer this event over OnValueChanged, because this does not
    // fire every frame the pointer is held.
    public SliderEndEditEvent endEdit;

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
            slider.IsInteractable())
        {
            MenuSfx.instance.PlaySelectSound();
        }
    }

    public void OnSelect(BaseEventData eventData)
    {
        if (eventData is AxisEventData &&
            slider.IsInteractable())
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
        endEdit?.Invoke(slider.value);
    }

    public void OnPointerUp(PointerEventData eventData)
    {
        // So that the next move starts from the correct value.
        previousValue = slider.value;
        endEdit?.Invoke(slider.value);
    }
}

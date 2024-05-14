using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.EventSystems;

public class MaterialDropdownItem : MonoBehaviour,
    ISelectHandler
{
    public UnityEngine.UI.Image image;

    public void OnSelect(BaseEventData eventData)
    {
        if (eventData is AxisEventData)
        {
            // Only play sound if selected with keyboard navigation.
            MenuSfx.instance.PlaySelectSound();
        }
    }

    private void Update()
    {
        image.gameObject.SetActive(image.sprite != null);
    }
}

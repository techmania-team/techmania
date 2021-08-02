using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

public class ScrollIntoViewOnSelect : MonoBehaviour, ISelectHandler
{
    public void OnSelect(BaseEventData eventData)
    {
        if (!(eventData is AxisEventData)) return;
        ScrollIntoView();
    }

    public void ScrollIntoView()
    {
        UIUtils.ScrollIntoView(
            GetComponent<RectTransform>(),
            GetComponentInParent<ScrollRect>(),
            normalizedMargin: 0.05f,
            viewRectAsPoint: false,
            horizontal: false,
            vertical: true);
    }
}

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

        RectTransform rect = GetComponent<RectTransform>();
        ScrollRect scrollRect = GetComponentInParent<ScrollRect>();
        RectTransform viewPort = scrollRect.viewport;
        RectTransform content = scrollRect.content;

        float minY, maxY, viewMinY, viewMaxY, contentMinY, contentMaxY;
        GetMinMaxY(rect, out minY, out maxY);
        GetMinMaxY(viewPort, out viewMinY, out viewMaxY);
        GetMinMaxY(content, out contentMinY, out contentMaxY);

        float contentHeight = contentMaxY - contentMinY;
        float viewPortHeight = viewMaxY - viewMinY;

        if (maxY > viewMaxY)
        {
            // Scroll upwards.
            scrollRect.verticalNormalizedPosition =
                (maxY - contentMinY - viewPortHeight) / (contentHeight - viewPortHeight);
        }
        if (minY < viewMinY)
        {
            // Scroll downwards.
            scrollRect.verticalNormalizedPosition =
                (minY - contentMinY) / (contentHeight - viewPortHeight);
        }
    }

    private void GetMinMaxY(RectTransform r, out float minY, out float maxY)
    {
        Vector3[] corners = new Vector3[4];
        r.GetWorldCorners(corners);
        minY = float.MaxValue;
        maxY = float.MinValue;
        foreach (Vector3 c in corners)
        {
            if (c.y < minY) minY = c.y;
            if (c.y > maxY) maxY = c.y;
        }
    }
}

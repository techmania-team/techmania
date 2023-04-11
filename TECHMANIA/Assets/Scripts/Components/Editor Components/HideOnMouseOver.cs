using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.EventSystems;

public class HideOnMouseOver : MonoBehaviour, IPointerEnterHandler,
    IPointerExitHandler
{
    public void OnPointerEnter(PointerEventData eventData)
    {
        Hide();
    }

    public void OnPointerExit(PointerEventData eventData)
    {
        Show();
    }

    private void Show()
    {
        GetComponent<CanvasGroup>().alpha = 1f;
    }

    private void Hide()
    {
        GetComponent<CanvasGroup>().alpha = 0f;
    }

    private void OnEnable()
    {
        Show();
    }
}

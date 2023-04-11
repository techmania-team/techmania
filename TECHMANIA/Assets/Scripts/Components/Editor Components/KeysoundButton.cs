using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Events;
using UnityEngine.EventSystems;

public class KeysoundButton : MonoBehaviour, IPointerClickHandler
{
    public GameObject selectedOverlay;
    public GameObject upcomingIndicator;
    public UnityAction clickHandler;

    public void OnPointerClick(PointerEventData eventData)
    {
        clickHandler?.Invoke();
    }

    public void UpdateSelected(bool selected)
    {
        selectedOverlay.SetActive(selected);
    }

    public void UpdateUpcoming(bool upcoming)
    {
        upcomingIndicator.SetActive(upcoming);
    }
}

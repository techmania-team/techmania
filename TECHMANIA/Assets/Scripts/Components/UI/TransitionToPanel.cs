using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

public class TransitionToPanel : MonoBehaviour, IPointerClickHandler, ISubmitHandler
{
    public Panel target;
    public enum Direction
    {
        Left,
        Right
    }
    public Direction targetAppearsFrom;

    public virtual void Invoke()
    {
        PanelTransitioner.TransitionTo(target, targetAppearsFrom);
    }

    public void OnPointerClick(PointerEventData eventData)
    {
        if (!GetComponent<Button>().interactable)
        {
            return;
        }
        Invoke();
    }

    public void OnSubmit(BaseEventData eventData)
    {
        Invoke();
    }
}

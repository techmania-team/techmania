using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.EventSystems;

public class TransitionToPanel : MonoBehaviour, IPointerClickHandler
{
    public Panel target;
    public enum Direction
    {
        Left,
        Right
    }
    public Direction targetAppearsFrom;

    public void OnPointerClick(PointerEventData eventData)
    {
        PanelTransitioner.TransitionTo(target, targetAppearsFrom);
    }
}

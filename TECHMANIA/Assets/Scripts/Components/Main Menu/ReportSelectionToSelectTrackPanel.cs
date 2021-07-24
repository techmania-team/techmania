using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.EventSystems;

public class ReportSelectionToSelectTrackPanel :
    MonoBehaviour, ISelectHandler
{
    private SelectTrackPanel panel;
    private int cardIndex;

    public void Initialize(SelectTrackPanel panel,
        int cardIndex)
    {
        this.panel = panel;
        this.cardIndex = cardIndex;
    }

    public void OnSelect(BaseEventData eventData)
    {
        panel.OnCardSelected(cardIndex);
    }
}

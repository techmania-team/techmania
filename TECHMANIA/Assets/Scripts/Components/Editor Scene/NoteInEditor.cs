using System.Collections;
using System.Collections.Generic;
using TMPro;
using UnityEngine;
using UnityEngine.Events;
using UnityEngine.EventSystems;
using UnityEngine.UI;

public class NoteInEditor : MonoBehaviour
{
    public Image selectionOverlay;
    public RectTransform noteImage;
    public RectTransform pathToPreviousNote;

    public static event UnityAction<GameObject> LeftClicked;
    public static event UnityAction<GameObject> RightClicked;
    public static event UnityAction<GameObject> BeginDrag;
    public static event UnityAction<Vector2> Drag;
    public static event UnityAction EndDrag;

    private void OnEnable()
    {
        PatternPanel.SelectionChanged += UpdateSelection;
        PatternPanel.KeysoundVisibilityChanged += SetKeysoundVisibility;
    }

    private void OnDisable()
    {
        PatternPanel.SelectionChanged -= UpdateSelection;
        PatternPanel.KeysoundVisibilityChanged -= SetKeysoundVisibility;
    }

    private void UpdateSelection(HashSet<GameObject> selection)
    {
        if (selectionOverlay == null) return;
        selectionOverlay.enabled = selection.Contains(gameObject);
    }

    public void SetKeysoundVisibility(bool visible)
    {
        GetComponentInChildren<TextMeshProUGUI>(includeInactive: true)
            .gameObject.SetActive(visible);
    }

    public void SetKeysoundText()
    {
        NoteObject noteObject = GetComponent<NoteObject>();
        GetComponentInChildren<TextMeshProUGUI>(includeInactive: true)
            .text = UIUtils.StripExtension(noteObject.sound);
    }

    #region Event Relay
    public void OnPointerClick(BaseEventData eventData)
    {
        if (!(eventData is PointerEventData)) return;
        PointerEventData pointerData = eventData as PointerEventData;
        if (pointerData.dragging) return;

        switch (pointerData.button)
        {
            case PointerEventData.InputButton.Left:
                LeftClicked?.Invoke(gameObject);
                break;
            case PointerEventData.InputButton.Right:
                RightClicked?.Invoke(gameObject);
                break;
        }
    }

    public void OnBeginDrag(BaseEventData eventData)
    {
        if (!(eventData is PointerEventData)) return;
        BeginDrag?.Invoke(gameObject);
    }

    public void OnDrag(BaseEventData eventData)
    {
        if (!(eventData is PointerEventData)) return;
        PointerEventData pointerData = eventData as PointerEventData;
        Drag?.Invoke(pointerData.delta);
    }

    public void OnEndDrag(BaseEventData eventData)
    {
        if (!(eventData is PointerEventData)) return;
        EndDrag?.Invoke();
    }
    #endregion

    #region Note Attachments
    public void TurnNoteImageToward(RectTransform nextNote)
    {
        Vector2 cur = GetComponent<RectTransform>().anchoredPosition;
        Vector2 next = nextNote.anchoredPosition;
        float angleInRadian = Mathf.Atan2(next.y - cur.y,
            next.x - cur.x);
        noteImage.localRotation = Quaternion.Euler(0f, 0f,
            angleInRadian * Mathf.Rad2Deg);
    }

    public void PointPathToward(RectTransform prevNote)
    {
        Vector2 prev = prevNote.anchoredPosition;
        Vector2 cur = GetComponent<RectTransform>().anchoredPosition;
        float distance = Vector2.Distance(prev, cur);
        float angleInRadian = Mathf.Atan2(cur.y - prev.y,
            cur.x - prev.x);

        pathToPreviousNote.sizeDelta = new Vector2(distance, 0f);
        pathToPreviousNote.localRotation = Quaternion.Euler(0f, 0f,
            angleInRadian * Mathf.Rad2Deg);
    }
    #endregion
}

using System.Collections;
using System.Collections.Generic;
using System.Windows.Forms;
using TMPro;
using UnityEngine;
using UnityEngine.Events;
using UnityEngine.EventSystems;
using UnityEngine.UI;

public class NoteInEditor : MonoBehaviour
{
    public Sprite hiddenSprite;
    public Sprite hiddenTrailSprite;
    public Image selectionOverlay;
    public RectTransform noteImage;
    public RectTransform pathToPreviousNote;
    public RectTransform durationTrail;
    public RectTransform invisibleTrail;

    public static event UnityAction<GameObject> LeftClicked;
    public static event UnityAction<GameObject> RightClicked;
    public static event UnityAction<GameObject> BeginDrag;
    public static event UnityAction<Vector2> Drag;
    public static event UnityAction EndDrag;
    public static event UnityAction<GameObject> DurationHandleBeginDrag;
    public static event UnityAction<Vector2> DurationHandleDrag;
    public static event UnityAction DurationHandleEndDrag;

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

    public void UseHiddenSprite()
    {
        noteImage.GetComponent<Image>().sprite = hiddenSprite;
        durationTrail.GetComponent<Image>().sprite = hiddenTrailSprite;
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

    #region Event Relay From Note Image
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

    #region Event Relay From Duration Handle
    public void OnDurationHandleBeginDrag(BaseEventData eventData)
    {
        if (!(eventData is PointerEventData)) return;
        DurationHandleBeginDrag?.Invoke(gameObject);
    }

    public void OnDurationHandleDrag(BaseEventData eventData)
    {
        if (!(eventData is PointerEventData)) return;
        PointerEventData pointerData = eventData as PointerEventData;
        DurationHandleDrag?.Invoke(pointerData.delta);
    }

    public void OnDurationHandleEndDrag(BaseEventData eventData)
    {
        if (!(eventData is PointerEventData)) return;
        DurationHandleEndDrag?.Invoke();
    }
    #endregion

    #region Note Image and Path
    public void ResetNoteImageRotation()
    {
        noteImage.localRotation = Quaternion.identity;
    }

    public void KeepPathInPlaceWhileNoteBeingDragged(
        Vector2 noteMovement)
    {
        if (pathToPreviousNote == null) return;
        pathToPreviousNote.anchoredPosition -= noteMovement;
    }

    public void ResetPathPosition()
    {
        if (pathToPreviousNote == null) return;
        pathToPreviousNote.anchoredPosition = Vector2.zero;
    }

    public void PointPathToward(GameObject target)
    {
        float distance = 0f;
        float angleInRadian = 0f;
        if (target != null)
        {
            Vector2 targetPos = target.GetComponent<RectTransform>()
            .anchoredPosition;
            Vector2 selfPos = GetComponent<RectTransform>().anchoredPosition;
            distance = Vector2.Distance(targetPos, selfPos);
            angleInRadian = Mathf.Atan2(selfPos.y - targetPos.y,
                selfPos.x - targetPos.x);
        }

        pathToPreviousNote.sizeDelta = new Vector2(distance, 0f);
        pathToPreviousNote.localRotation = Quaternion.Euler(0f, 0f,
            angleInRadian * Mathf.Rad2Deg);

        if (target != null &&
            target.GetComponent<NoteObject>().note.type == NoteType.ChainHead)
        {
            target.GetComponent<NoteInEditor>().noteImage.localRotation =
                Quaternion.Euler(0f, 0f, angleInRadian * Mathf.Rad2Deg);
        }
    }
    #endregion

    #region Duration Trail
    public void ResetTrail()
    {
        int duration = (GetComponent<NoteObject>().note as HoldNote).duration;
        float width = duration * (PatternPanel.ScanWidth /
            EditorContext.Pattern.patternMetadata.bps /
            Pattern.pulsesPerBeat);
        durationTrail.sizeDelta = new Vector2(width, 0f);
        invisibleTrail.sizeDelta = new Vector2(width, 0f);

        ToggleNoteImageTransparency();
    }

    // Only visual; does not affect note duration.
    public void ResizeTrail(float delta)
    {
        durationTrail.sizeDelta += new Vector2(delta, 0f);
        invisibleTrail.sizeDelta += new Vector2(delta, 0f);
        ToggleNoteImageTransparency();
    }

    private void ToggleNoteImageTransparency()
    {
        // TODO: make note image transparent if trails are too short.
    }
    #endregion
}

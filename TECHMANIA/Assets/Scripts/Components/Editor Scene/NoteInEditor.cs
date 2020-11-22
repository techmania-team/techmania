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
    public Texture2D horizontalResizeCursor;
    public bool transparentNoteImageWhenTrailTooShort;

    [Header("Drag Note")]
    public Sprite hiddenCurveSprite;
    public CurvedImage curvedImage;
    public RectTransform newAnchorReceiverContainer;
    public GameObject newAnchorReceiverTemplate;
    public RectTransform anchorContainer;
    public GameObject anchorTemplate;

    public static event UnityAction<GameObject> LeftClicked;
    public static event UnityAction<GameObject> RightClicked;

    public static event UnityAction<GameObject> BeginDrag;
    public static event UnityAction<Vector2> Drag;
    public static event UnityAction EndDrag;

    public static event UnityAction<GameObject> DurationHandleBeginDrag;
    public static event UnityAction<float> DurationHandleDrag;
    public static event UnityAction DurationHandleEndDrag;

    // GameObject is the anchor being dragged, not this.gameObject.
    public static event UnityAction<GameObject> AnchorRightClicked;
    public static event UnityAction<GameObject> AnchorBeginDrag;
    public static event UnityAction<Vector2> AnchorDrag;
    public static event UnityAction AnchorEndDrag;
    // GameObject is the control point being dragged, not
    // this.gameObject. int is control point index (0 or 1).
    public static event UnityAction<GameObject, int> ControlPointRightClicked;
    public static event UnityAction<GameObject, int> ControlPointBeginDrag;
    public static event UnityAction<Vector2> ControlPointDrag;
    public static event UnityAction ControlPointEndDrag;

    private void OnEnable()
    {
        PatternPanel.SelectionChanged += UpdateSelection;
        PatternPanel.KeysoundVisibilityChanged += SetKeysoundVisibility;
        resizeCursorState = 0;
    }

    private void OnDisable()
    {
        PatternPanel.SelectionChanged -= UpdateSelection;
        PatternPanel.KeysoundVisibilityChanged -= SetKeysoundVisibility;
    }

    public void UseHiddenSprite()
    {
        noteImage.GetComponent<Image>().sprite = hiddenSprite;
        if (durationTrail != null)
        {
            durationTrail.GetComponent<Image>().sprite = hiddenTrailSprite;
        }
        if (curvedImage != null)
        {
            curvedImage.sprite = hiddenCurveSprite;
        }
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
    private static int resizeCursorState;
    private void UseResizeCursor()
    {
        resizeCursorState++;
        if (resizeCursorState > 0)
        {
            UnityEngine.Cursor.SetCursor(horizontalResizeCursor,
                new Vector2(64f, 64f), CursorMode.Auto);
        }
    }

    private void UseDefaultCursor()
    {
        resizeCursorState--;
        if (resizeCursorState <= 0)
        {
            UnityEngine.Cursor.SetCursor(null,
                Vector2.zero, CursorMode.Auto);
        }
    }

    public void OnDurationHandlePointerEnter(BaseEventData eventData)
    {
        if (!(eventData is PointerEventData)) return;
        UseResizeCursor();
    }

    public void OnDurationHandlePointerExit(BaseEventData eventData)
    {
        if (!(eventData is PointerEventData)) return;
        UseDefaultCursor();
    }

    public void OnDurationHandleBeginDrag(BaseEventData eventData)
    {
        if (!(eventData is PointerEventData)) return;
        UseResizeCursor();
        DurationHandleBeginDrag?.Invoke(gameObject);
    }

    public void OnDurationHandleDrag(BaseEventData eventData)
    {
        if (!(eventData is PointerEventData)) return;
        PointerEventData pointerData = eventData as PointerEventData;
        DurationHandleDrag?.Invoke(pointerData.delta.x);
    }

    public void OnDurationHandleEndDrag(BaseEventData eventData)
    {
        if (!(eventData is PointerEventData)) return;
        UseDefaultCursor();
        DurationHandleEndDrag?.Invoke();
    }
    #endregion

    #region Event Relay From Curve
    /* Controls on new anchor receiver:
     * - Click: add anchor (TODO)
     * 
     * On anchor:
     * - Drag: move
     * - Right click: delete (TODO)
     * - Ctrl-drag: reset control points (symmetric)
     * 
     * On control point:
     * - Drag: move and rotate opposite point
     * - Right click: delete (TODO)
     * - Alt-drag: move without rotating opposite point
     */

    public void OnAnchorClick(BaseEventData eventData)
    {

    }

    public void OnAnchorBeginDrag(BaseEventData eventData)
    {
        if (!(eventData is PointerEventData)) return;
        PointerEventData pointerData = eventData as PointerEventData;
        AnchorBeginDrag?.Invoke(pointerData.pointerDrag);
    }

    public void OnAnchorDrag(BaseEventData eventData)
    {
        if (!(eventData is PointerEventData)) return;
        PointerEventData pointerData = eventData as PointerEventData;
        AnchorDrag?.Invoke(pointerData.delta);
    }

    public void OnAnchorEndDrag(BaseEventData eventData)
    {
        if (!(eventData is PointerEventData)) return;
        AnchorEndDrag?.Invoke();
    }

    public void OnControlPointClick(BaseEventData eventData)
    {
        if (!(eventData is PointerEventData)) return;
        PointerEventData pointerData = eventData as PointerEventData;
        if (pointerData.button != 
            PointerEventData.InputButton.Right) return;

        GameObject clicked = pointerData.pointerPress;
        DragNoteAnchor anchor = clicked
            .GetComponentInParent<DragNoteAnchor>();
        int controlPointIndex = anchor
            .GetControlPointIndex(clicked);
        ControlPointRightClicked?.Invoke(
            clicked, controlPointIndex);
    }

    public void OnControlPointBeginDrag(BaseEventData eventData)
    {
        if (!(eventData is PointerEventData)) return;
        PointerEventData pointerData = eventData as PointerEventData;

        GameObject dragging = pointerData.pointerDrag;
        DragNoteAnchor anchor = dragging
            .GetComponentInParent<DragNoteAnchor>();
        int controlPointIndex = anchor
            .GetControlPointIndex(dragging);
        ControlPointBeginDrag?.Invoke(dragging, controlPointIndex);
    }

    public void OnControlPointDrag(BaseEventData eventData)
    {
        if (!(eventData is PointerEventData)) return;
        PointerEventData pointerData = eventData as PointerEventData;
        ControlPointDrag?.Invoke(pointerData.delta);
    }

    public void OnControlPointEndDrag(BaseEventData eventData)
    {
        if (!(eventData is PointerEventData)) return;
        ControlPointEndDrag?.Invoke();
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

    public void RecordTrailActualLength()
    {
        trailActualLength = durationTrail.sizeDelta.x;
    }

    // During adjustment, this is the trail's "backend" length,
    // which may become negative; shown length is capped at 0.
    // Both lengths are only visual.
    private float trailActualLength;
    // Only visual; does not affect note duration.
    public void AdjustTrailLength(float delta)
    {
        trailActualLength += delta;
        float trailVisualLength = Mathf.Max(0f, trailActualLength);
        durationTrail.sizeDelta = new Vector2(trailVisualLength, 0f);
        invisibleTrail.sizeDelta = new Vector2(trailVisualLength, 0f);
        ToggleNoteImageTransparency();
    }

    private void ToggleNoteImageTransparency()
    {
        if (!transparentNoteImageWhenTrailTooShort) return;

        if (invisibleTrail.sizeDelta.x <
            GetComponent<RectTransform>().sizeDelta.x * 0.5f)
        {
            noteImage.GetComponent<Image>().color = new Color(1f, 1f, 1f, 0.38f);
        }
        else
        {
            noteImage.GetComponent<Image>().color = Color.white;
        }
    }
    #endregion

    #region Curve
    // All positions relative to note head.
    public List<Vector2> PointsOnCurve { get; private set; }

    // This does not render anchors and control points; call
    // ResetAnchorsAndControlPoints for that.
    public void ResetCurve()
    {
        DragNote dragNote = GetComponent<NoteObject>().note
            as DragNote;
        PointsOnCurve = new List<Vector2>();

        foreach (FloatPoint p in dragNote.Interpolate())
        {
            Vector2 pointOnCurve = new Vector2(
                p.pulse * PatternPanel.PulseWidth,
                -p.lane * PatternPanel.LaneHeight);
            PointsOnCurve.Add(pointOnCurve);
        }
        // TODO: do we need to smooth these points?

        // Rotate note head.
        float noteHeadAngle = Mathf.Atan2(
            PointsOnCurve[1].y - PointsOnCurve[0].y,
            PointsOnCurve[1].x - PointsOnCurve[0].x);
        noteImage.localRotation = Quaternion.Euler(0f, 0f,
            noteHeadAngle * Mathf.Rad2Deg);

        // Draw curve.
        curvedImage.SetVerticesDirty();

        // Draw new anchor receivers. Reuse them if applicable.
        for (int i = 0;
            i < newAnchorReceiverContainer.childCount;
            i++)
        {
            newAnchorReceiverContainer.GetChild(i).gameObject
                .SetActive(false);
        }
        if (newAnchorReceiverTemplate.transform.GetSiblingIndex()
            != 0)
        {
            newAnchorReceiverTemplate.transform.SetAsFirstSibling();
        }
        for (int i = 0; i < PointsOnCurve.Count - 1; i++)
        {
            int childIndex = i + 1;
            while (newAnchorReceiverContainer.childCount - 1
                < childIndex)
            {
                Instantiate(
                    newAnchorReceiverTemplate,
                    parent: newAnchorReceiverContainer);
            }
            RectTransform receiver = 
                newAnchorReceiverContainer.GetChild(childIndex)
                .GetComponent<RectTransform>();
            receiver.gameObject.SetActive(true);
            receiver.anchoredPosition = PointsOnCurve[i];

            Vector2 toNextPoint =
                PointsOnCurve[i + 1] - PointsOnCurve[i];
            receiver.sizeDelta = new Vector2(
                toNextPoint.magnitude, receiver.sizeDelta.y);
            float angle = Mathf.Atan2(toNextPoint.y,
                toNextPoint.x);
            receiver.localRotation = Quaternion.Euler(0f, 0f,
                angle * Mathf.Rad2Deg);
        }
    }

    public void ResetAllAnchorsAndControlPoints()
    {
        DragNote dragNote = GetComponent<NoteObject>().note
            as DragNote;

        for (int i = 0; i < anchorContainer.childCount; i++)
        {
            if (anchorContainer.GetChild(i).gameObject !=
                anchorTemplate)
            {
                Destroy(anchorContainer.GetChild(i).gameObject);
            }
        }
        for (int i = 0; i < dragNote.nodes.Count; i++)
        {
            DragNode dragNode = dragNote.nodes[i];

            GameObject anchor = Instantiate(anchorTemplate,
                parent: anchorContainer);
            anchor.SetActive(true);
            anchor.GetComponent<DragNoteAnchor>().anchorIndex = i;
            anchor.GetComponent<RectTransform>().anchoredPosition
                = new Vector2(
                    dragNode.anchor.pulse * PatternPanel.PulseWidth,
                    -dragNode.anchor.lane * PatternPanel.LaneHeight);

            for (int control = 0; control < 2; control++)
            {
                ResetControlPointPosition(dragNode,
                    anchor, control);
            }

            ResetPathsToControlPoints(
                anchor.GetComponent<DragNoteAnchor>());
        }
    }

    public void ResetControlPointPosition(DragNode dragNode,
        GameObject anchor,
        int index)
    {
        FloatPoint point = dragNode.GetControlPoint(index);
        Vector2 position = new Vector2(
            point.pulse * PatternPanel.PulseWidth,
            -point.lane * PatternPanel.LaneHeight);
        anchor.GetComponent<DragNoteAnchor>()
            .GetControlPoint(index)
            .GetComponent<RectTransform>()
            .anchoredPosition = position;
    }

    public void ResetPathsToControlPoints(DragNoteAnchor anchor)
    {
        for (int control = 0; control < 2; control++)
        {
            Vector2 position = anchor.GetControlPoint(control)
                .GetComponent<RectTransform>().anchoredPosition;
            RectTransform path = anchor
                .GetPathToControlPoint(control);
            path.sizeDelta = new Vector2(
                position.magnitude,
                path.sizeDelta.y);
            path.localRotation = Quaternion.Euler(0f, 0f,
                Mathf.Atan2(
                    position.y, position.x)
                * Mathf.Rad2Deg);
        }
    }
    #endregion
}

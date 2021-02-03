using System.Collections;
using System.Collections.Generic;
using TMPro;
using UnityEngine;
using UnityEngine.Events;
using UnityEngine.EventSystems;
using UnityEngine.UI;

public class NoteInEditor : MonoBehaviour, IPointsOnCurveProvider
{
    public Sprite hiddenSprite;
    public Sprite hiddenTrailSprite;
    public Image selectionOverlay;
    public RectTransform endOfScanIndicator;
    public RectTransform noteImage;

    public RectTransform pathToPreviousNote;
    public RectTransform durationTrail;
    public RectTransform invisibleTrail;
    public Texture2D horizontalResizeCursor;
    public bool transparentNoteImageWhenTrailTooShort;

    [Header("Drag Note")]
    public Sprite hiddenCurveSprite;
    public CurvedImage curvedImage;
    public RectTransform anchorReceiverContainer;
    public GameObject anchorReceiverTemplate;
    public RectTransform anchorContainer;
    public GameObject anchorTemplate;
    public Texture2D addAnchorCursor;

    public static event UnityAction<GameObject> LeftClicked;
    public static event UnityAction<GameObject> RightClicked;

    public static event UnityAction<GameObject> BeginDrag;
    public static event UnityAction<Vector2> Drag;
    public static event UnityAction EndDrag;

    public static event UnityAction<GameObject> DurationHandleBeginDrag;
    public static event UnityAction<float> DurationHandleDrag;
    public static event UnityAction DurationHandleEndDrag;

    public static event UnityAction<GameObject> AnchorReceiverClicked;
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
        PatternPanel.KeysoundVisibilityChanged += 
            UpdateKeysoundVisibility;
        resizeCursorState = 0;
    }

    private void OnDisable()
    {
        PatternPanel.SelectionChanged -= UpdateSelection;
        PatternPanel.KeysoundVisibilityChanged -=
            UpdateKeysoundVisibility;
    }

    public void UseHiddenSprite()
    {
        noteImage.GetComponent<Image>().sprite = hiddenSprite;
        if (durationTrail != null)
        {
            durationTrail.GetComponent<Image>().sprite = 
                hiddenTrailSprite;
        }
        if (curvedImage != null)
        {
            curvedImage.sprite = hiddenCurveSprite;
        }
    }

    private void UpdateSelection(HashSet<GameObject> selection)
    {
        bool selected = selection.Contains(gameObject);
        if (selectionOverlay != null)
        {
            selectionOverlay.enabled = selected;
        }
        if (anchorReceiverContainer != null)
        {
            anchorReceiverContainer.gameObject.SetActive(selected);
        }
        if (anchorContainer != null)
        {
            anchorContainer.gameObject.SetActive(selected);
        }
    }

    public void UpdateKeysoundVisibility()
    {
        GetComponentInChildren<TextMeshProUGUI>(includeInactive: true)
            .gameObject.SetActive(
            Options.instance.editorOptions.showKeysounds);
    }

    public void UpdateEndOfScanIndicator()
    {
        Note n = GetComponent<NoteObject>().note;
        bool showIndicator = n.endOfScan;
        if (showIndicator)
        {
            // Don't show indicator if the note is not on a
            // scan divider.
            int pulsesPerScan = Pattern.pulsesPerBeat *
                EditorContext.Pattern.patternMetadata.bps;
            if (n.pulse % pulsesPerScan != 0)
            {
                showIndicator = false;
            }
        }

        if (showIndicator)
        {
            endOfScanIndicator.sizeDelta = new Vector2(
                endOfScanIndicator.sizeDelta.y,
                endOfScanIndicator.sizeDelta.y);
            endOfScanIndicator.GetComponent<Image>().enabled = true;
        }
        else
        {
            endOfScanIndicator.sizeDelta = new Vector2(
                0f,
                endOfScanIndicator.sizeDelta.y);
            endOfScanIndicator.GetComponent<Image>().enabled = false;
        }
    }

    public void SetKeysoundText()
    {
        NoteObject noteObject = GetComponent<NoteObject>();
        GetComponentInChildren<TextMeshProUGUI>(includeInactive: true)
            .text = UIUtils.StripAudioExtension(
                noteObject.note.sound);
    }

    private void Update()
    {

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
        if ((eventData as PointerEventData).button
            != PointerEventData.InputButton.Left)
        {
            return;
        }
        BeginDrag?.Invoke(gameObject);
    }

    public void OnDrag(BaseEventData eventData)
    {
        if (!(eventData is PointerEventData)) return;
        PointerEventData pointerData = eventData as PointerEventData;
        if (pointerData.button
            != PointerEventData.InputButton.Left)
        {
            return;
        }
        Drag?.Invoke(pointerData.delta);
    }

    public void OnEndDrag(BaseEventData eventData)
    {
        if (!(eventData is PointerEventData)) return;
        if ((eventData as PointerEventData).button
            != PointerEventData.InputButton.Left)
        {
            return;
        }
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
    public void OnAnchorReceiverPointerEnter()
    {
        UnityEngine.Cursor.SetCursor(addAnchorCursor,
            Vector2.zero, CursorMode.Auto);
    }

    public void OnAnchorReceiverPointerExit()
    {
        UnityEngine.Cursor.SetCursor(null,
            Vector2.zero, CursorMode.Auto);
    }

    public void OnAnchorReceiverClick(BaseEventData eventData)
    {
        if (!(eventData is PointerEventData)) return;
        PointerEventData pointerData = eventData as PointerEventData;
        if (pointerData.button !=
            PointerEventData.InputButton.Left) return;

        AnchorReceiverClicked?.Invoke(gameObject);
    }

    public void OnAnchorClick(BaseEventData eventData)
    {
        if (!(eventData is PointerEventData)) return;
        PointerEventData pointerData = eventData as PointerEventData;
        if (pointerData.button !=
            PointerEventData.InputButton.Right) return;

        AnchorRightClicked?.Invoke(pointerData.pointerPress);
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
        if (target != null)
        {
            UIUtils.PointToward(self: pathToPreviousNote,
                selfPos: GetComponent<RectTransform>()
                    .anchoredPosition,
                targetPos: target.GetComponent<RectTransform>()
                    .anchoredPosition);
        }
        else
        {
            pathToPreviousNote.sizeDelta = Vector2.zero;
            pathToPreviousNote.localRotation = Quaternion.identity;
        }

        if (target != null &&
            target.GetComponent<NoteObject>().note.type
                == NoteType.ChainHead)
        {
            UIUtils.RotateToward(
                self: target.GetComponent<NoteInEditor>().noteImage,
                selfPos: target.GetComponent<RectTransform>()
                    .anchoredPosition,
                targetPos: GetComponent<RectTransform>()
                    .anchoredPosition);
        }
    }
    #endregion

    #region Duration Trail
    public void ResetTrail()
    {
        int duration = (GetComponent<NoteObject>().note as 
            HoldNote).duration;
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
            noteImage.GetComponent<Image>().color =
                new Color(1f, 1f, 1f, 0.38f);
        }
        else
        {
            noteImage.GetComponent<Image>().color = Color.white;
        }
    }
    #endregion

    #region Curve
    // All positions relative to note head.
    private List<Vector2> pointsOnCurve;

    public IList<Vector2> GetVisiblePointsOnCurve()
    {
        return pointsOnCurve;
    }

    // This does not render anchors and control points; call
    // ResetAnchorsAndControlPoints for that.
    public void ResetCurve()
    {
        DragNote dragNote = GetComponent<NoteObject>().note
            as DragNote;
        pointsOnCurve = new List<Vector2>();

        foreach (FloatPoint p in dragNote.Interpolate())
        {
            Vector2 pointOnCurve = new Vector2(
                p.pulse * PatternPanel.PulseWidth,
                -p.lane * PatternPanel.LaneHeight);
            pointsOnCurve.Add(pointOnCurve);
        }
        // TODO: do we need to smooth these points?

        // Rotate note head.
        UIUtils.RotateToward(self: noteImage,
            selfPos: pointsOnCurve[0],
            targetPos: pointsOnCurve[1]);

        // Draw curve.
        curvedImage.SetVerticesDirty();

        // Draw new anchor receivers. Reuse them if applicable.
        for (int i = 0;
            i < anchorReceiverContainer.childCount;
            i++)
        {
            anchorReceiverContainer.GetChild(i).gameObject
                .SetActive(false);
        }
        if (anchorReceiverTemplate.transform.GetSiblingIndex()
            != 0)
        {
            anchorReceiverTemplate.transform.SetAsFirstSibling();
        }
        for (int i = 0; i < pointsOnCurve.Count - 1; i++)
        {
            int childIndex = i + 1;
            while (anchorReceiverContainer.childCount - 1
                < childIndex)
            {
                Instantiate(
                    anchorReceiverTemplate,
                    parent: anchorReceiverContainer);
            }
            RectTransform receiver =
                anchorReceiverContainer.GetChild(childIndex)
                .GetComponent<RectTransform>();
            receiver.gameObject.SetActive(true);
            receiver.anchoredPosition = pointsOnCurve[i];

            UIUtils.PointToward(receiver,
                selfPos: pointsOnCurve[i],
                targetPos: pointsOnCurve[i + 1]);
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
            UIUtils.PointToward(path,
                selfPos: Vector2.zero,
                targetPos: position);
        }
    }

    public bool ClickLandsOnCurve(Vector3 screenPosition)
    {
        // Prune
        if (screenPosition.x < noteImage.transform.position.x
            - PatternPanel.LaneHeight * 0.5f)
        {
            return false;
        }
        if (screenPosition.x > noteImage.transform.position.x
            + pointsOnCurve[pointsOnCurve.Count - 1].x
            + PatternPanel.LaneHeight * 0.5f)
        {
            return false;
        }

        float minDistanceSquared = PatternPanel.LaneHeight * 
            PatternPanel.LaneHeight * 0.25f;
        foreach (Vector2 v in pointsOnCurve)
        {
            Vector2 distance = new Vector2(
                noteImage.transform.position.x + v.x 
                    - screenPosition.x,
                noteImage.transform.position.y + v.y
                    - screenPosition.y);
            float squareDistance = distance.sqrMagnitude;
            if (squareDistance <= minDistanceSquared)
            {
                return true;
            }
        }

        return false;
    }
    #endregion
}

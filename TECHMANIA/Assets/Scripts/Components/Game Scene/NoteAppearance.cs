using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

// Different from the editor, here the notes do not report click
// and drag events on themselves. Instead, the Game component
// performs ray tracing on all clicks and touches. This is so that
// when a held down click/touch enters another lane, Game can
// handle that has a new click/touch. This is necessary for chain
// notes.
public class NoteAppearance : MonoBehaviour,
    IPointsOnCurveProvider
{
    public enum State
    {
        // Note has not appeared yet; starting state.
        Inactive,
        // Some notes become transparent.
        Prepare,
        // Note is opaque and can be played.
        Active,
        // Exclusive to notes with a duration: note is
        // being played.
        Ongoing,
        // Exclusive to repeat heads: head is resolved, but
        // waiting for all managed repeat notes to also resolve.
        PendingResolve,
        // Note is resolved and no longer visible.
        Resolved
    }
    public State state { get; private set; }

    public enum Visibility
    {
        Hidden,
        Transparent,
        Visible
    }

    public Image noteImage;
    public GameObject feverOverlay;
    [Header("Chain")]
    public RectTransform pathToPreviousNote;
    [Header("Hold & Repeat Hold")]
    public RectTransform durationTrail;
    public RectTransform durationTrailEnd;
    public RectTransform ongoingTrail;
    public RectTransform ongoingTrailEnd;
    [Header("Drag")]
    public CurvedImage curve;
    public RectTransform curveEnd;
    [Header("Repeat")]
    public RectTransform pathToLastRepeatNote;

    private Image feverOverlayImage;
    private Animator feverOverlayAnimator;
    private bool hidden;
    private Scan scanRef;
    private Scanline scanlineRef;

    #region State Interfaces
    public void SetHidden(bool hidden)
    {
        this.hidden = hidden;
    }

    public void Prepare()
    {
        state = State.Prepare;
        UpdateState();
    }

    public void Activate()
    {
        state = State.Active;
        UpdateState();
    }

    public void SetOngoing()
    {
        state = State.Ongoing;
        UpdateState();
    }

    public void Resolve()
    {
        switch (GetNoteType())
        {
            case NoteType.RepeatHead:
            case NoteType.RepeatHeadHold:
                state = State.PendingResolve;
                // Only fully resolved when all managed repeat notes
                // get resolved.
                ManagedRepeatNoteResolved();
                break;
            case NoteType.Repeat:
            case NoteType.RepeatHold:
                state = State.Resolved;
                repeatHead.ManagedRepeatNoteResolved();
                break;
            default:
                state = State.Resolved;
                break;
        }
        UpdateState();
    }
    #endregion

    #region States
    private void SetNoteImageVisibility(Visibility v)
    {
        noteImage.gameObject.SetActive(v != Visibility.Hidden);
        noteImage.color = (v == Visibility.Transparent) ?
            new Color(1f, 1f, 1f, 0.6f) :
            Color.white;
    }

    private void SetFeverOverlayVisibility(Visibility v)
    {
        if (feverOverlayImage == null) return;
        feverOverlayImage.enabled = v != Visibility.Hidden;
    }

    private void SetPathToPreviousChainNodeVisibility(Visibility v)
    {
        if (pathToPreviousNote == null) return;
        pathToPreviousNote.gameObject.SetActive(
            v != Visibility.Hidden);
    }

    private void SetPathFromNextChainNodeVisibility(Visibility v)
    {
        if (nextChainNode == null) return;
        nextChainNode.GetComponent<NoteAppearance>()
            .SetPathToPreviousChainNodeVisibility(v);
    }

    private void SetDurationTrailVisibility(Visibility v)
    {
        if (durationTrail == null) return;
        durationTrail.gameObject.SetActive(v != Visibility.Hidden);
        if (ongoingTrail != null)
            ongoingTrail.gameObject.SetActive(
                v != Visibility.Hidden);
        Color color = (v == Visibility.Transparent) ?
            new Color(1f, 1f, 1f, 0.6f) :
            Color.white;
        durationTrail.GetComponent<Image>().color = color;
        if (ongoingTrail != null)
        {
            ongoingTrail.GetComponent<Image>().color = color;
        }
    }

    private void SetHoldExtensionVisibility(Visibility v)
    {
        if (holdExtensions == null) return;
        foreach (HoldExtension e in holdExtensions)
        {
            e.SetDurationTrailVisibility(v);
        }
    }

    private void SetCurveVisibility(Visibility v)
    {
        if (curve == null) return;
        curve.gameObject.SetActive(v != Visibility.Hidden);
        curve.color = (v == Visibility.Transparent) ?
            new Color(1f, 1f, 1f, 0.6f) :
            Color.white;
    }

    private void SetRepeatPathVisibility(Visibility v)
    {
        if (pathToLastRepeatNote == null) return;
        pathToLastRepeatNote.gameObject.SetActive(
            v != Visibility.Hidden);
    }

    private void SetRepeatPathExtensionVisibility(Visibility v)
    {
        if (repeatPathExtensions == null) return;
        foreach (RepeatPathExtension e in repeatPathExtensions)
        {
            e.SetExtensionVisibility(v);
        }
    }

    private void UpdateState()
    {
        // Is the note image visible and targetable?
        if (hidden)
        {
            SetNoteImageVisibility(Visibility.Hidden);
            SetFeverOverlayVisibility(Visibility.Hidden);
            SetPathToPreviousChainNodeVisibility(Visibility.Hidden);
            SetDurationTrailVisibility(Visibility.Hidden);
            SetHoldExtensionVisibility(Visibility.Hidden);
            SetCurveVisibility(Visibility.Hidden);
            SetRepeatPathVisibility(Visibility.Hidden);
            SetRepeatPathExtensionVisibility(Visibility.Hidden);
            return;
        }

        switch (state)
        {
            case State.Inactive:
            case State.Resolved:
                SetNoteImageVisibility(Visibility.Hidden);
                SetFeverOverlayVisibility(Visibility.Hidden);
                SetPathFromNextChainNodeVisibility(
                    Visibility.Hidden);
                SetDurationTrailVisibility(Visibility.Hidden);
                SetHoldExtensionVisibility(Visibility.Hidden);
                SetCurveVisibility(Visibility.Hidden);
                SetRepeatPathVisibility(Visibility.Hidden);
                SetRepeatPathExtensionVisibility(Visibility.Hidden);
                break;
            case State.Prepare:
                // Only the following should be transparent:
                // - Basic Note
                // - Trail of Hold Note
                // - Curve
                NoteType type = GetNoteType();
                if (type == NoteType.Basic)
                {
                    SetNoteImageVisibility(Visibility.Transparent);
                }
                else
                {
                    SetNoteImageVisibility(Visibility.Visible);
                }
                SetFeverOverlayVisibility(Visibility.Visible);
                SetPathFromNextChainNodeVisibility(
                    Visibility.Visible);
                if (type == NoteType.RepeatHeadHold ||
                    type == NoteType.RepeatHold)
                {
                    SetDurationTrailVisibility(Visibility.Visible);
                }
                else
                {
                    SetDurationTrailVisibility(
                        Visibility.Transparent);
                }
                SetCurveVisibility(Visibility.Transparent);
                SetRepeatPathVisibility(Visibility.Visible);
                // Not set for extensions: these will be controlled
                // by the scan they belong to.
                break;
            case State.Active:
            case State.Ongoing:
                if (GetNoteType() == NoteType.RepeatHold)
                {
                    // TODO: look into why these images disappear before note enters ongoing state.
                    SetNoteImageVisibility(Visibility.Hidden);
                }
                else
                {
                    SetNoteImageVisibility(Visibility.Visible);
                }
                SetFeverOverlayVisibility(Visibility.Visible);
                SetPathFromNextChainNodeVisibility(
                    Visibility.Visible);
                SetDurationTrailVisibility(Visibility.Visible);
                SetCurveVisibility(Visibility.Visible);
                SetRepeatPathVisibility(Visibility.Visible);
                // Not set for extensions: these will be controlled
                // by the scan they belong to.
                break;
            case State.PendingResolve:
                SetNoteImageVisibility(Visibility.Visible);
                SetFeverOverlayVisibility(Visibility.Visible);
                SetDurationTrailVisibility(Visibility.Hidden);
                SetRepeatPathVisibility(Visibility.Visible);
                break;
        }
    }
    #endregion

    private void Start()
    {
        if (feverOverlay != null)
        {
            feverOverlayAnimator =
                feverOverlay.GetComponent<Animator>();
            feverOverlayImage = feverOverlay.GetComponent<Image>();
        }

        state = State.Inactive;
        UpdateState();
    }

    public void SetScanAndScanlineRef(Scan scan, Scanline scanline)
    {
        scanRef = scan;
        scanlineRef = scanline;
    }

    #region Update
    private void Update()
    {
        if (hidden) return;
        if (state == State.Inactive || state == State.Resolved) return;

        if (feverOverlay != null)
        {
            UpdateFeverOverlay();
        }
        if (state == State.Ongoing)
        {
            if (durationTrail != null)
            {
                UpdateOngoingTrail();
            }
            if (curve != null)
            {
                UpdateOngoingCurve();
                PlaceNoteImageOnCurve();
            }
        }
    }

    private void UpdateFeverOverlay()
    {
        if (Game.feverState == Game.FeverState.Active)
        {
            if (!feverOverlayAnimator.enabled)
            {
                feverOverlayAnimator.enabled = true;
                feverOverlayImage.color = Color.white;
            }
            else if (Game.feverAmount < 0.1f)
            {
                feverOverlayImage.color = new Color(
                    1f, 1f, 1f, Game.feverAmount * 10f);
            }
        }
        else
        {
            if (feverOverlayAnimator.enabled)
            {
                feverOverlayAnimator.enabled = false;
                feverOverlayImage.color = Color.clear;
            }
        }
    }
    #endregion

    private NoteType GetNoteType()
    {
        return GetComponent<NoteObject>().note.type;
    }

    #region Path
    // A little complication here is that, to achieve the correct
    // draw order, each Chain Node draws a path to its previous
    // Chain Head/Node, the same way as in the editor.
    // However, when a Chain Head/Node gets resolved, it should
    // also take away the path pointing to it. Therefore, it's
    // necessary for each Chain Head/Node to be aware of, and
    // eventually control, the next Chain Node.
    private GameObject nextChainNode;
    public void SetNextChainNode(NoteObject nextChainNode)
    {
        this.nextChainNode = null;
        if (nextChainNode != null)
        {
            this.nextChainNode = nextChainNode.gameObject;
            nextChainNode.GetComponent<NoteAppearance>()
                .PointPathTowards(GetComponent<RectTransform>());
            if (GetNoteType() == NoteType.ChainHead)
            {
                UIUtils.RotateToward(
                    noteImage.GetComponent<RectTransform>(),
                    selfPos: GetComponent<RectTransform>()
                        .anchoredPosition,
                    targetPos: nextChainNode
                        .GetComponent<RectTransform>()
                        .anchoredPosition);
            }
        }
    }

    private void PointPathTowards(RectTransform previousNote)
    {
        if (pathToPreviousNote == null) return;
        UIUtils.PointToward(pathToPreviousNote,
            selfPos: GetComponent<RectTransform>().anchoredPosition,
            targetPos: previousNote
                .GetComponent<RectTransform>().anchoredPosition);
    }
    #endregion

    #region Trail
    private List<HoldExtension> holdExtensions;
    private float durationTrailInitialWidth;
    private bool trailExtendsLeft;
    public void InitializeTrail()
    {
        holdExtensions = new List<HoldExtension>();

        HoldNote holdNote = GetComponent<NoteObject>().note
            as HoldNote;
        float startX = GetComponent<RectTransform>()
            .anchoredPosition.x;
        float endX = scanRef.FloatPulseToXPosition(
            holdNote.pulse + holdNote.duration,
            positionEndOfScanOutOfBounds: false,
            positionAfterScanOutOfBounds: true);
        trailExtendsLeft = endX < startX;
        durationTrailInitialWidth = Mathf.Abs(startX - endX);

        // Both trails (if existant) are anchored at 0
        // and extend right.
        durationTrail.sizeDelta = new Vector2(
            durationTrailInitialWidth,
            durationTrail.sizeDelta.y);
        
        // TODO: after fixing stuff, copy to HoldExtension.
        if (trailExtendsLeft)
        {
            durationTrail.localRotation =
                Quaternion.Euler(0f, 0f, 180f);
            if (ongoingTrail != null)
            {
                ongoingTrail.localRotation =
                    Quaternion.Euler(0f, 0f, 180f);
            }
        }
        if (ongoingTrail != null)
        {
            ongoingTrail.sizeDelta = new Vector2(0f,
                ongoingTrail.sizeDelta.y);
        }
    }

    public void RegisterHoldExtension(HoldExtension e)
    {
        holdExtensions.Add(e);
        e.RegisterNoteAppearance(this);
    }

    private void UpdateOngoingTrail()
    {
        float startX = GetComponent<RectTransform>()
            .anchoredPosition.x;
        float endX = scanlineRef.GetComponent<RectTransform>()
            .anchoredPosition.x;
        float width = Mathf.Min(Mathf.Abs(startX - endX),
            durationTrailInitialWidth);

        // Override width to 0 if the scanline is on the wrong side.
        float durationTrailDirection = 
            durationTrailEnd.transform.position.x -
            transform.position.x;
        float scanlineDirection =
            scanlineRef.transform.position.x -
            transform.position.x;
        if (durationTrailDirection * scanlineDirection < 0f)
        {
            width = 0f;
        }

        // TODO: after fixing stuff, copy to HoldExtension.
        if (GetNoteType() == NoteType.Hold)
        {
            // For hold notes, the duration trail stays still,
            // and the ongoing trail extends to the right.
            ongoingTrail.sizeDelta = new Vector2(width,
                ongoingTrail.sizeDelta.y);
        }
        else
        {
            // For repeat holds, the duration trail contracts to
            // the right, and the ongoing trail does not exist.
            durationTrail.anchoredPosition = new Vector2(
                trailExtendsLeft ? -width : width,
                0f);
            durationTrail.sizeDelta = new Vector2(
                durationTrailInitialWidth - width,
                durationTrail.sizeDelta.y);
        }

        foreach (HoldExtension e in holdExtensions)
        {
            e.UpdateOngoingTrail();
        }
    }

    // VFXSpawner calls this to draw completion VFX at the correct
    // position.
    public Vector3 GetDurationTrailEndPosition()
    {
        if (holdExtensions.Count > 0)
        {
            return holdExtensions[holdExtensions.Count - 1]
                .durationTrailEnd.position;
        }
        return durationTrailEnd.position;
    }

    // VFXSpawner calls this to draw ongoing VFX at the correct
    // position.
    public Vector3 GetOngoingTrailEndPosition()
    {
        if (holdExtensions.Count == 0 ||
            Game.Scan == scanRef.scanNumber)
        {
            return ongoingTrailEnd.position;
        }
        else
        {
            int extensionIndex = Game.Scan - scanRef.scanNumber - 1;
            if (extensionIndex > holdExtensions.Count - 1)
            {
                extensionIndex = holdExtensions.Count - 1;
            }
            return holdExtensions[extensionIndex]
                .ongoingTrailEnd.position;
        }
    }
    #endregion

    #region Curve
    // All positions relative to note head.
    private ListView<Vector2> pointsOnCurve;
    private float curveXDirection;

    public IList<Vector2> GetPointsOnCurve()
    {
        return pointsOnCurve;
    }

    public void InitializeCurve()
    {
        DragNote dragNote = GetComponent<NoteObject>().note
            as DragNote;
        pointsOnCurve = new ListView<Vector2>();

        Vector2 headPosition = GetComponent<RectTransform>()
            .anchoredPosition;
        foreach (FloatPoint p in dragNote.Interpolate())
        {
            Vector2 pointOnCurve = new Vector2(
                scanRef.FloatPulseToXPosition(
                    dragNote.pulse + p.pulse)
                - headPosition.x,
                scanRef.FloatLaneToYPosition(
                    dragNote.lane + p.lane)
                - headPosition.y);
            pointsOnCurve.Add(pointOnCurve);
        }

        curveEnd.anchoredPosition =
            pointsOnCurve[pointsOnCurve.Count - 1];
        curveXDirection = Mathf.Sign(
            pointsOnCurve[pointsOnCurve.Count - 1].x
            - pointsOnCurve[0].x);
        PlaceNoteImageOnCurve();
        curve.SetVerticesDirty();
    }

    public void UpdateOngoingCurve()
    {
        if (pointsOnCurve.Count < 2)
        {
            return;
        }
        float scanlineX = scanlineRef
            .GetComponent<RectTransform>().anchoredPosition.x -
            GetComponent<RectTransform>().anchoredPosition.x;
        // Make sure scanline is before pointsOnCurve[1]; remove
        // points if necessary.
        while ((scanlineX - pointsOnCurve[1].x) * curveXDirection
            >= 0f)
        {
            if (pointsOnCurve.Count < 3) break;
            pointsOnCurve.RemoveFirst();
        }
        // Interpolate pointsOnCurve[0] and pointsOnCurve[1].
        float t = (scanlineX - pointsOnCurve[0].x) /
            (pointsOnCurve[1].x - pointsOnCurve[0].x);
        pointsOnCurve[0] = new Vector2(
            scanlineX,
            Mathf.Lerp(pointsOnCurve[0].y, pointsOnCurve[1].y, t));
    }

    public void PlaceNoteImageOnCurve()
    {
        RectTransform imageRect = noteImage
            .GetComponent<RectTransform>();
        imageRect.anchoredPosition = pointsOnCurve[0];
        if (pointsOnCurve.Count > 1)
        {
            UIUtils.PointToward(imageRect,
                selfPos: pointsOnCurve[0],
                targetPos: pointsOnCurve[1]);
        }
    }

    public Vector3 GetCurveEndPosition()
    {
        return curveEnd.position;
    }
    #endregion

    #region Repeat
    // Repeat heads and repeat hold heads store references to
    // all repeat notes and repeat hold notes after it.
    private List<NoteObject> managedRepeatNotes;
    // Counting backwards because notes are drawn backwards.
    // A value equal to managedRepeatNotes.Count means
    // the head itself.
    private int nextUnresolvedRepeatNoteIndex;
    // Repeat notes and repeat hold notes store references to
    // the repeat head or repeat hold head before it.
    private NoteAppearance repeatHead;
    private List<RepeatPathExtension> repeatPathExtensions;

    public void ManageRepeatNotes(List<NoteObject> repeatNotes)
    {
        // Clone the list because it will be cleared later.
        managedRepeatNotes = new List<NoteObject>(repeatNotes);
        foreach (NoteObject n in managedRepeatNotes)
        {
            n.GetComponent<NoteAppearance>().repeatHead
                = this;
        }
        nextUnresolvedRepeatNoteIndex = managedRepeatNotes.Count;
    }

    public NoteAppearance GetRepeatHead()
    {
        return repeatHead;
    }

    public NoteObject GetFirstUnresolvedRepeatNote()
    {
        if (nextUnresolvedRepeatNoteIndex == 
            managedRepeatNotes.Count)
        {
            return GetComponent<NoteObject>();
        }
        else
        {
            return managedRepeatNotes
                [nextUnresolvedRepeatNoteIndex];
        }
    }

    private void ManagedRepeatNoteResolved()
    {
        nextUnresolvedRepeatNoteIndex--;
        if (nextUnresolvedRepeatNoteIndex < 0)
        {
            state = State.Resolved;
            UpdateState();
        }
    }

    public void DrawRepeatHeadBeforeRepeatNotes()
    {
        // Since notes are drawn from back to front, we look
        // for the 1st note in the same scan, and draw
        // before that one.
        foreach (NoteObject n in managedRepeatNotes)
        {
            if (n.transform.parent == transform.parent)
            {
                transform.SetSiblingIndex(
                    n.transform.GetSiblingIndex());
                return;
            }
        }
    }

    public void DrawRepeatPathTo(NoteObject lastRepeatNote)
    {
        float startX = GetComponent<RectTransform>()
            .anchoredPosition.x;
        float endX = scanRef.FloatPulseToXPosition(
            lastRepeatNote.note.pulse,
            positionEndOfScanOutOfBounds: true,
            positionAfterScanOutOfBounds: true);
        float width = Mathf.Abs(startX - endX);

        pathToLastRepeatNote.sizeDelta = new Vector2(width,
            pathToLastRepeatNote.sizeDelta.y);
        if (endX < startX)
        {
            pathToLastRepeatNote.localRotation =
                Quaternion.Euler(0f, 0f, 180f);
        }

        repeatPathExtensions = new List<RepeatPathExtension>();
    }

    public void RegisterRepeatPathExtension(
        RepeatPathExtension extension)
    {
        repeatPathExtensions.Add(extension);
    }

    #endregion
}

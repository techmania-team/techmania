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
        Inactive,  // Note has not appeared yet; starting state
        Prepare,  // Note is 50% transparent
        Active,  // Note is opaque and can be played
        Ongoing,  // Note with a duration is being played
        Resolved  // Note is resolved and no longer visible
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
    [Header("Hold")]
    public RectTransform durationTrail;
    public RectTransform durationTrailEnd;
    public RectTransform ongoingTrail;
    public RectTransform ongoingTrailEnd;
    [Header("Drag")]
    public CurvedImage curve;
    public RectTransform curveEnd;

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
        state = State.Resolved;
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
        ongoingTrail.gameObject.SetActive(v != Visibility.Hidden);
        Color color = (v == Visibility.Transparent) ?
            new Color(1f, 1f, 1f, 0.6f) :
            Color.white;
        durationTrail.GetComponent<Image>().color = color;
        ongoingTrail.GetComponent<Image>().color = color;
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
                break;
            case State.Prepare:
                // Only the following should be transparent:
                // - Basic Note
                // - Trail of Hold Note
                // - Curve
                if (GetNoteType() == NoteType.Basic)
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
                SetDurationTrailVisibility(Visibility.Transparent);
                SetCurveVisibility(Visibility.Transparent);
                // Not set for extensions: these will be controlled
                // by the scan they belong to.
                break;
            case State.Active:
            case State.Ongoing:
                SetNoteImageVisibility(Visibility.Visible);
                SetFeverOverlayVisibility(Visibility.Visible);
                SetPathFromNextChainNodeVisibility(
                    Visibility.Visible);
                SetDurationTrailVisibility(Visibility.Visible);
                SetCurveVisibility(Visibility.Visible);
                // Not set for extensions: these will be controlled
                // by the scan they belong to.
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
            if (ongoingTrail != null)
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
    public void InitializeTrail(Scan scanRef, Scanline scanlineRef)
    {
        this.scanRef = scanRef;
        this.scanlineRef = scanlineRef;
        holdExtensions = new List<HoldExtension>();

        HoldNote holdNote = GetComponent<NoteObject>().note
            as HoldNote;
        float startX = GetComponent<RectTransform>()
            .anchoredPosition.x;
        float endX = scanRef.FloatPulseToXPosition(
            holdNote.pulse + holdNote.duration,
            extendOutOfBoundPosition: true);
        float width = Mathf.Abs(startX - endX);

        durationTrail.sizeDelta = new Vector2(width,
            durationTrail.sizeDelta.y);
        if (endX < startX)
        {
            durationTrail.localRotation =
                Quaternion.Euler(0f, 0f, 180f);
            ongoingTrail.localRotation =
                Quaternion.Euler(0f, 0f, 180f);
        }
        ongoingTrail.sizeDelta = new Vector2(0f,
            ongoingTrail.sizeDelta.y);
    }

    public void RegisterExtension(HoldExtension e)
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
            durationTrail.sizeDelta.x);

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

        ongoingTrail.sizeDelta = new Vector2(width,
            ongoingTrail.sizeDelta.y);

        foreach (HoldExtension e in holdExtensions)
        {
            e.UpdateOngoingTrail();
        }
    }

    // VFXSpawner calls this to draw ongoing VFX at the correct
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

    #region
    // All positions relative to note head.
    private ListView<Vector2> pointsOnCurve;
    private float curveXDirection;

    public IList<Vector2> GetPointsOnCurve()
    {
        return pointsOnCurve;
    }

    public void InitializeCurve(Scan scanRef, Scanline scanlineRef)
    {
        this.scanRef = scanRef;
        this.scanlineRef = scanlineRef;

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
}

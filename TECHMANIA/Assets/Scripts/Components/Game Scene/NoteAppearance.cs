using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

// Different from the editor, here the notes do not report click
// and drag events on themselves. Instead, the Game component
// performs ray tracing on all clicks and touches. This is so that
// when a held down click/touch enters another lane, Game can
// handle that as a new click/touch. This is necessary for chain
// notes.
public class NoteAppearance : MonoBehaviour,
    IPointsOnCurveProvider
{
    public enum State
    {
        // Note has not appeared yet; starting state.
        Inactive,
        // Some notes become transparent, other opaque.
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
    public RectTransform hitbox;
    [Header("Chain")]
    public RectTransform pathToPreviousNote;
    [Header("Drag")]
    public CurvedImage curve;
    public RectTransform curveEnd;
    [Header("Repeat")]
    public RectTransform pathToLastRepeatNote;

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
        InitializeHitbox();
    }

    public void Activate()
    {
        if (state == State.Resolved)
        {
            // Do nothing.
            return;
        }
        state = State.Active;
        UpdateState();
    }

    public void SetOngoing()
    {
        state = State.Ongoing;
        UpdateState();
        if (GetNoteType() == NoteType.Drag)
        {
            SetHitboxSize(Ruleset.instance.ongoingDragHitboxWidth,
                Ruleset.instance.ongoingDragHitboxHeight);
        }
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
        if (hitbox != null)
        {
            hitbox.gameObject.SetActive(v != Visibility.Hidden);
        }
        noteImage.color = (v == Visibility.Transparent) ?
            new Color(1f, 1f, 1f, 0.6f) :
            Color.white;
    }

    private void SetFeverOverlayVisibility(Visibility v)
    {
        if (feverOverlay == null) return;
        feverOverlay.GetComponent<Image>().enabled =
            v != Visibility.Hidden;
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
        HoldTrailManager holdTrailManager = 
            GetComponent<HoldTrailManager>();
        if (holdTrailManager == null) return;
        holdTrailManager.SetVisibility(v);
    }

    private void SetHoldExtensionVisibility(Visibility v)
    {
        if (holdExtensions == null) return;
        foreach (HoldExtension e in holdExtensions)
        {
            e.SetVisibility(v);
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
                SetNoteImageVisibility(Visibility.Visible);
                SetFeverOverlayVisibility(Visibility.Visible);
                SetPathFromNextChainNodeVisibility(
                    Visibility.Visible);
                SetDurationTrailVisibility(Visibility.Visible);
                SetCurveVisibility(Visibility.Visible);
                SetRepeatPathVisibility(Visibility.Visible);
                // Not set for extensions: these will be controlled
                // by the scan they belong to.
                break;
            case State.Ongoing:
                if (GetNoteType() == NoteType.RepeatHold)
                {
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
                SetHoldExtensionVisibility(Visibility.Hidden);
                SetRepeatPathVisibility(Visibility.Visible);
                break;
        }
    }
    #endregion

    private void Start()
    {
        state = State.Inactive;
        UpdateState();
    }

    private void OnEnable()
    {
        if (hitbox != null)
        {
            Game.HitboxVisibilityChanged += OnHitboxVisibilityChanged;
        }
    }

    private void OnDisable()
    {
        if (hitbox != null)
        {
            Game.HitboxVisibilityChanged -= OnHitboxVisibilityChanged;
        }
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

        UpdateSprites();
        if (GetComponent<HoldTrailManager>() != null)
        {
            // Do this in all visible states because it updates
            // sprites.
            UpdateOngoingTrail();
        }
        if (repeatPathExtensions != null)
        {
            foreach (RepeatPathExtension extension in
                repeatPathExtensions)
            {
                extension.UpdateSprites();
            }
        }
        if (state == State.Ongoing)
        {
            if (curve != null)
            {
                UpdateOngoingCurve();
                PlaceNoteImageAndHitboxOnCurve();
            }
        }
    }
    #endregion

    #region Note skin
    public void InitializeScale()
    {
        float noteImageScaleX = 1f;
        float noteImageScaleY = 1f;
        switch (GetNoteType())
        {
            case NoteType.Basic:
                noteImageScaleX = GlobalResource.noteSkin.basic.scale;
                noteImageScaleY = GlobalResource.noteSkin.basic.scale;
                break;
            case NoteType.ChainHead:
                noteImageScaleX = GlobalResource.noteSkin.
                    chainHead.scale;
                noteImageScaleY = GlobalResource.noteSkin.
                    chainHead.scale;
                break;
            case NoteType.ChainNode:
                noteImageScaleX = GlobalResource.noteSkin.
                    chainNode.scale;
                noteImageScaleY = GlobalResource.noteSkin.
                    chainNode.scale;
                pathToPreviousNote.localScale = new Vector3(1f,
                    GlobalResource.noteSkin.chainPath.scale,
                    1f);
                break;
            case NoteType.Drag:
                noteImageScaleX = GlobalResource.noteSkin.
                    dragHead.scale;
                noteImageScaleY = GlobalResource.noteSkin.
                    dragHead.scale;
                curve.scale = GlobalResource.noteSkin.dragCurve.scale;
                break;
            case NoteType.Hold:
                noteImageScaleX = GlobalResource.noteSkin.
                    holdHead.scale;
                noteImageScaleY = GlobalResource.noteSkin.
                    holdHead.scale;
                break;
            case NoteType.RepeatHead:
            case NoteType.RepeatHeadHold:
                noteImageScaleX = GlobalResource.noteSkin.
                    repeatHead.scale;
                noteImageScaleY = GlobalResource.noteSkin.
                    repeatHead.scale;
                pathToLastRepeatNote.localScale = new Vector3(
                    pathToLastRepeatNote.localScale.x,
                    GlobalResource.noteSkin.repeatPath.scale,
                    1f);
                break;
            case NoteType.Repeat:
            case NoteType.RepeatHold:
                noteImageScaleX = GlobalResource.noteSkin.
                    repeat.scale;
                noteImageScaleY = GlobalResource.noteSkin.
                    repeat.scale;
                break;
        }
        noteImage.transform.localScale = new Vector3(
            noteImageScaleX, noteImageScaleY, 1f);
    }

    private void UpdateSprites()
    {
        switch (GetNoteType())
        {
            case NoteType.Basic:
                noteImage.sprite = GlobalResource.noteSkin.basic
                    .GetSpriteForFloatBeat(Game.FloatBeat);
                break;
            case NoteType.ChainHead:
                noteImage.sprite = GlobalResource.noteSkin.chainHead
                    .GetSpriteForFloatBeat(Game.FloatBeat);
                break;
            case NoteType.ChainNode:
                noteImage.sprite = GlobalResource.noteSkin.chainNode
                    .GetSpriteForFloatBeat(Game.FloatBeat);
                pathToPreviousNote.GetComponent<Image>().sprite =
                    GlobalResource.noteSkin.chainPath
                    .GetSpriteForFloatBeat(Game.FloatBeat);
                break;
            case NoteType.Drag:
                noteImage.sprite = GlobalResource.noteSkin.dragHead
                    .GetSpriteForFloatBeat(Game.FloatBeat);
                curve.sprite = GlobalResource.noteSkin.dragCurve
                    .GetSpriteForFloatBeat(Game.FloatBeat);
                break;
            case NoteType.Hold:
                noteImage.sprite = GlobalResource.noteSkin.holdHead
                    .GetSpriteForFloatBeat(Game.FloatBeat);
                break;
            case NoteType.RepeatHead:
            case NoteType.RepeatHeadHold:
                noteImage.sprite = GlobalResource.noteSkin.repeatHead
                    .GetSpriteForFloatBeat(Game.FloatBeat);
                pathToLastRepeatNote.GetComponent<Image>().sprite =
                    GlobalResource.noteSkin.repeatPath
                    .GetSpriteForFloatBeat(Game.FloatBeat);
                break;
            case NoteType.Repeat:
            case NoteType.RepeatHold:
                noteImage.sprite = GlobalResource.noteSkin.repeat
                    .GetSpriteForFloatBeat(Game.FloatBeat);
                break;
        }
    }
    #endregion

    public NoteType GetNoteType()
    {
        return GetComponent<NoteObject>().note.type;
    }

    #region Hitbox
    public void InitializeHitbox()
    {
        if (hitbox == null) return;

        float hitboxWidth;
        switch (GetNoteType())
        {
            case NoteType.ChainHead:
                hitboxWidth = Ruleset.instance.chainHeadHitboxWidth;
                break;
            case NoteType.ChainNode:
                hitboxWidth = Ruleset.instance.chainNodeHitboxWidth;
                break;
            default:
                hitboxWidth = Ruleset.instance.hitboxWidth;
                break;
        }
        SetHitboxSize(hitboxWidth, 1f);
    }

    private void SetHitboxSize(float width, float height)
    {
        if (hitbox == null) return;

        hitbox.anchorMin = new Vector2(0.5f - width * 0.5f,
            0.5f - height * 0.5f);
        hitbox.anchorMax = new Vector2(0.5f + width * 0.5f,
            0.5f + height * 0.5f);
    }

    private void OnHitboxVisibilityChanged(bool visible)
    {
        if (hitbox == null) return;

        Image hitboxImage = hitbox.GetComponent<Image>();
        hitboxImage.color = new Color(
            hitboxImage.color.r,
            hitboxImage.color.g,
            hitboxImage.color.b,
            visible ? 1f : 0f);
    }
    #endregion

    #region Path
    // A little complication here is that, to achieve the correct
    // draw order, each Chain Node draws a path to its previous
    // Chain Head/Node, the same way as in the editor.
    // However, when a Chain Head/Node gets resolved, it should
    // also take away the path pointing to it. Therefore, it's
    // necessary for each Chain Head/Node to be aware of, and
    // eventually control, the next Chain Node.
    private GameObject nextChainNode;
    public void SetNextChainNode(GameObject nextChainNode)
    {
        this.nextChainNode = null;
        if (nextChainNode != null)
        {
            this.nextChainNode = nextChainNode;
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

    public void InitializeTrail()
    {
        holdExtensions = new List<HoldExtension>();
        HoldNote holdNote = GetComponent<NoteObject>().note
            as HoldNote;

        GetComponent<HoldTrailManager>().Initialize(
            scanRef, scanlineRef, holdNote);
    }

    public void RegisterHoldExtension(HoldExtension e)
    {
        holdExtensions.Add(e);
        e.RegisterNoteAppearance(this);
    }

    private void UpdateOngoingTrail()
    {
        GetComponent<HoldTrailManager>().UpdateTrails();

        foreach (HoldExtension e in holdExtensions)
        {
            e.UpdateTrails();
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
        return GetComponent<HoldTrailManager>()
            .durationTrailEnd.position;
    }

    // VFXSpawner calls this to draw ongoing VFX at the correct
    // position.
    public Vector3 GetOngoingTrailEndPosition()
    {
        if (holdExtensions.Count == 0 ||
            Game.Scan == scanRef.scanNumber)
        {
            return GetComponent<HoldTrailManager>()
                .ongoingTrailEnd.position;
        }
        else
        {
            int extensionIndex = Game.Scan - scanRef.scanNumber - 1;
            extensionIndex = Mathf.Clamp(extensionIndex,
                0, holdExtensions.Count - 1);
            return holdExtensions[extensionIndex]
                .ongoingTrailEnd.position;
        }
    }
    #endregion

    #region Curve
    // All positions relative to note head.
    private ListView<Vector2> visiblePointsOnCurve;
    private List<Vector2> pointsOnCurve;
    private float curveXDirection;
    private float inputLatency;

    public IList<Vector2> GetVisiblePointsOnCurve()
    {
        return visiblePointsOnCurve;
    }

    public void SetInputLatency(float latency)
    {
        inputLatency = latency;
    }

    public void InitializeCurve()
    {
        DragNote dragNote = GetComponent<NoteObject>().note
            as DragNote;
        visiblePointsOnCurve = new ListView<Vector2>();
        pointsOnCurve = new List<Vector2>();

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
            visiblePointsOnCurve.Add(pointOnCurve);
            pointsOnCurve.Add(pointOnCurve);
        }

        curveEnd.anchoredPosition =
            visiblePointsOnCurve[visiblePointsOnCurve.Count - 1];
        curveXDirection = Mathf.Sign(
            visiblePointsOnCurve[visiblePointsOnCurve.Count - 1].x
            - visiblePointsOnCurve[0].x);
        curve.SetVerticesDirty();

        noteImage.rectTransform.anchoredPosition = Vector2.zero;
        feverOverlay.GetComponent<RectTransform>().anchoredPosition =
            Vector2.zero;
        hitbox.anchoredPosition = Vector2.zero;
        UIUtils.RotateToward(noteImage.rectTransform,
                selfPos: pointsOnCurve[0],
                targetPos: pointsOnCurve[1]);
    }

    public void UpdateOngoingCurve()
    {
        if (visiblePointsOnCurve.Count < 2)
        {
            return;
        }
        float scanlineX = scanlineRef
            .GetComponent<RectTransform>().anchoredPosition.x -
            GetComponent<RectTransform>().anchoredPosition.x;
        // Make sure scanline is before pointsOnCurve[1]; remove
        // points if necessary.
        while ((scanlineX - visiblePointsOnCurve[1].x)
            * curveXDirection >= 0f)
        {
            if (visiblePointsOnCurve.Count < 3) break;
            visiblePointsOnCurve.RemoveFirst();
        }
        // Interpolate visiblePointsOnCurve[0] and
        // visiblePointsOnCurve[1].
        float t = (scanlineX - visiblePointsOnCurve[0].x) /
            (visiblePointsOnCurve[1].x - visiblePointsOnCurve[0].x);
        visiblePointsOnCurve[0] = Vector2.Lerp(
            visiblePointsOnCurve[0],
            visiblePointsOnCurve[1], t);
        curve.SetVerticesDirty();
    }

    public void PlaceNoteImageAndHitboxOnCurve()
    {
        RectTransform imageRect = noteImage
            .GetComponent<RectTransform>();
        imageRect.anchoredPosition = visiblePointsOnCurve[0];
        feverOverlay.GetComponent<RectTransform>()
            .anchoredPosition = visiblePointsOnCurve[0];
        if (visiblePointsOnCurve.Count > 1)
        {
            UIUtils.RotateToward(imageRect,
                selfPos: visiblePointsOnCurve[0],
                targetPos: visiblePointsOnCurve[1]);
        }

        // To calculate the hitbox's position, we need to compensate
        // for latency.
        float compensatedPulse = GameSetup.pattern.TimeToPulse(
            Game.Time - inputLatency);
        float compensatedScanlineX = scanRef.FloatPulseToXPosition(
            compensatedPulse) -
            GetComponent<RectTransform>().anchoredPosition.x;
        int pointIndexAfterHitbox = -1;
        // Find the first point after the compensated scanline's
        // position.
        for (int i = 0; i < pointsOnCurve.Count; i++)
        {
            if ((pointsOnCurve[i].x - compensatedScanlineX) *
                curveXDirection >= 0f)
            {
                pointIndexAfterHitbox = i;
                break;
            }
        }
        if (pointIndexAfterHitbox < 0)
        {
            // All points are before the compensated scanline.
            pointIndexAfterHitbox = pointsOnCurve.Count - 1;
        }
        else if (pointIndexAfterHitbox == 0)
        {
            // All points are after the compensated scanline.
            pointIndexAfterHitbox = 1;
        }
        // Interpolate pointsOnCurve[pointIndexAfterHitbox - 1]
        // and pointsOnCurve[pointIndexAfterHitbox].
        Vector2 pointBeforeHitbox =
            pointsOnCurve[pointIndexAfterHitbox - 1];
        Vector2 pointAfterHitbox =
            pointsOnCurve[pointIndexAfterHitbox];
        float t = (compensatedScanlineX - pointBeforeHitbox.x) /
            (pointBeforeHitbox.x - pointAfterHitbox.x);
        hitbox.anchoredPosition = Vector2.Lerp(pointBeforeHitbox,
            pointAfterHitbox, t);
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

    public void DrawRepeatPathTo(int lastRepeatNotePulse,
        bool positionEndOfScanOutOfBounds)
    {
        float startX = GetComponent<RectTransform>()
            .anchoredPosition.x;
        float endX = scanRef.FloatPulseToXPosition(
            lastRepeatNotePulse,
            positionEndOfScanOutOfBounds,
            positionAfterScanOutOfBounds: true);
        float width = Mathf.Abs(startX - endX);

        pathToLastRepeatNote.sizeDelta = new Vector2(width,
            pathToLastRepeatNote.sizeDelta.y);
        if (endX < startX)
        {
            pathToLastRepeatNote.localRotation =
                Quaternion.Euler(0f, 0f, 180f);
            pathToLastRepeatNote.localScale = new Vector3(
                pathToLastRepeatNote.localScale.x,
                -pathToLastRepeatNote.localScale.y,
                pathToLastRepeatNote.localScale.z);
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

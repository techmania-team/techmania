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
//
// TODO: break this into derived classes, one for each note type.
public class NoteAppearance : MonoBehaviour
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

    [Header("Repeat")]
    public RectTransform pathToLastRepeatNote;

    protected Scan scanRef;
    protected Scanline scanlineRef;

    public virtual void TypeSpecificInitialize() { }

    #region State Interfaces
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

    public virtual void SetOngoing()
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
    protected void SetNoteImageVisibility(Visibility v)
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

    protected void SetFeverOverlayVisibility(Visibility v)
    {
        if (feverOverlay == null) return;
        feverOverlay.GetComponent<Image>().enabled =
            v != Visibility.Hidden;
    }

    protected void SetDurationTrailVisibility(Visibility v)
    {
        HoldTrailManager holdTrailManager = 
            GetComponent<HoldTrailManager>();
        if (holdTrailManager == null) return;
        holdTrailManager.SetVisibility(v);
    }

    protected void SetHoldExtensionVisibility(Visibility v)
    {
        if (holdExtensions == null) return;
        foreach (HoldExtension e in holdExtensions)
        {
            e.SetVisibility(v);
        }
    }

    protected void SetRepeatPathVisibility(Visibility v)
    {
        if (pathToLastRepeatNote == null) return;
        pathToLastRepeatNote.gameObject.SetActive(
            v != Visibility.Hidden);
    }

    protected void SetRepeatPathExtensionVisibility(Visibility v)
    {
        if (repeatPathExtensions == null) return;
        foreach (RepeatPathExtension e in repeatPathExtensions)
        {
            e.SetExtensionVisibility(v);
        }
    }

    protected virtual void UpdateState()
    {
        switch (state)
        {
            case State.Inactive:
            case State.Resolved:
                SetNoteImageVisibility(Visibility.Hidden);
                SetFeverOverlayVisibility(Visibility.Hidden);
                SetDurationTrailVisibility(Visibility.Hidden);
                SetHoldExtensionVisibility(Visibility.Hidden);
                SetRepeatPathVisibility(Visibility.Hidden);
                SetRepeatPathExtensionVisibility(Visibility.Hidden);
                break;
            case State.Prepare:
                // Only the following should be transparent:
                // - Basic Note (handled in subclass)
                // - Trail of Hold Note
                // - Curve
                NoteType type = GetNoteType();
                SetNoteImageVisibility(Visibility.Visible);
                SetFeverOverlayVisibility(Visibility.Visible);
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
                SetRepeatPathVisibility(Visibility.Visible);
                // Not set for extensions: these will be controlled
                // by the scan they belong to.
                break;
            case State.Active:
                SetNoteImageVisibility(Visibility.Visible);
                SetFeverOverlayVisibility(Visibility.Visible);
                SetDurationTrailVisibility(Visibility.Visible);
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
                SetDurationTrailVisibility(Visibility.Visible);
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

    protected void Start()
    {
        state = State.Inactive;
        UpdateState();
    }

    protected void OnEnable()
    {
        if (hitbox != null)
        {
            Game.HitboxVisibilityChanged += 
                OnHitboxVisibilityChanged;
        }
    }

    protected void OnDisable()
    {
        if (hitbox != null)
        {
            Game.HitboxVisibilityChanged -= 
                OnHitboxVisibilityChanged;
        }
    }

    public void SetScanAndScanlineRef(Scan scan, Scanline scanline)
    {
        scanRef = scan;
        scanlineRef = scanline;
    }

    #region Update
    protected void Update()
    {
        if (state == State.Inactive || state == State.Resolved) 
            return;

        UpdateSprites();
        TypeSpecificUpdate();
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
    }

    protected virtual void TypeSpecificUpdate() { }
    #endregion

    #region Note skin
    protected virtual void GetNoteImageScale(out float x,
        out float y)
    {
        x = 1f;
        y = 1f;
    }

    // For paths and trails and stuff.
    protected virtual void TypeSpecificInitializeScale() { }

    public void InitializeScale()
    {
        float noteImageScaleX = 1f;
        float noteImageScaleY = 1f;
        switch (GetNoteType())
        {
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
            default:
                GetNoteImageScale(out noteImageScaleX, out noteImageScaleY);
                break;
        }
        noteImage.transform.localScale = new Vector3(
            noteImageScaleX, noteImageScaleY, 1f);

        TypeSpecificInitializeScale();
    }

    protected virtual void UpdateSprites()
    {
        switch (GetNoteType())
        {
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
    protected virtual float GetHitboxWidth()
    {
        return Ruleset.instance.hitboxWidth;
    }

    private void InitializeHitbox()
    {
        if (hitbox == null) return;
        float hitboxWidth = GetHitboxWidth();
        SetHitboxSize(hitboxWidth, 1f);
    }

    protected void SetHitboxSize(float width, float height)
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

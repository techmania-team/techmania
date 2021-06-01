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
    public State state { get; protected set; }

    public enum Visibility
    {
        Hidden,
        Transparent,
        Visible
    }

    public Image noteImage;
    public GameObject feverOverlay;
    public RectTransform hitbox;

    protected Scan scanRef;
    protected Scanline scanlineRef;

    public NoteType GetNoteType()
    {
        return GetComponent<NoteObject>().note.type;
    }

    #region Initialization
    public virtual void TypeSpecificInitialize() { }

    public void SetScanAndScanlineRef(Scan scan, Scanline scanline)
    {
        scanRef = scan;
        scanlineRef = scanline;
    }
    #endregion

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

    protected virtual void TypeSpecificResolve() 
    {
        state = State.Resolved;
    }

    public void Resolve()
    {
        TypeSpecificResolve();
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

    protected virtual void TypeSpecificUpdateState() { }

    private void UpdateState()
    {
        TypeSpecificUpdateState();
        UpdateTrailAndHoldExtension();
    }

    private void UpdateTrailAndHoldExtension()
    {
        switch (state)
        {
            case State.Inactive:
            case State.Resolved:
                SetDurationTrailVisibility(Visibility.Hidden);
                SetHoldExtensionVisibility(Visibility.Hidden);
                break;
            case State.Prepare:
                // Only the following should be transparent:
                // - Basic Note (handled in subclass)
                // - Trail of Hold Note
                // - Curve (handled in subclass)
                NoteType type = GetNoteType();
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
                // Not set for extensions: these will be controlled
                // by the scan they belong to.
                break;
            case State.Active:
                SetDurationTrailVisibility(Visibility.Visible);
                // Not set for extensions: these will be controlled
                // by the scan they belong to.
                break;
            case State.Ongoing:
                SetDurationTrailVisibility(Visibility.Visible);
                // Not set for extensions: these will be controlled
                // by the scan they belong to.
                break;
            case State.PendingResolve:
                SetDurationTrailVisibility(Visibility.Hidden);
                SetHoldExtensionVisibility(Visibility.Hidden);
                break;
        }
    }
    #endregion

    #region Monobehaviuor
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

    protected virtual void TypeSpecificUpdate() { }

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
    }
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
        float x, y;
        GetNoteImageScale(out x, out y);
        noteImage.transform.localScale = new Vector3(x, y, 1f);

        TypeSpecificInitializeScale();
    }

    protected virtual void UpdateSprites() { }
    #endregion

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
}

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
// Each note type has a corresponding subclass, and note prefabs
// contain the subclass instead of this class. However, due to
// multiple note types containing a hold trail, and C# not allowing
// multiple inheritance, the logic to handle hold trails remains
// in this base class.
//
// Inheritance graph:
// NoteAppearance
//   |-- BasicNoteAppearance
//   |-- ChainAppearanceBase
//   |     |-- ChainHeadAppearance
//   |     |-- ChainNodeAppearance
//   |
//   |-- HoldNoteAppearance
//   |-- DragNoteAppearance
//   |-- RepeatHeadApearanceBase
//   |     |-- RepeatHeadAppearance
//   |     |-- RepeatHeadHoldAppearance
//   |
//   |-- RepeatNoteAppearanceBase
//         |-- RepeatNoteAppearance
//         |-- RepeatHoldAppearance
public class NoteAppearance : MonoBehaviour
{
    public enum State
    {
        // Note has not appeared yet; starting state.
        Inactive,
        // Some note parts become transparent (basic note,
        // hold trail, drag curve), others opaque.
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

    private float alphaUpperBound;
    private bool hitboxVisible;

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

        // Initialize alpha upper bound before making anything
        // appear.
        alphaUpperBound = 1f;
        hitboxVisible = false;
        if (Modifiers.instance.noteOpacity ==
            Modifiers.NoteOpacity.FadeIn ||
            Modifiers.instance.noteOpacity ==
            Modifiers.NoteOpacity.FadeIn2)
        {
            alphaUpperBound = 0f;
        }

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

    // Drag note overrides this to additionaly adjust hitbox size.
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

        float alpha = 0f;
        switch (v)
        {
            case Visibility.Visible: alpha = 1f; break;
            case Visibility.Transparent:
                if (Modifiers.instance.noteOpacity ==
                    Modifiers.NoteOpacity.Normal)
                {
                    alpha = 0.6f;
                }
                else
                {
                    alpha = 1f;
                }
                break;
            case Visibility.Hidden: alpha = 0f; break;
        }
        alpha = Mathf.Min(alpha, alphaUpperBound);
        noteImage.color = new Color(1f, 1f, 1f, alpha);
    }

    protected void SetFeverOverlayVisibility(Visibility v)
    {
        if (feverOverlay == null) return;
        feverOverlay.GetComponent<Image>().enabled =
            v != Visibility.Hidden;
        feverOverlay.GetComponent<Image>().color = new Color(
            1f, 1f, 1f, alphaUpperBound);
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
        if (GetComponent<HoldTrailManager>() == null) return;

        switch (state)
        {
            case State.Inactive:
            case State.Resolved:
            case State.PendingResolve:
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
            case State.Ongoing:
                SetDurationTrailVisibility(Visibility.Visible);
                // Not set for extensions: these will be controlled
                // by the scan they belong to.
                break;
        }
    }
    #endregion

    #region Monobehaviour
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

    private void UpdateAlphaUpperBound()
    {
        if (Modifiers.instance.noteOpacity
            == Modifiers.NoteOpacity.Normal) return;

        float correctScan = 
            (float)GetComponent<NoteObject>().note.pulse
            / Game.PulsesPerScan;
        float currentScan = Game.FloatPulse / Game.PulsesPerScan;
        switch (Modifiers.instance.noteOpacity)
        {
            case Modifiers.NoteOpacity.FadeOut:
                if (currentScan < correctScan - 0.5f)
                {
                    alphaUpperBound = 1f;
                }
                else if (currentScan < correctScan - 0.25f)
                {
                    alphaUpperBound = 
                        (correctScan - 0.25f - currentScan)
                        * 4f;
                }
                else
                {
                    alphaUpperBound = 0f;
                }
                break;
            case Modifiers.NoteOpacity.FadeOut2:
                if (currentScan < correctScan - 1f)
                {
                    alphaUpperBound = 1f;
                }
                else if (currentScan < correctScan - 0.5f)
                {
                    alphaUpperBound = 
                        (correctScan - 0.5f - currentScan)
                        * 2f;
                }
                else
                {
                    alphaUpperBound = 0f;
                }
                break;
            case Modifiers.NoteOpacity.FadeIn:
                if (currentScan < correctScan - 0.5f)
                {
                    alphaUpperBound = 0f;
                }
                else if (currentScan < correctScan - 0.25f)
                {
                    alphaUpperBound = 1f - 
                        (correctScan - 0.25f - currentScan) * 4f;
                }
                else
                {
                    alphaUpperBound = 1f;
                }
                break;
            case Modifiers.NoteOpacity.FadeIn2:
                if (currentScan < correctScan - 0.25f)
                {
                    alphaUpperBound = 0f;
                }
                else if (currentScan < correctScan - 0.125f)
                {
                    alphaUpperBound = 1f -
                        (correctScan - 0.125f - currentScan) * 8f;
                }
                else
                {
                    alphaUpperBound = 1f;
                }
                break;
        }
    }

    protected virtual void UpdateAlpha() { }

    protected void Update()
    {
        if (state == State.Inactive || state == State.Resolved)
            return;

        UpdateHitboxImage();
        UpdateSprites();
        TypeSpecificUpdate();
        if ((state == State.Prepare || state == State.Active) &&
            Modifiers.instance.noteOpacity
            != Modifiers.NoteOpacity.Normal)
        {
            UpdateAlphaUpperBound();
            UpdateAlpha();
        }
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
        // The base implementation is unused.
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
    // Chain heads and chain nodes override this.
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
        hitboxVisible = visible;
    }

    private void UpdateHitboxImage()
    {
        if (hitbox == null) return;

        float alpha = noteImage.color.a;
        if (!hitboxVisible) alpha = 0f;
        Image hitboxImage = hitbox.GetComponent<Image>();
        hitboxImage.color = new Color(
            hitboxImage.color.r,
            hitboxImage.color.g,
            hitboxImage.color.b,
            alpha);
    }
    #endregion

    #region Trail and hold extensions
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

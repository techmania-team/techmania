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
    public GameObject approachOverlay;
    public RectTransform hitbox;

    protected Scan scanRef;
    protected Scanline scanlineRef;
    protected Image hitboxImage;

    private float alphaUpperBound;
    [Tooltip("This NoteAppearance shall not update itself.")]
    public bool controlledExternally;

    public NoteType GetNoteType()
    {
        return GetComponent<NoteObject>().note.type;
    }

    #region Initialization
    protected virtual void TypeSpecificInitialize() { }

    public void Initialize()
    {
        alphaUpperBound = 1f;
        if (Modifiers.instance.noteOpacity ==
            Modifiers.NoteOpacity.FadeIn ||
            Modifiers.instance.noteOpacity ==
            Modifiers.NoteOpacity.FadeIn2)
        {
            alphaUpperBound = 0f;
        }
        InitializeScale();
    }

    private void InitializeForUI()
    {
        alphaUpperBound = 1f;
        InitializeScale();
        Activate();
    }

    public void SetScanAndScanlineRef(Scan scan, Scanline scanline)
    {
        scanRef = scan;
        scanlineRef = scanline;
    }
    #endregion

    #region State Interfaces
    public void SetInactive()
    {
        state = State.Inactive;
        InitializeHitbox();
        TypeSpecificInitialize();
        UpdateState();
    }

    public void Prepare()
    {
        state = State.Prepare;
        UpdateState();
    }

    public void Activate()
    {
        if (state == State.Resolved)
        {
            // It is possible to resolve a note before it's activated
            // (eg. by hitting it very early), so we should not
            // activate it again.
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
    public float VisibilityToAlpha(Visibility v,
        bool bypassNoteOpacityModifier = false)
    {
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
        if (!bypassNoteOpacityModifier)
        {
            alpha = Mathf.Min(alpha, alphaUpperBound);
        }
        return alpha;
    }

    // This includes approach overlay and hit box.
    protected void SetNoteImageVisibility(Visibility v,
        bool bypassNoteOpacityModifier = false)
    {
        noteImage.gameObject.SetActive(v != Visibility.Hidden);
        noteImage.color = new Color(1f, 1f, 1f,
            VisibilityToAlpha(v, bypassNoteOpacityModifier));

        if (approachOverlay != null)
        {
            approachOverlay.GetComponent<Image>().enabled =
                v != Visibility.Hidden;
            approachOverlay.GetComponent<ApproachOverlay>()
                .SetNoteAlpha(VisibilityToAlpha(
                    v, bypassNoteOpacityModifier));
        }

        if (hitbox != null)
        {
            hitbox.gameObject.SetActive(v != Visibility.Hidden);
            if (hitboxImage != null)
            {
                // So that raycasting ignores invisible notes.
                hitboxImage.raycastTarget = v != Visibility.Hidden;
            }
        }
    }

    protected void SetFeverOverlayVisibility(Visibility v,
        bool bypassNoteOpacityModifier = false)
    {
        if (feverOverlay == null) return;
        feverOverlay.GetComponent<Image>().enabled =
            v != Visibility.Hidden;
        feverOverlay.GetComponent<FeverOverlay>().SetNoteAlpha(
            VisibilityToAlpha(v, bypassNoteOpacityModifier));
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
                if (GetNoteType() == NoteType.Hold)
                {
                    SetDurationTrailVisibility(
                        Visibility.Transparent);
                }
                else
                {
                    SetDurationTrailVisibility(Visibility.Visible);
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
        if (controlledExternally)
        {
            InitializeForUI();
        }
        else
        {
            state = State.Inactive;
            UpdateState();
            UpdateSprites();
            UpdateHitboxImage();
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
                alphaUpperBound = Mathf.InverseLerp(
                    correctScan - 0.25f,
                    correctScan - 0.5f,
                    currentScan);
                break;
            case Modifiers.NoteOpacity.FadeOut2:
                alphaUpperBound = Mathf.InverseLerp(
                   correctScan - 0.5f,
                   correctScan - 1f,
                   currentScan);
                break;
            case Modifiers.NoteOpacity.FadeIn:
                alphaUpperBound = Mathf.InverseLerp(
                   correctScan - 0.5f,
                   correctScan - 0.25f,
                   currentScan);
                break;
            case Modifiers.NoteOpacity.FadeIn2:
                alphaUpperBound = Mathf.InverseLerp(
                   correctScan - 0.25f,
                   correctScan - 0.125f,
                   currentScan);
                break;
        }

        alphaUpperBound = Mathf.SmoothStep(0f, 1f, alphaUpperBound);
    }

    protected void Update()
    {
        if (state == State.Inactive || state == State.Resolved)
            return;
        if (controlledExternally) return;

        UpdateHitboxImage();
        UpdateSprites();
        TypeSpecificUpdate();
        if ((state == State.Prepare || state == State.Active) &&
            Modifiers.instance.noteOpacity
            != Modifiers.NoteOpacity.Normal)
        {
            UpdateAlphaUpperBound();
            // Reset visibility of note parts every frame.
            UpdateState();
            if (holdExtensions == null) return;
            foreach (HoldExtension e in holdExtensions)
            {
                e.ResetVisibility();
            }
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

    private void InitializeScale()
    {
        float x, y;
        GetNoteImageScale(out x, out y);
        noteImage.transform.localScale = new Vector3(x, y, 1f);

        if (feverOverlay != null)
        {
            float scale = GlobalResource.vfxSkin.feverOverlay.scale;
            feverOverlay.GetComponent<RectTransform>().localScale =
                new Vector3(scale, scale, 1f);
        }
        if (approachOverlay != null)
        {
            float scale = GlobalResource.gameUiSkin.approachOverlay
                .scale;
            approachOverlay.GetComponent<RectTransform>().localScale =
                new Vector3(
                    scanRef.direction == Scan.Direction.Right 
                        ? scale : -scale,
                    scale,
                    1f);
        }

        TypeSpecificInitializeScale();
    }

    protected virtual void UpdateSprites() { }
    #endregion

    #region Hitbox
    // Chain heads, chain nodes and drag notes override this.
    protected virtual Vector2 GetHitboxSizeFromRuleset()
    {
        return new Vector2(Ruleset.instance.hitboxWidth,
            Ruleset.instance.hitboxHeight);
    }

    private void InitializeHitbox()
    {
        if (hitbox == null) return;
        Vector2 hitboxSize = GetHitboxSizeFromRuleset();
        SetHitboxSize(hitboxSize.x, hitboxSize.y);
    }

    protected void SetHitboxSize(float width, float height)
    {
        if (hitbox == null) return;

        hitbox.anchorMin = new Vector2(0.5f - width * 0.5f,
            0.5f - height * 0.5f);
        hitbox.anchorMax = new Vector2(0.5f + width * 0.5f,
            0.5f + height * 0.5f);
    }

    private void UpdateHitboxImage()
    {
        if (hitbox == null) return;

        float alpha = noteImage.color.a;
        if (!Game.hitboxVisible) alpha = 0f;
        if (hitboxImage == null)
        {
            hitboxImage = hitbox.GetComponent<Image>();
        }
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
            this, scanRef, scanlineRef, holdNote);
    }

    public void RegisterHoldExtension(HoldExtension e)
    {
        holdExtensions.Add(e);
        e.RegisterNoteAppearance(this);
    }

    private void UpdateOngoingTrail()
    {
        GetComponent<HoldTrailManager>().UpdateTrails(
            state == State.Ongoing);
        foreach (HoldExtension e in holdExtensions)
        {
            e.UpdateTrails(state == State.Ongoing);
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

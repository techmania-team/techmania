using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UIElements;

// This class maintains the VisualElements that make up a note's
// appearance. For a hidden note, all fields other than "note"
// are not set.
//
// Different from the editor, here the notes do not report click
// and drag events on themselves. Instead, the GameController
// performs ray tracing on all clicks and touches. This is so that
// when a held down click/touch enters another lane, GameController
// can handle that as a new click/touch. This is necessary for chain
// notes.
//
// Each note type has a corresponding subclass, and NoteManager
// contains references to the subclass instances instead of this
// class (except for hidden notes). However, due to multiple note
// types containing a hold trail, and C# not allowing multiple
// inheritance, the logic to handle hold trails remains in this
// base class.
//
// Inheritance graph:
// NoteElements
//   |-- BasicNoteElements
//   |-- ChainElementsBase
//   |     |-- ChainHeadElements
//   |     |-- ChainNodeElements
//   |
//   |-- HoldNoteElements
//   |-- DragNoteElements
//   |-- RepeatHeadElementsBase
//   |     |-- RepeatHeadElements
//   |     |-- RepeatHeadHoldElements
//   |
//   |-- RepeatNoteElementsBase
//         |-- RepeatNoteElements
//         |-- RepeatHoldElements
//
// TODO: remove the inheritance on NoteObject once we deprecate
// NoteObject.
public class NoteElements : NoteObject
{
    // Inherited: public Note note, a reference to
    // the note that spawned these elements.

    private float floatScan;  // Disregards end-of-scan.
    // Determines whether some elements are horizontally flipped.
    private GameLayout.ScanDirection scanDirection;

    public VisualElement noteImage;
    public VisualElement feverOverlay;
    public VisualElement approachOverlay;
    public VisualElement hitbox;

    public GameLayout layout;

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
    // Affected by NoteOpacity modifiers.
    private float alphaUpperBound;
    public static bool hitboxVisible;
    // If this note is controlled by some UI (eg. calibration)
    // instead of NoteManager / GameController.
    public bool controlledExternally;

    public NoteElements(Note note)
    {
        this.note = note;
    }

    #region Initialization
    // Called by SetInactive.
    protected virtual void TypeSpecificInitialize() { }

    public void Initialize(
        Pattern pattern,
        TemplateContainer templateInstance,
        GameLayout layout)
    {
        // Determine scan number and scan direction.
        this.layout = layout;
        floatScan = (float)note.pulse / Pattern.pulsesPerBeat /
            pattern.patternMetadata.bps;
        int intScan = note.GetScanNumber(pattern.patternMetadata.bps);
        scanDirection = (intScan % 2 == 0) ?
            layout.evenScanDirection :
            layout.oddScanDirection;

        // Set up the common VisualElements.
        templateInstance.AddToClassList("note-anchor");
        templateInstance.pickingMode = PickingMode.Ignore;

        noteImage = templateInstance.Q("note-image");
        feverOverlay = templateInstance.Q("fever-overlay");
        approachOverlay = templateInstance.Q("approach-overlay");
        hitbox = templateInstance.Q("hitbox");

        // Set up initial alphaUpperBound.
        alphaUpperBound = 1f;
        if (Modifiers.instance.noteOpacity ==
            Modifiers.NoteOpacity.FadeIn ||
            Modifiers.instance.noteOpacity ==
            Modifiers.NoteOpacity.FadeIn2)
        {
            alphaUpperBound = 0f;
        }

        // Set up scale and size.
        InitializeSize();

        // Initialize.
        if (controlledExternally)
        {
            InitializeForUI();
        }
        else
        {
            state = State.Inactive;
            UpdateState();
            UpdateSprites();
            HitboxMatchNoteImageAlpha();
        }
    }

    private void InitializeForUI()
    {
        alphaUpperBound = 1f;
        InitializeSize();
        Activate();
    }
    #endregion

    #region State Interfaces
    public void SetInactive()
    {
        state = State.Inactive;
        // Drag notes need to reset hitbox size.
        InitializeHitbox();
        // Drag notes need to reset curve; repeat heads need to
        // reset next unresolved note index.
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

    // Repeat heads go to PendingResolve state instead of Resolved.
    // Repeat notes report their resolvement to repeat head.
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

    // Affects note image, approach overlay and hitbox.
    // Called by subclasses' TypeSpecificUpdateState methods.
    protected void SetNoteImageApproachOverlayAndHitboxVisibility(
        Visibility v,
        bool bypassNoteOpacityModifier = false)
    {
        noteImage.visible = v != Visibility.Hidden;
        noteImage.style.opacity = VisibilityToAlpha(
            v, bypassNoteOpacityModifier);

        if (approachOverlay != null)
        {
            approachOverlay.visible =
                v != Visibility.Hidden;
            approachOverlay.style.opacity = VisibilityToAlpha(
                v, bypassNoteOpacityModifier);
        }

        if (hitbox != null)
        {
            hitbox.visible = v != Visibility.Hidden;
            // So that raycasting ignores invisible notes.
            hitbox.pickingMode = (v == Visibility.Hidden) ?
                PickingMode.Ignore : PickingMode.Position;
        }
    }

    // Affects fever overlay.
    // Called by subclasses' TypeSpecificUpdateState methods.
    protected void SetFeverOverlayVisibility(Visibility v,
        bool bypassNoteOpacityModifier = false)
    {
        if (feverOverlay == null) return;
        feverOverlay.visible = v != Visibility.Hidden;
        feverOverlay.style.opacity = VisibilityToAlpha(
            v, bypassNoteOpacityModifier);
    }

    protected void SetDurationTrailVisibility(Visibility v)
    {
        //HoldTrailManager holdTrailManager =
        //    GetComponent<HoldTrailManager>();
        //if (holdTrailManager == null) return;
        //holdTrailManager.SetVisibility(v);
    }

    protected void SetHoldExtensionVisibility(Visibility v)
    {
        //if (holdExtensions == null) return;
        //foreach (HoldExtension e in holdExtensions)
        //{
        //    e.SetVisibility(v);
        //}
    }

    protected virtual void TypeSpecificUpdateState() { }

    // Called on initialization, update and state changes.
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
                if (note.type == NoteType.Hold)
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

    #region Update
    protected virtual void TypeSpecificUpdate() { }

    private void UpdateAlphaUpperBound(float currentScan)
    {
        if (Modifiers.instance.noteOpacity
            == Modifiers.NoteOpacity.Normal) return;

        float correctScan = floatScan;
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

    protected void UpdateTime(GameTimer timer)
    {
        if (state == State.Inactive || state == State.Resolved)
            return;
        if (controlledExternally) return;

        HitboxMatchNoteImageAlpha();
        UpdateSprites();
        TypeSpecificUpdate();
        if ((state == State.Prepare || state == State.Active) &&
            Modifiers.instance.noteOpacity
            != Modifiers.NoteOpacity.Normal)
        {
            UpdateAlphaUpperBound(timer.Scan);
            // Reset visibility of note parts every frame.
            UpdateState();
            //if (holdExtensions == null) return;
            //foreach (HoldExtension e in holdExtensions)
            //{
            //    e.ResetVisibility();
            //}
        }
        //if (GetComponent<HoldTrailManager>() != null)
        //{
        //    // Do this in all visible states because it updates
        //    // sprites.
        //    //UpdateOngoingTrail();
        //}
    }
    #endregion

    #region Note skin, size and scale
    protected virtual float GetNoteImageScaleFromRuleset()
    {
        // The base implementation is unused.
        return 1f;
    }

    // For paths and trails and stuff.
    protected virtual void TypeSpecificInitializeSize() { }

    // Called during initialization. Takes care of note image,
    // fever overlay and approach overlay. Not hitbox; that's by
    // InitializeHitbox.
    private void InitializeSize()
    {
        float scale = GetNoteImageScaleFromRuleset();
        float laneHeight = layout.laneHeight;
        noteImage.style.width = laneHeight * scale;
        noteImage.style.height = laneHeight * scale;

        if (feverOverlay != null)
        {
            scale = GlobalResource.vfxSkin.feverOverlay.scale;
            feverOverlay.style.width = laneHeight * scale;
            feverOverlay.style.height = laneHeight * scale;
        }
        if (approachOverlay != null)
        {
            scale = GlobalResource.gameUiSkin.approachOverlay.scale;
            approachOverlay.style.width = laneHeight * scale *
                (scanDirection == GameLayout.ScanDirection.Left
                ? -1f : 1f);
            approachOverlay.style.height = laneHeight * scale;
        }

        TypeSpecificInitializeSize();
    }

    // Called during initialization and update.
    protected virtual void UpdateSprites() { }
    #endregion

    #region Hitbox
    // Chain heads, chain nodes and drag notes override this.
    protected virtual Vector2 GetHitboxScaleFromRuleset()
    {
        return new Vector2(Ruleset.instance.hitboxWidth,
            Ruleset.instance.hitboxHeight);
    }

    // Called when going to Inactive state.
    // Not during initialization because drag notes' hitbox
    // sizes change.
    private void InitializeHitbox()
    {
        if (hitbox == null) return;
        Vector2 hitboxScale = GetHitboxScaleFromRuleset();
        SetHitboxScale(hitboxScale);
    }

    // Called by InitializeHitbox, as well as drag notes.
    protected void SetHitboxScale(Vector2 scale)
    {
        if (hitbox == null) return;

        float laneHeight = layout.laneHeight;
        hitbox.style.width = scale.x * laneHeight;
        hitbox.style.height = scale.y * laneHeight;
    }

    // Called during initialization and update.
    private void HitboxMatchNoteImageAlpha()
    {
        if (hitbox == null) return;
        hitbox.style.opacity = noteImage.style.opacity.value;
    }
    #endregion

    #region Trail and hold extension
    // TODO: copy from NoteAppearance.
    #endregion
}
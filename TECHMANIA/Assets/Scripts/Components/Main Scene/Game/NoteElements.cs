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
// class (except for hidden notes).
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
// ================================================================
//
// Due to multiple note types (hold, repeat head hold, repeat hold)
// containing a note trail, and C# not allowing multiple
// inheritance, these note types manage trails and extensions via
// components:
//
// The note itself corresponds to an instance of HoldNoteElements /
// RepeatHeadHoldElements / RepeatHoldElements, which manage the
// note head only, and holds an instance of:
// |
// |-- HoldTrailAndExtensions, which manages trails and talks to
//     extensions, and holds an instance of:
//     |
//     |-- HoldTrailElements, to update styles of trail elements
//
// A hold extension contains to an instance of HoldExtension,
// which manages the extension itself, and holds an instance of:
// |
// |-- HoldTrailElements, to update styles of trail elements
//
// ================================================================
//
// Repeat head and repeat head hold manage repeat paths and
// extensions. This can be done via RepeatHeadElementsBase, but we
// make components anyway because they look nice next to hold trail
// stuff.
public class NoteElements : INoteHolder
{
    public const string hFlippedClass = "h-flipped";

    public Note note;
    Note INoteHolder.note
    {
        get { return note; }
        set { note = value; }
    }

    public int intScan { get; private set; }  // Respects end-of-scan.
    protected float floatScan;  // Disregards end-of-scan.
    // Some note types need additional metadata, such as
    // control scheme and BPS.
    protected Pattern pattern;
    // Determines whether some elements are horizontally flipped.
    protected GameLayout.ScanDirection scanDirection;

    public TemplateContainer templateContainer;
    public VisualElement noteImage;
    public VisualElement feverOverlay;  // May be null
    public VisualElement approachOverlay;  // May be null
    public VisualElement hitbox;  // May be null

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
    private float feverOverlayAlphaUpperBound;
    private float approachOverlayAlphaUpperBound;
    // If this note is controlled by some UI (eg. calibration)
    // instead of NoteManager / GameController.
    public bool controlledExternally;

    // Common behavior between hold, repeat head hold and
    // repeat hold. Null for other types.
    public HoldTrailAndExtensions holdTrailAndExtensions
    { get; private set; }
    // Common behavior between repeat head and repeat head hold.
    // Null for other types.
    public RepeatPathAndExtensions repeatPathAndExtensions
    { get; private set; }

    public NoteElements(Note note, bool controlledExternally = false)
    {
        this.note = note;
        this.controlledExternally = controlledExternally;
    }

    public void ResetSize()
    {
        InitializeSizeExceptHitBox();
        ResetHitbox();
    }

    #region Initialization
    public void Initialize(
        float floatScan, int intScan,
        Pattern pattern,
        TemplateContainer templateInstance,
        GameLayout layout)
    {
        // Determine scan direction.
        this.layout = layout;
        this.floatScan = floatScan;
        this.intScan = intScan;
        this.pattern = pattern;
        scanDirection = (intScan % 2 == 0) ?
            layout.evenScanDirection :
            layout.oddScanDirection;

        // Set up the common VisualElements.
        templateContainer = templateInstance;
        templateContainer.AddToClassList("note-anchor");
        templateContainer.pickingMode = PickingMode.Ignore;

        noteImage = templateContainer.Q("note-image");
        feverOverlay = templateContainer.Q("fever-overlay");
        approachOverlay = templateContainer.Q("approach-overlay");
        hitbox = templateContainer.Q("hitbox");

        if (note.type == NoteType.Hold ||
            note.type == NoteType.RepeatHeadHold ||
            note.type == NoteType.RepeatHold)
        {
            holdTrailAndExtensions = new HoldTrailAndExtensions(this,
                intScan, pattern.patternMetadata.bps, layout);
            holdTrailAndExtensions.Initialize(templateContainer);
        }
        if (note.type == NoteType.RepeatHead ||
            note.type == NoteType.RepeatHeadHold)
        {
            repeatPathAndExtensions = new RepeatPathAndExtensions(
                this, intScan, pattern.patternMetadata.bps, layout);
            repeatPathAndExtensions.Initialize(templateContainer);
        }

        TypeSpecificInitialize();

        // Set up initial alphaUpperBound.
        alphaUpperBound = 1f;
        if (!controlledExternally)
        {
            if (GameController.instance.modifiers.noteOpacity ==
                Modifiers.NoteOpacity.FadeIn ||
                GameController.instance.modifiers.noteOpacity ==
                Modifiers.NoteOpacity.FadeIn2)
            {
                alphaUpperBound = 0f;
            }
        }

        // Set up scale and size.
        InitializeSizeExceptHitBox();

        // Set up hitbox alpha.
        HitboxMatchNoteImageAlpha();

        // Optionally set state.
        if (controlledExternally)
        {
            Activate();
        }
        else
        {
            // Intentionally not calling ResetToInactive();
            // it will be called by GameController.JumpToScan.
        }
    }

    // For elements other than note head, fever overlay, approach
    // overlay and hitbox.
    protected virtual void TypeSpecificInitialize() { }

    // Takes care of note image, fever overlay and approach overlay.
    // Not hitbox; that's by InitializeHitbox, and called when
    // setting Inactive state.
    private void InitializeSizeExceptHitBox()
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
            feverOverlay.EnableInClassList(hFlippedClass,
                scanDirection == GameLayout.ScanDirection.Left &&
                GlobalResource.vfxSkin.feverOverlay.flipWhenScanningLeft);
        }
        if (approachOverlay != null)
        {
            scale = GlobalResource.gameUiSkin.approachOverlay.scale;
            approachOverlay.style.width = laneHeight * scale;
            approachOverlay.style.height = laneHeight * scale;
            approachOverlay.EnableInClassList(hFlippedClass,
                scanDirection == GameLayout.ScanDirection.Left);
        }

        holdTrailAndExtensions?.InitializeSize();
        repeatPathAndExtensions?.InitializeSize();
        TypeSpecificInitializeSizeExceptHitbox();
    }

    protected virtual float GetNoteImageScaleFromRuleset()
    {
        // The base implementation is unused.
        throw new System.NotImplementedException(
            "NoteElements.GetNoteImageScaleFromRuleset should never be called.");
    }

    // For paths and trails and stuff. Also flipping.
    protected virtual void TypeSpecificInitializeSizeExceptHitbox() { }
    #endregion

    #region Setting / Resetting Inactive state
    public void ResetToInactive()
    {
        state = State.Inactive;
        // Drag notes need to reset hitbox size.
        ResetHitbox();
        // Ongoing trails need to reset to width zero.
        holdTrailAndExtensions?.ResetToInactive();
        // Drag notes need to reset curve; repeat heads need to
        // reset next unresolved note index.
        TypeSpecificResetToInactive();
        UpdateState();
    }

    protected virtual void TypeSpecificResetToInactive() { }

    // Not called during initialization because drag notes' hitbox
    // sizes change.
    protected void ResetHitbox()
    {
        if (hitbox == null) return;
        Vector2 hitboxScale = GetHitboxScaleFromRuleset();

        float laneHeight = layout.laneHeight;
        hitbox.style.width = hitboxScale.x * laneHeight;
        hitbox.style.height = hitboxScale.y * laneHeight;
    }

    // Chain heads, chain nodes and drag notes override this.
    protected virtual Vector2 GetHitboxScaleFromRuleset()
    {
        return new Vector2(Ruleset.instance.hitboxWidth,
            Ruleset.instance.hitboxHeight);
    }
    #endregion

    #region Other states
    #region State interfaces
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

    public void Resolve()
    {
        TypeSpecificResolve();
        UpdateState();
    }

    // Repeat heads go to PendingResolve state instead of Resolved.
    // Repeat notes report their resolvement to repeat head.
    protected virtual void TypeSpecificResolve()
    {
        state = State.Resolved;
    }
    #endregion

    // Called on initialization, update and state changes.
    protected void UpdateState()
    {
        TypeSpecificUpdateState();
        holdTrailAndExtensions?.UpdateState(state);
        repeatPathAndExtensions?.UpdateState(state);
    }

    protected virtual void TypeSpecificUpdateState() { }
    #endregion

    #region Visibility changes, called by TypeSpecificUpdateState
    public float VisibilityToAlpha(Visibility v,
        bool bypassNoteOpacityModifier = false)
    {
        float alpha = 0f;
        switch (v)
        {
            case Visibility.Visible: alpha = 1f; break;
            case Visibility.Transparent:
                if (GameController.instance.modifiers.noteOpacity ==
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
            approachOverlayAlphaUpperBound = VisibilityToAlpha(
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

    protected void SetFeverOverlayVisibility(Visibility v,
        bool bypassNoteOpacityModifier = false)
    {
        if (feverOverlay == null) return;
        feverOverlay.visible = v != Visibility.Hidden;
        feverOverlayAlphaUpperBound = VisibilityToAlpha(
            v, bypassNoteOpacityModifier);
    }
    #endregion

    #region Update
    public void UpdateTime(GameTimer timer, ScoreKeeper scoreKeeper)
    {
        if (state == State.Inactive || state == State.Resolved)
            return;
        if (controlledExternally) return;

        HitboxMatchNoteImageAlpha();
        UpdateSprites(timer);
        holdTrailAndExtensions?.UpdateSprites(timer);
        repeatPathAndExtensions?.UpdateSprites(timer);
        TypeSpecificUpdate(timer);
        if ((state == State.Prepare || state == State.Active) &&
            GameController.instance.modifiers.noteOpacity
            != Modifiers.NoteOpacity.Normal)
        {
            UpdateAlphaUpperBound(timer.scan);
            // Reset visibility of note parts every frame.
            UpdateState();
        }
        if (state == State.Ongoing)
        {
            holdTrailAndExtensions?.UpdateOngoingTrail(timer);
        }
        UpdateFeverOverlay(timer.gameTime, scoreKeeper);
        UpdateApproachOverlay(timer.scan);
    }

    private void HitboxMatchNoteImageAlpha()
    {
        if (hitbox == null) return;

        float alpha = noteImage.style.opacity.value;
        if (!GameController.instance.showHitbox) alpha = 0f;
        hitbox.style.opacity = alpha;
    }

    protected virtual void UpdateSprites(GameTimer timer) { }

    protected virtual void TypeSpecificUpdate(GameTimer timer) { }

    private void UpdateAlphaUpperBound(float currentScan)
    {
        if (GameController.instance.modifiers.noteOpacity
            == Modifiers.NoteOpacity.Normal) return;

        float correctScan = floatScan;
        switch (GameController.instance.modifiers.noteOpacity)
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

    private void UpdateFeverOverlay(float time,
        ScoreKeeper scoreKeeper)
    {
        if (feverOverlay == null) return;
        if (scoreKeeper.feverState != ScoreKeeper.FeverState.Active)
        {
            feverOverlay.style.opacity = 0f;
            return;
        }

        feverOverlay.style.backgroundImage = new
            StyleBackground(GlobalResource.vfxSkin.feverOverlay
            .GetSpriteForTime(time, loop: true));
        float alpha = Mathf.Min(1f, scoreKeeper.feverAmount * 6f);
        alpha *= feverOverlayAlphaUpperBound;
        feverOverlay.style.opacity = alpha;
    }

    private void UpdateApproachOverlay(float currentScan)
    {
        if (approachOverlay == null) return;

        const float kOverlayStart = -0.5f;
        const float kOverlayEnd = 0f;

        float distance = currentScan - floatScan;
        if (distance < kOverlayStart || distance > kOverlayEnd)
        {
            approachOverlay.style.opacity = 0f;
            return;
        }

        float t = Mathf.InverseLerp(kOverlayStart, kOverlayEnd, 
            distance);
        approachOverlay.style.backgroundImage = new StyleBackground(
            GlobalResource.gameUiSkin.approachOverlay
            .GetSpriteAtFloatIndex(t));
        approachOverlay.style.opacity = approachOverlayAlphaUpperBound;
    }
    #endregion

    #region Rotation
    private static void CheckElementStyleIsInPercents(
        VisualElement e)
    {
        if (e.style.left.value.unit != LengthUnit.Percent ||
            e.style.top.value.unit != LengthUnit.Percent)
        {
            throw new System.Exception($"Element {e.name}'s left aor top styles are not in unit Percent.");
        }
    }

    private static void RotateElementToward(VisualElement self,
        Vector2 delta)
    {
        if (Mathf.Abs(delta.x) < Mathf.Epsilon &&
            Mathf.Abs(delta.y) < Mathf.Epsilon)
        {
            // Do nothing.
            return;
        }

        if (self.ClassListContains(hFlippedClass))
        {
            delta = -delta;
        }

        float angleInRadian = Mathf.Atan2(delta.y, delta.x);
        self.style.rotate = new StyleRotate(new Rotate(new Angle(
            angleInRadian, AngleUnit.Radian)));
    }

    private Vector2 DeltaBetween(VisualElement e1, VisualElement e2)
    {
        return new Vector2(
            (e2.style.left.value.value - e1.style.left.value.value) *
            layout.gameContainerWidth,
            (e2.style.top.value.value - e1.style.top.value.value) *
            layout.scanHeight)
            * 0.01f;
    }

    protected void RotateElementToward(VisualElement self,
        Vector2 selfRelativePosition, Vector2 targetRelativePosition)
    {
        Vector2 delta = new Vector2(
            (targetRelativePosition.x - selfRelativePosition.x)
            * layout.gameContainerWidth,
            (targetRelativePosition.y - selfRelativePosition.y)
            * layout.scanHeight);
        RotateElementToward(self, delta);
    }

    // Rotate "self" according to the angle from "selfAnchor" to
    // "targetAnchor".
    protected void RotateElementToward(VisualElement self,
        VisualElement selfAnchor, VisualElement targetAnchor)
    {
        CheckElementStyleIsInPercents(selfAnchor);
        CheckElementStyleIsInPercents(targetAnchor);

        Vector2 delta = DeltaBetween(selfAnchor, targetAnchor);
        RotateElementToward(self, delta);
    }

    // Rotate "self" according to the angle from "selfAnchor" to
    // "targetAnchor". Also stretch "self" according to the distance
    // between "selfAnchor" and "targetAnchor".
    protected void RotateAndStretchElementToward(VisualElement self,
        VisualElement selfAnchor, VisualElement targetAnchor)
    {
        CheckElementStyleIsInPercents(selfAnchor);
        CheckElementStyleIsInPercents(targetAnchor);

        Vector2 delta = DeltaBetween(selfAnchor, targetAnchor);
        RotateElementToward(self, delta);
        self.style.width = delta.magnitude;
    }
    #endregion
}
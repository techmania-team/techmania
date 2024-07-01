using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UIElements;

public class RepeatHeadElementsBase : NoteElements
{
    public RepeatHeadElementsBase(Note n) : base(n) { }

    protected override void TypeSpecificInitializeSizeExceptHitbox()
    {
        noteImage.EnableInClassList(hFlippedClass,
            scanDirection == GameLayout.ScanDirection.Left &&
            GlobalResource.noteSkin.repeatHead.flipWhenScanningLeft);
    }

    protected override void TypeSpecificResolve()
    {
        state = State.PendingResolve;
        // Only fully resolved when all managed repeat notes
        // get resolved.
        ManagedNoteResolved();
    }

    protected override void TypeSpecificUpdateState()
    {
        switch (state)
        {
            case State.Inactive:
            case State.Resolved:
                SetNoteImageApproachOverlayAndHitboxVisibility(
                    Visibility.Hidden,
                    bypassNoteOpacityModifier: true);
                SetFeverOverlayVisibility(Visibility.Hidden,
                    bypassNoteOpacityModifier: true);
                break;
            case State.Prepare:
            case State.Active:
            case State.PendingResolve:
                SetNoteImageApproachOverlayAndHitboxVisibility(
                    Visibility.Visible,
                    bypassNoteOpacityModifier: true);
                SetFeverOverlayVisibility(Visibility.Visible,
                    bypassNoteOpacityModifier: true);
                break;
            case State.Ongoing:
                // Only applies to repeat head hold.
                SetNoteImageApproachOverlayAndHitboxVisibility(
                    Visibility.Visible,
                    bypassNoteOpacityModifier: true);
                SetFeverOverlayVisibility(Visibility.Visible,
                    bypassNoteOpacityModifier: true);
                break;
        }
    }

    protected override float GetNoteImageScaleFromRuleset()
    {
        return GlobalResource.noteSkin.repeatHead.scale;
    }

    protected override void UpdateSprites(GameTimer timer)
    {
        noteImage.style.backgroundImage = new StyleBackground(
            GlobalResource.noteSkin.repeatHead
            .GetSpriteAtFloatIndex(timer.beat));
    }

    #region Managed notes
    // Repeat heads and repeat hold heads store references to
    // all repeat notes and repeat hold notes after it.
    private List<RepeatNoteElementsBase> managedNotes;
    // kNoteHeadIndex means the head itself.
    private int nextUnresolvedRepeatNoteIndex;
    private const int kNoteHeadIndex = -1;

    public void ManageRepeatNotes(
        List<RepeatNoteElementsBase> managedNotes)
    {
        // Clone the list because it will be cleared later.
        this.managedNotes = new List<RepeatNoteElementsBase>(
            managedNotes);
        foreach (RepeatNoteElementsBase e in managedNotes)
        {
            e.head = this;
        }
    }

    protected override void TypeSpecificResetToInactive()
    {
        nextUnresolvedRepeatNoteIndex = kNoteHeadIndex;
    }

    public NoteElements GetFirstUnresolvedManagedNote()
    {
        if (nextUnresolvedRepeatNoteIndex == kNoteHeadIndex)
        {
            return this;
        }
        else
        {
            return managedNotes[nextUnresolvedRepeatNoteIndex];
        }
    }

    public void ManagedNoteResolved()
    {
        nextUnresolvedRepeatNoteIndex++;
        if (nextUnresolvedRepeatNoteIndex >= managedNotes.Count)
        {
            state = State.Resolved;
            UpdateState();
        }
    }
    #endregion
}

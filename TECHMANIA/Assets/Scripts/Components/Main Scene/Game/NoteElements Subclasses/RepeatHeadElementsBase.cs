using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UIElements;

public class RepeatHeadElementsBase : NoteElements
{
    public RepeatHeadElementsBase(Note n) : base(n) { }

    protected override void TypeSpecificResolve()
    {
        state = State.PendingResolve;
        // Only fully resolved when all managed repeat notes
        // get resolved.
        // ManagedRepeatNoteResolved();
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
            .GetSpriteAtFloatIndex(timer.Beat));
    }

    #region Managed notes
    // Repeat heads and repeat hold heads store references to
    // all repeat notes and repeat hold notes after it.
    private List<NoteElements> managedNotes;
    // kNoteHeadIndex means the head itself.
    private int nextUnresolvedRepeatNoteIndex;
    private const int kNoteHeadIndex = -1;

    // TODO: reverse the list before calling, so that the notes
    // are in the correct order.
    // TODO: call RepeatPathAndExtensions.InitializeWithLastManagedNote.
    public void ManageRepeatNotes(List<NoteElements> managedNotes)
    {
        // Clone the list because it will be cleared later.
        managedNotes = new List<NoteElements>(managedNotes);
        foreach (NoteElements e in managedNotes)
        {
            //TODO: n.GetComponent<RepeatNoteAppearanceBase>().repeatHead
            //    = this;
        }

        // Adjust draw order so the head is drawn behind all
        // managed notes.
        foreach (NoteElements e in managedNotes)
        {
            if (templateContainer.parent ==
                e.templateContainer.parent)
            {
                templateContainer.PlaceBehind(e.templateContainer);
                break;
            }
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
            Resolve();
        }
    }
    #endregion
}

using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UIElements;

public class RepeatNoteElementsBase : NoteElements
{
    public RepeatNoteElementsBase(Note n) : base(n) { }

    // Repeat notes and repeat hold notes store references to
    // the repeat head or repeat hold head before it.
    public RepeatHeadElementsBase head;

    protected override void TypeSpecificResolve()
    {
        state = State.Resolved;
        head.ManagedNoteResolved();
    }

    protected override void TypeSpecificUpdateState()
    {
        switch (state)
        {
            case State.Inactive:
            case State.Resolved:
                SetNoteImageApproachOverlayAndHitboxVisibility(
                    Visibility.Hidden);
                break;
            case State.Prepare:
            case State.Active:
                SetNoteImageApproachOverlayAndHitboxVisibility(
                    Visibility.Visible);
                break;
            case State.Ongoing:
                // Only applies to repeat hold.
                SetNoteImageApproachOverlayAndHitboxVisibility(
                    Visibility.Hidden);
                break;
        }
    }

    protected override float GetNoteImageScaleFromRuleset()
    {
        return GlobalResource.noteSkin.repeat.scale;
    }

    protected override void UpdateSprites(GameTimer timer)
    {
        noteImage.style.backgroundImage = new StyleBackground(
            GlobalResource.noteSkin.repeat.GetSpriteAtFloatIndex(
                timer.Beat));
    }
}

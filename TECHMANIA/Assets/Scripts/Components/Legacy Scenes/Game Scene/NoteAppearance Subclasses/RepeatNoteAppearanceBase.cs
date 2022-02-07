using System.Collections;
using System.Collections.Generic;
using UnityEngine;

// Contains common logic between RepeatAppearance and
// RepeatHoldAppearance.
public class RepeatNoteAppearanceBase : NoteAppearance
{
    [HideInInspector]
    // Repeat notes and repeat hold notes store references to
    // the repeat head or repeat hold head before it.
    public RepeatHeadAppearanceBase repeatHead;

    protected override void TypeSpecificResolve()
    {
        state = State.Resolved;
        repeatHead.ManagedRepeatNoteResolved();
    }

    protected override void TypeSpecificUpdateState()
    {
        switch (state)
        {
            case State.Inactive:
            case State.Resolved:
                SetNoteImageVisibility(Visibility.Hidden);
                SetFeverOverlayVisibility(Visibility.Hidden);
                break;
            case State.Prepare:
            case State.Active:
                SetNoteImageVisibility(Visibility.Visible);
                SetFeverOverlayVisibility(Visibility.Visible);
                break;
            case State.Ongoing:
                // Only applies to repeat hold.
                SetNoteImageVisibility(Visibility.Hidden);
                SetFeverOverlayVisibility(Visibility.Visible);
                break;
        }
    }

    protected override void GetNoteImageScale(
        out float x, out float y)
    {
        x = GlobalResource.noteSkin.repeat.scale;
        y = GlobalResource.noteSkin.repeat.scale;
    }

    protected override void UpdateSprites()
    {
        noteImage.sprite = GlobalResource.noteSkin.repeat
            .GetSpriteAtFloatIndex(Game.FloatBeat);
    }
}

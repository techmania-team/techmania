using System.Collections;
using System.Collections.Generic;
using UnityEngine;

// This class is specific to hold notes, and does not handle trails.
public class HoldNoteAppearance : NoteAppearance
{
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
            case State.Ongoing:
                SetNoteImageVisibility(Visibility.Visible);
                SetFeverOverlayVisibility(Visibility.Visible);
                break;
        }
    }

    protected override void GetNoteImageScale(
        out float x, out float y)
    {
        x = GlobalResource.noteSkin.holdHead.scale;
        y = GlobalResource.noteSkin.holdHead.scale;
    }

    protected override void UpdateSprites()
    {
        noteImage.sprite = GlobalResource.noteSkin.holdHead
            .GetSpriteForFloatBeat(Game.FloatBeat);
    }
}

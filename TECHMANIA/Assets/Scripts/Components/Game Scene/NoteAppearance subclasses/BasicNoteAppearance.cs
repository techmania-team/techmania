using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class BasicNoteAppearance : NoteAppearance
{
    protected override void UpdateState()
    {
        switch (state)
        {
            case State.Inactive:
            case State.Resolved:
                SetNoteImageVisibility(Visibility.Hidden);
                SetFeverOverlayVisibility(Visibility.Hidden);
                break;
            case State.Prepare:
                SetNoteImageVisibility(Visibility.Transparent);
                SetFeverOverlayVisibility(Visibility.Visible);
                break;
            case State.Active:
                SetNoteImageVisibility(Visibility.Visible);
                SetFeverOverlayVisibility(Visibility.Visible);
                break;
        }
    }

    protected override void GetNoteImageScale(
        out float x, out float y)
    {
        x = GlobalResource.noteSkin.basic.scale;
        y = GlobalResource.noteSkin.basic.scale;
    }

    protected override void UpdateSprites()
    {
        noteImage.sprite = GlobalResource.noteSkin.basic
            .GetSpriteForFloatBeat(Game.FloatBeat);
    }
}

using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class BasicNoteElements : NoteElements
{
    public BasicNoteElements(Note note) : base(note) { }

    protected override void TypeSpecificInitializeSizeExceptHitbox()
    {
        noteImage.EnableInClassList(hFlippedClass,
            scanDirection == GameLayout.ScanDirection.Left &&
            GlobalResource.noteSkin.basic.flipWhenScanningLeft);
    }

    protected override void TypeSpecificUpdateState()
    {
        switch (state)
        {
            case State.Inactive:
            case State.Resolved:
                SetNoteImageApproachOverlayAndHitboxVisibility(
                    Visibility.Hidden);
                SetFeverOverlayVisibility(Visibility.Hidden);
                break;
            case State.Prepare:
                SetNoteImageApproachOverlayAndHitboxVisibility(
                    Visibility.Transparent);
                SetFeverOverlayVisibility(Visibility.Visible);
                break;
            case State.Active:
                SetNoteImageApproachOverlayAndHitboxVisibility(
                    Visibility.Visible);
                SetFeverOverlayVisibility(Visibility.Visible);
                break;
        }
    }

    protected override float GetNoteImageScaleFromRuleset()
    {
        return GlobalResource.noteSkin.basic.scale;
    }

    protected override void UpdateSprites(GameTimer timer)
    {
        noteImage.style.backgroundImage = new 
            UnityEngine.UIElements.StyleBackground(
            GlobalResource.noteSkin.basic
            .GetSpriteAtFloatIndex(timer.beat));
    }
}

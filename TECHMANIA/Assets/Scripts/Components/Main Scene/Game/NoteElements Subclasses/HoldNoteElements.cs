using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class HoldNoteElements : NoteElements
{
    public HoldNoteElements(Note n) : base(n) { }

    protected override void TypeSpecificInitializeSizeExceptHitbox()
    {
        noteImage.EnableInClassList(hFlippedClass,
            scanDirection == GameLayout.ScanDirection.Left &&
            GlobalResource.noteSkin.holdHead.flipWhenScanningLeft);
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
            case State.Active:
            case State.Ongoing:
                SetNoteImageApproachOverlayAndHitboxVisibility(
                    Visibility.Visible);
                SetFeverOverlayVisibility(Visibility.Visible);
                break;
        }
    }

    protected override float GetNoteImageScaleFromRuleset()
    {
        return GlobalResource.noteSkin.holdHead.scale;
    }

    protected override void UpdateSprites(GameTimer timer)
    {
        noteImage.style.backgroundImage = new UnityEngine.UIElements
            .StyleBackground(GlobalResource.noteSkin.holdHead
            .GetSpriteAtFloatIndex(timer.beat));
        // Trail sprites are covered by HoldTrailElements.UpdateTrails,
        // which calls HoldTrailElements.UpdateSprites.
    }
}

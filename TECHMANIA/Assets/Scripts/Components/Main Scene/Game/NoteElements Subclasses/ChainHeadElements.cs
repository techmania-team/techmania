using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class ChainHeadElements : ChainElementsBase
{
    public ChainHeadElements(Note n) : base(n) { }

    protected override float GetNoteImageScaleFromRuleset()
    {
        return GlobalResource.noteSkin.chainHead.scale;
    }

    protected override void UpdateSprites(GameTimer timer)
    {
        noteImage.style.backgroundImage = new
            UnityEngine.UIElements.StyleBackground(
            GlobalResource.noteSkin.chainHead
            .GetSpriteAtFloatIndex(timer.Beat));
    }

    protected override Vector2 GetHitboxScaleFromRuleset()
    {
        return new Vector2(Ruleset.instance.chainHeadHitboxWidth,
            Ruleset.instance.hitboxHeight);
    }
}
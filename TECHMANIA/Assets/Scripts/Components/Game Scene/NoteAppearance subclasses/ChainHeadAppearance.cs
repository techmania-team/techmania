using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class ChainHeadAppearance : ChainAppearanceBase
{
    protected override void GetNoteImageScale(
        out float x, out float y)
    {
        x = GlobalResource.noteSkin.chainHead.scale;
        y = GlobalResource.noteSkin.chainHead.scale;
    }

    protected override void UpdateSprites()
    {
        noteImage.sprite = GlobalResource.noteSkin.chainHead
            .GetSpriteForFloatBeat(Game.FloatBeat);
    }

    protected override float GetHitboxWidth()
    {
        return Ruleset.instance.chainHeadHitboxWidth;
    }
}

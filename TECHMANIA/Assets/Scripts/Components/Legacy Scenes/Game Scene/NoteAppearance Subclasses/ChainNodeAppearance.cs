using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

public class ChainNodeAppearance : ChainAppearanceBase
{
    public RectTransform pathToPreviousNote;

    public void SetPathToPreviousChainNodeVisibility(Visibility v)
    {
        if (pathToPreviousNote == null) return;
        pathToPreviousNote.gameObject.SetActive(
            v != Visibility.Hidden);
        pathToPreviousNote.GetComponent<Image>().color =
            new Color(1f, 1f, 1f, VisibilityToAlpha(v));
    }

    public void PointPathTowards(RectTransform previousNote)
    {
        if (pathToPreviousNote == null) return;
        UIUtils.PointToward(pathToPreviousNote,
            selfPos: GetComponent<RectTransform>().anchoredPosition,
            targetPos: previousNote
                .GetComponent<RectTransform>().anchoredPosition);
    }

    protected override void TypeSpecificInitializeScale()
    {
        pathToPreviousNote.localScale = new Vector3(1f,
            GlobalResource.noteSkin.chainPath.scale,
            1f);
    }

    protected override void GetNoteImageScale(
        out float x, out float y)
    {
        x = GlobalResource.noteSkin.chainNode.scale;
        y = GlobalResource.noteSkin.chainNode.scale;
    }

    protected override void UpdateSprites()
    {
        noteImage.sprite = GlobalResource.noteSkin.chainNode
            .GetSpriteAtFloatIndex(Game.FloatBeat);
        pathToPreviousNote.GetComponent<Image>().sprite =
            GlobalResource.noteSkin.chainPath
            .GetSpriteAtFloatIndex(Game.FloatBeat);
    }

    protected override Vector2 GetHitboxSizeFromRuleset()
    {
        return new Vector2(Ruleset.instance.chainNodeHitboxWidth,
            Ruleset.instance.hitboxHeight);
    }
}

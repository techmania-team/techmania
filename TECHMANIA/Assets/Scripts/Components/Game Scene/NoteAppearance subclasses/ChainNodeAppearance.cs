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
            .GetSpriteForFloatBeat(Game.FloatBeat);
        pathToPreviousNote.GetComponent<Image>().sprite =
            GlobalResource.noteSkin.chainPath
            .GetSpriteForFloatBeat(Game.FloatBeat);
    }

    protected override float GetHitboxWidth()
    {
        return Ruleset.instance.chainNodeHitboxWidth;
    }
}

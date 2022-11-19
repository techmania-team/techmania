using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UIElements;

public class ChainNodeElements : ChainElementsBase
{
    private VisualElement pathToPreviousNote;

    public ChainNodeElements(Note n) : base(n) { }

    protected override void TypeSpecificInitialize()
    {
        pathToPreviousNote = templateContainer.Q(
            "path-to-previous-note");
    }

    public void SetPathToPreviousChainNodeVisibility(Visibility v)
    {
        if (pathToPreviousNote == null) return;
        pathToPreviousNote.visible = v != Visibility.Hidden;
        pathToPreviousNote.style.opacity = VisibilityToAlpha(v);
    }

    public void PointPathTowards(TemplateContainer previousNote)
    {
        if (pathToPreviousNote == null) return;
        RotateAndStretchElementToward(pathToPreviousNote,
            selfAnchor: templateContainer,
            targetAnchor: previousNote);
    }

    protected override void TypeSpecificInitializeSize()
    {
        pathToPreviousNote.style.height = layout.laneHeight *
            GlobalResource.noteSkin.chainPath.scale;
    }

    protected override float GetNoteImageScaleFromRuleset()
    {
        return GlobalResource.noteSkin.chainNode.scale;
    }

    protected override void UpdateSprites(GameTimer timer)
    {
        noteImage.style.backgroundImage = new StyleBackground(
            GlobalResource.noteSkin.chainNode
            .GetSpriteAtFloatIndex(timer.Beat));
        pathToPreviousNote.style.backgroundImage = new
            StyleBackground(GlobalResource.noteSkin.chainPath
            .GetSpriteAtFloatIndex(timer.Beat));
    }

    protected override Vector2 GetHitboxScaleFromRuleset()
    {
        return new Vector2(Ruleset.instance.chainNodeHitboxWidth,
            Ruleset.instance.hitboxHeight);
    }
}

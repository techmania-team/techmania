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

    protected override void TypeSpecificInitializeSizeExceptHitbox()
    {
        noteImage.EnableInClassList(hFlippedClass,
            scanDirection == GameLayout.ScanDirection.Left &&
            GlobalResource.noteSkin.chainNode.flipWhenScanningLeft);

        pathToPreviousNote.style.height = layout.laneHeight *
            GlobalResource.noteSkin.chainPath.scale;
        pathToPreviousNote.EnableInClassList(hFlippedClass,
            scanDirection == GameLayout.ScanDirection.Left &&
            GlobalResource.noteSkin.chainPath.flipWhenScanningLeft);
        
        // During initialization, this does nothing as there is no
        // next node.
        // When NoteManager calls SetNextChainNode, this will also be
        // called.
        // When resetting size, there is a next node, so this will work.
        RotateNoteImageAndPathFromNextNode();
    }

    protected override float GetNoteImageScaleFromRuleset()
    {
        return GlobalResource.noteSkin.chainNode.scale;
    }

    protected override void UpdateSprites(GameTimer timer)
    {
        noteImage.style.backgroundImage = new StyleBackground(
            GlobalResource.noteSkin.chainNode
            .GetSpriteAtFloatIndex(timer.beat));
        pathToPreviousNote.style.backgroundImage = new
            StyleBackground(GlobalResource.noteSkin.chainPath
            .GetSpriteAtFloatIndex(timer.beat));
    }

    protected override Vector2 GetHitboxScaleFromRuleset()
    {
        return new Vector2(Ruleset.instance.chainNodeHitboxWidth,
            Ruleset.instance.hitboxHeight);
    }
}

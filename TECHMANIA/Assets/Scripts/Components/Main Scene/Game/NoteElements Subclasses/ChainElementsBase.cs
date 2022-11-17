using System.Collections;
using System.Collections.Generic;
using UnityEngine;

// Contains common logic between ChainHeadElements and
// ChainNodeElements.
public class ChainElementsBase : NoteElements
{
    public ChainElementsBase(Note note) : base(note) { }

    // A little complication here is that, to achieve the correct
    // draw order, each Chain Node draws a path to its previous
    // Chain Head/Node, the same way as in the editor.
    // However, when a Chain Head/Node gets resolved, it should
    // also take away the path pointing to it. Therefore, it's
    // necessary for each Chain Head/Node to be aware of, and
    // eventually control, the next Chain Node.
    //
    // May be null.
    protected ChainNodeElements nextChainNode;

    public void SetNextChainNode(ChainNodeElements nextChainNode)
    {
        this.nextChainNode = nextChainNode;
        if (nextChainNode == null) return;

        // Point path towards this note.
        nextChainNode.PointPathTowards(templateContainer);

        // Point note image towards next note.
        //UIUtils.RotateToward(
        //    noteImage.GetComponent<RectTransform>(),
        //    selfPos: GetComponent<RectTransform>()
        //        .anchoredPosition,
        //    targetPos: nextChainNode
        //        .GetComponent<RectTransform>()
        //        .anchoredPosition);

        if (nextChainNode.nextChainNode == null)
        {
            // Next node is the last node in the chain, so we
            // also rotate that node.
            //UIUtils.RotateToward(
            //    nextChainNode.GetComponent<NoteAppearance>()
            //        .noteImage.GetComponent<RectTransform>(),
            //    selfPos: GetComponent<RectTransform>()
            //        .anchoredPosition,
            //    targetPos: nextChainNode
            //        .GetComponent<RectTransform>()
            //        .anchoredPosition);
        }
    }

    private void SetPathFromNextChainNodeVisibility(Visibility v)
    {
        if (nextChainNode == null) return;
        nextChainNode.SetPathToPreviousChainNodeVisibility(v);
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
                SetPathFromNextChainNodeVisibility(
                    Visibility.Hidden);
                break;
            case State.Prepare:
            case State.Active:
                SetNoteImageApproachOverlayAndHitboxVisibility(
                    Visibility.Visible);
                SetFeverOverlayVisibility(Visibility.Visible);
                SetPathFromNextChainNodeVisibility(
                    Visibility.Visible);
                break;
        }
    }
}

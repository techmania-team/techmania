using System.Collections;
using System.Collections.Generic;
using UnityEngine;

// Contains common logic between ChainHeadAppearance and
// ChainNodeAppearance.
public class ChainAppearanceBase : NoteAppearance
{
    // A little complication here is that, to achieve the correct
    // draw order, each Chain Node draws a path to its previous
    // Chain Head/Node, the same way as in the editor.
    // However, when a Chain Head/Node gets resolved, it should
    // also take away the path pointing to it. Therefore, it's
    // necessary for each Chain Head/Node to be aware of, and
    // eventually control, the next Chain Node.
    //
    // May be null.
    private GameObject nextChainNode;
    public void SetNextChainNode(GameObject nextChainNode)
    {
        this.nextChainNode = nextChainNode;
        if (nextChainNode == null) return;

        nextChainNode.GetComponent<ChainNodeAppearance>()
            .PointPathTowards(GetComponent<RectTransform>());
        UIUtils.RotateToward(
            noteImage.GetComponent<RectTransform>(),
            selfPos: GetComponent<RectTransform>()
                .anchoredPosition,
            targetPos: nextChainNode
                .GetComponent<RectTransform>()
                .anchoredPosition);

        if (nextChainNode.GetComponent<ChainNodeAppearance>()
            .nextChainNode == null)
        {
            // Next node is the last node in the chain, so we
            // also rotate that node.
            UIUtils.RotateToward(
                nextChainNode.GetComponent<NoteAppearance>()
                    .noteImage.GetComponent<RectTransform>(),
                selfPos: GetComponent<RectTransform>()
                    .anchoredPosition,
                targetPos: nextChainNode
                    .GetComponent<RectTransform>()
                    .anchoredPosition);
        }
    }

    private void SetPathFromNextChainNodeVisibility(Visibility v)
    {
        if (nextChainNode == null) return;
        nextChainNode.GetComponent<ChainNodeAppearance>()
            .SetPathToPreviousChainNodeVisibility(v);
    }

    protected override void TypeSpecificUpdateState()
    {
        switch (state)
        {
            case State.Inactive:
            case State.Resolved:
                SetNoteImageVisibility(Visibility.Hidden);
                SetFeverOverlayVisibility(Visibility.Hidden);
                SetPathFromNextChainNodeVisibility(
                    Visibility.Hidden);
                break;
            case State.Prepare:
            case State.Active:
                SetNoteImageVisibility(Visibility.Visible);
                SetFeverOverlayVisibility(Visibility.Visible);
                SetPathFromNextChainNodeVisibility(
                    Visibility.Visible);
                break;
        }
    }
}

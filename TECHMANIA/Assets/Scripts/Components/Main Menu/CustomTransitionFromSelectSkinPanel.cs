using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class CustomTransitionFromSelectSkinPanel : TransitionToPanel
{
    public ConfirmDialog confirmDialog;

    [HideInInspector]
    private bool skinChanged;

    public void MarkSkinChanged()
    {
        skinChanged = true;
    }

    private void OnEnable()
    {
        skinChanged = false;
    }

    public override void Invoke()
    {
        if (skinChanged)
        {
            confirmDialog.Show(
                "The game needs to restart in order to load the specified skins. Continue?",
                "yes", "no", ForceTransition);
        }
        else
        {
            ForceTransition();
        }
    }

    public void ForceTransition()
    {
        Curtain.DrawCurtainThenGoToScene("Main Menu");
    }
}

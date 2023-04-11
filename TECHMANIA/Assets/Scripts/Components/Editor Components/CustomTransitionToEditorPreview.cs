using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class CustomTransitionToEditorPreview : TransitionToPanel
{
    public override void Invoke()
    {
        PanelTransitioner.TransitionTo(null, Direction.Right,
            callbackOnFinish: EditorContext.previewCallback);
    }
}

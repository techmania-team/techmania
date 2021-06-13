using System.Collections;
using System.Collections.Generic;
using UnityEngine;

// A sidesheet is functionally the same thing as a dialog, except
// it appears on the right edge of the screen, instead of the
// center.
public class Sidesheet : Dialog
{
    protected override Vector2 FadeInStep(float progress)
    {
        return new Vector2(
            PanelTransitioner.Damp(kFadeDistance, 0f, progress) +
            restingAnchoredPosition.x,
            restingAnchoredPosition.y);
    }

    protected override Vector2 FadeOutStep(float progress)
    {
        return new Vector2(
            PanelTransitioner.Damp(0f, kFadeDistance, progress) +
            restingAnchoredPosition.x,
            restingAnchoredPosition.y);
    }
}

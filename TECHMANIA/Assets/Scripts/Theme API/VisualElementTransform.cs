using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UIElements;
using MoonSharp.Interpreter;

namespace ThemeApi
{
    [MoonSharpUserData]
    public class VisualElementTransform
    {
        // Note: Y+ is downward.
        public static Vector2 ScreenSpaceToElementLocalSpace(
            VisualElement element, Vector2 screenSpace)
        {
            Vector2 invertedScreenSpace = new Vector2(
                screenSpace.x, Screen.height - screenSpace.y);
            // worldSpace will be in the reference resolution of
            // 1920 x 1080. Therefore, invertedScreenSpace and
            // worldSpace will be different if the current resolution
            // is not 1920 x 1080.
            Vector2 worldSpace = RuntimePanelUtils.ScreenToPanel(
                element.panel, invertedScreenSpace);
            Vector2 localSpace = element.WorldToLocal(worldSpace);
            return localSpace;
        }

        // Used by VFXManager and ComboText.
        public static Vector2 ElementCenterToWorldSpace(
            VisualElement element)
        {
            Vector2 screenPoint = element.worldBound.center;
            int referenceResolutionHeight = 
                TopLevelObjects.instance.mainUiDocument
                .panelSettings.referenceResolution.y;
            // Reverse Y coordinate when passing a position to Canvas.
            screenPoint.y = referenceResolutionHeight - screenPoint.y;
            return screenPoint;
        }

        public static bool ElementContainsPointInScreenSpace(
            VisualElement element, Vector2 screenSpace)
        {
            Vector2 localSpace = ScreenSpaceToElementLocalSpace(
                element, screenSpace);
            return element.ContainsPoint(localSpace);
        }
    }
}
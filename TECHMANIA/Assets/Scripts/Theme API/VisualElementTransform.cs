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
            Vector2 worldSpace = RuntimePanelUtils.ScreenToPanel(
                element.panel, invertedScreenSpace);
            return element.WorldToLocal(worldSpace);
        }

        public static Vector2 ElementCenterToScreenSpace(
            VisualElement element)
        {
            Vector2 screenPoint = element.worldBound.center;
            // Reverse Y coordinate when passing a position to Canvas.
            screenPoint.y = Screen.height - screenPoint.y;
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
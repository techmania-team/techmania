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

        // Note: Y+ is downward.
        public static Vector2 ScreenSpaceToElementLocalSpace(
            VisualElementWrap element, Vector2 screenSpace)
        {
            return ScreenSpaceToElementLocalSpace(element.inner,
                screenSpace);
        }

        public static bool ElementContainsPointInScreenSpace(
            VisualElement element, Vector2 screenSpace)
        {
            Vector2 localSpace = ScreenSpaceToElementLocalSpace(
                element, screenSpace);
            return element.ContainsPoint(localSpace);
        }

        public static bool ElementContainsPointInScreenSpace(
            VisualElementWrap element, Vector2 screenSpace)
        {
            return ElementContainsPointInScreenSpace(element.inner,
                screenSpace);
        }
    }
}
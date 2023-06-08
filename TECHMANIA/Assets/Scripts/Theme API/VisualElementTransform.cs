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
            // TODO: update this
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

        // Viewport space: bottom left of screen is (0, 0),
        // top right is (1, 1).
        public static Vector2 LocalSpaceToViewportSpace(
            VisualElement element, Vector2 localPoint)
        {
            VisualElement root = TopLevelObjects.instance.mainUiDocument
                .rootVisualElement;
            // It's called "world" but it's actually in the root
            // element's local space. Took me a while to figure out.
            Vector2 worldPoint = element.LocalToWorld(localPoint);
            // In UI Toolkit, Y+ is downwards, but in Unity UI's
            // definition of "view port", Y+ is upwards.
            return new Vector2(
                worldPoint.x / root.contentRect.width,
                1f - worldPoint.y / root.contentRect.height);
        }

        // Used by VFXManager and ComboText.
        public static Vector2 ElementCenterToViewportSpace(
            VisualElement element, bool log = false)
        {
            return LocalSpaceToViewportSpace(element,
                element.contentRect.center);
        }

        public static bool ElementContainsPointInScreenSpace(
            VisualElement element, Vector2 screenSpace)
        {
            // TODO: update this
            Vector2 localSpace = ScreenSpaceToElementLocalSpace(
                element, screenSpace);
            return element.ContainsPoint(localSpace);
        }
    }
}
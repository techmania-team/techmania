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
        // A few definitions
        //
        // Local space: in a UI Toolkit element's content rect,
        // in pixels, (0, 0) is top left.
        //
        // Call VisualElement.LocalToWorld/WorldToLocal to convert
        // between local and world.
        //
        // World space: in UI Toolkit root element's content rect,
        // in pixels, (0, 0) is top left.
        //
        // To convert between world and viewport, divide/multiply
        // by the root element's content rect's size, also reverse y.
        //
        // Viewport space: bottom left of the screen is (0, 0),
        // top right of the screen is (1, 1).
        //
        // To convert between viewport and screen, divide/multiply
        // by resolution.
        //
        // Screen space: bottom left of the screen is (0, 0),
        // top right of the screen is (Screen.width, Screen.height).

        public static Vector2 ScreenSpaceToLocalSpace(
            VisualElement element, Vector2 screenPoint)
        {
            VisualElement root = TopLevelObjects.instance.mainUiDocument
                .rootVisualElement;
            Vector2 viewportPoint = new Vector2(
                screenPoint.x / Screen.width,
                screenPoint.y / Screen.height);
            Vector2 worldPoint = new Vector2(
                viewportPoint.x * root.contentRect.width,
                (1f - viewportPoint.y) * root.contentRect.height);
            return element.WorldToLocal(worldPoint);
        }

        public static Vector2 LocalSpaceToViewportSpace(
            VisualElement element, Vector2 localPoint)
        {
            VisualElement root = TopLevelObjects.instance.mainUiDocument
                .rootVisualElement;
            Vector2 worldPoint = element.LocalToWorld(localPoint);
            return new Vector2(
                worldPoint.x / root.contentRect.width,
                1f - worldPoint.y / root.contentRect.height);
        }

        public static Vector2 LocalSpaceToScreenSpace(
            VisualElement element, Vector2 localPoint)
        {
            Vector2 viewportPoint = LocalSpaceToViewportSpace(
                element, localPoint);
            return new Vector2(viewportPoint.x * Screen.width,
                viewportPoint.y * Screen.height);
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
            Vector2 localSpace = ScreenSpaceToLocalSpace(
                element, screenSpace);
            return element.ContainsPoint(localSpace);
        }
    }
}
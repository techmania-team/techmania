using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using MoonSharp.Interpreter;
using UnityEngine.UIElements;

namespace ThemeApi
{
    // VisualElement and related classes are wrapped in these API
    // class because:
    // - Lua doesn't support generics or extension methods
    // - Lua functions aren't automatically converted to Actions
    //
    // Note that it's possible to create multiple wraps on the same
    // VisualElement.
    [MoonSharpUserData]
    public class VisualElementWrap
    {
        public VisualElement inner { get; private set; }

        [MoonSharpHidden]
        public VisualElementWrap(VisualElement e)
        {
            inner = e;
        }

        #region Properties
        public int childCount => inner.childCount;
        public bool enabledInHierarchy => inner.enabledInHierarchy;
        public bool enabledSelf => inner.enabledSelf;
        public string name => inner.name;
        public VisualElementWrap parent =>
            new VisualElementWrap(inner.parent);
        public bool visible => inner.visible;
        // If false, this element will ignore pointer events.
        public bool pickable
        {
            get { return inner.pickingMode == PickingMode.Position; }
            set
            {
                inner.pickingMode = value ?
                    PickingMode.Position : PickingMode.Ignore;
            }
        }

        public void SetEnabled(bool enabled)
        {
            inner.SetEnabled(enabled);
        }

        public IResolvedStyle resolvedStyle => inner.resolvedStyle;
        public IStyle style => inner.style;
        #endregion

        #region Subclass-specific properties
        public void CheckType(System.Type type, string targetMember)
        {
            if (!type.IsAssignableFrom(inner.GetType()))
            {
                throw new System.Exception($"VisualElement {name} is not a {type.Name}, and therefore does not have the '{targetMember}' member.");
            }
        }

        public bool IsTextElement() { return inner is TextElement; }
        public bool IsButton() { return inner is Button; }
        public bool IsToggle() { return inner is Toggle; }

        public string text
        {
            get
            {
                CheckType(typeof(TextElement), "text");
                return (inner as TextElement).text;
            }
            set
            {
                CheckType(typeof(TextElement), "text");
                (inner as TextElement).text = value;
            }
        }
        #endregion

        #region Events
        // https://docs.unity3d.com/2021.2/Documentation/Manual/UIE-Events-Reference.html
        // Exposed as the "eventType" global table
        public enum EventType
        {
            // Capture events: omitted

            // Change events
            Change,

            // Command events: omitted

            // Drag events
            DragExited,
            DragUpdated,
            DragPerform,
            DragEnter,
            DragLeave,

            // Focus events
            FocusOut,
            FocusIn,
            Blur,
            Focus,

            // Input events
            Input,

            // Keyboard events
            KeyDown,
            KeyUp,

            // Layout events
            GeometryChanged,

            // Pointer & mouse events
            // (mouse fires both, touchscreen only fires pointer)
            PointerDown,
            PointerUp,
            PointerMove,
            PointerEnter,
            PointerLeave,
            PointerOver,
            PointerOut,
            PointerStationary,
            PointerCancel,
            Click,
            Wheel,
            
            // Panel events
            AttachToPanel,
            DetachFromPanel,

            // Tooltip events
            Tooptip,

            // Unity events
            FrameUpdate,
            ApplicationFocus,
        }

        // Callback parameters:
        // 1. The VisualElementWrap receiving the event
        // 2. The event
        // 3. The data (Void if called without this parameters)
        public void RegisterCallback(EventType eventType,
            DynValue callback, DynValue data)
        {
            callback.CheckType("VisualElementWrap.RegisterCallback",
                DataType.Function);
            switch (eventType)
            {
                case EventType.Click:
                    CallbackRegistry.AddCallback<ClickEvent>(
                        inner, callback, data);
                    break;
                case EventType.FrameUpdate:
                    CallbackRegistry.AddCallback<FrameUpdateEvent>(
                        inner, callback, data);
                    UnityEventSynthesizer.AddListener
                        <FrameUpdateEvent>(inner);
                    break;
                case EventType.ApplicationFocus:
                    CallbackRegistry.AddCallback
                        <ApplicationFocusEvent>(
                        inner, callback, data);
                    UnityEventSynthesizer.AddListener
                        <ApplicationFocusEvent>(inner);
                    break;
                default:
                    throw new System.Exception("Unsupported event type: " + eventType);
            }
        }

        public void UnregisterCallback(EventType eventType,
            DynValue callback)
        {
            callback.CheckType(
                "VisualElementWrap.UnregisterCallback",
                DataType.Function);
            switch (eventType)
            {
                case EventType.Click:
                    CallbackRegistry.RemoveCallback<ClickEvent>(
                        inner, callback);
                    break;
                case EventType.FrameUpdate:
                    CallbackRegistry.RemoveCallback
                        <FrameUpdateEvent>(
                        inner, callback);
                    UnityEventSynthesizer.RemoveListener
                        <FrameUpdateEvent>(inner);
                    break;
                case EventType.ApplicationFocus:
                    CallbackRegistry.RemoveCallback
                        <ApplicationFocusEvent>(
                        inner, callback);
                    UnityEventSynthesizer.RemoveListener
                        <ApplicationFocusEvent>(inner);
                    break;
                default:
                    throw new System.Exception("Unsupported event type: " + eventType);
            }
        }
        #endregion

        #region Query
        // className is optional, even in Lua.
        public VisualElementWrap Q(string name,
            string className = null)
        {
            return new VisualElementWrap(inner.Q(name, className));
        }

        // Leave out `name` to query all elements.
        public UQueryStateWrap Query(string name = null,
            string className = null)
        {
            return new UQueryStateWrap(inner.Query(
                name, className).Build());
        }
        #endregion

        #region Class manipulation
        public IEnumerable<string> GetClasses()
            => inner.GetClasses();
        public bool ClassListContains(string className)
            => inner.ClassListContains(className);
        public void AddToClassList(string className)
            => inner.AddToClassList(className);
        public void RemoveFromClassList(string className)
            => inner.RemoveFromClassList(className);
        public void ClearClassList()
            => inner.ClearClassList();
        public void EnableInClassList(string className, bool enable)
            => inner.EnableInClassList(className, enable);
        public void ToggleInClassList(string className)
            => inner.ToggleInClassList(className);
        #endregion

        #region Style shortcuts
        public void SetDisplay(bool display)
        {
            style.display = display ? DisplayStyle.Flex
                : DisplayStyle.None;
        }

        public void SetVisibility(bool visible)
        {
            style.visibility = visible ? Visibility.Visible
                : Visibility.Hidden;
        }
        #endregion
    }

    [MoonSharpUserData]
    public class UQueryStateWrap
    {
        public UQueryState<VisualElement> inner { get; private set; }
        [MoonSharpHidden]
        public UQueryStateWrap(UQueryState<VisualElement> s)
        {
            inner = s;
        }

        public void ForEach(DynValue f)
        {
            f.CheckType("UQueryStateApi.ForEach", DataType.Function);
            inner.ForEach((VisualElement e) =>
            {
                f.Function.Call(new VisualElementWrap(e));
            });
        }
    }
}
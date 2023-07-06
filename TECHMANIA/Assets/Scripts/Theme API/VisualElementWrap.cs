using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using MoonSharp.Interpreter;
using UnityEngine.UIElements;
using System.Reflection;
using System;

namespace ThemeApi
{
    // VisualElement and related classes are wrapped in these API
    // class because:
    // - Lua doesn't support generics
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
            if (e == null)
            {
                Debug.LogWarning("Creating VisualElementWrap around null.");
            }
            inner = e;
        }

        #region Properties
        public bool enabledInHierarchy => inner.enabledInHierarchy;
        public bool enabledSelf => inner.enabledSelf;
        public void SetEnabled(bool enabled)
        {
            inner.SetEnabled(enabled);
        }

        public string name
        {
            get { return inner.name; }
            set { inner.name = value; }
        }
        
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

        public IResolvedStyle resolvedStyle => inner.resolvedStyle;
        public IStyle style => inner.style;
        public ITransform transform => inner.transform;

        public Rect contentRect => inner.contentRect;
        public Rect localBound => inner.localBound;
        public Rect worldBound => inner.worldBound;
        #endregion

        #region Subclass-specific
        private void CheckType(System.Type type, string targetMember)
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
            get { return (inner as TextElement).text; }
            set { (inner as TextElement).text = value; }
        }

        public float lowValue
        {
            get
            {
                if (inner is Slider)
                    return (inner as Slider).lowValue;
                if (inner is SliderInt)
                    return (inner as SliderInt).lowValue;
                if (inner is Scroller)
                    return (inner as Scroller).lowValue;
                throw new System.Exception($"VisualElement {name} is not a Slider, SliderInt or Scroller, and therefore does not have the 'lowValue' member.");
            }
            set
            {
                if (inner is Slider)
                    (inner as Slider).lowValue = value;
                else if (inner is SliderInt)
                    (inner as SliderInt).lowValue = (int)value;
                else if (inner is Scroller)
                    (inner as Scroller).lowValue = value;
                else throw new System.Exception($"VisualElement {name} is not a Slider, SliderInt or Scroller, and therefore does not have the 'lowValue' member.");
            }
        }

        public float highValue
        {
            get
            {
                if (inner is Slider)
                    return (inner as Slider).highValue;
                if (inner is SliderInt)
                    return (inner as SliderInt).highValue;
                if (inner is Scroller)
                    return (inner as Scroller).highValue;
                throw new System.Exception($"VisualElement {name} is not a Slider, SliderInt or Scroller, and therefore does not have the 'highValue' member.");
            }
            set
            {
                if (inner is Slider)
                    (inner as Slider).highValue = value;
                else if (inner is SliderInt)
                    (inner as SliderInt).highValue = (int)value;
                else if (inner is Scroller)
                    (inner as Scroller).highValue = value;
                else throw new System.Exception($"VisualElement {name} is not a Slider, SliderInt or Scroller, and therefore does not have the 'highValue' member.");
            }
        }

        public float value
        {
            get
            {
                if (inner is Slider)
                    return (inner as Slider).value;
                if (inner is SliderInt)
                    return (inner as SliderInt).value;
                if (inner is Scroller)
                    return (inner as Scroller).value;
                if (inner is IntegerField)
                    return (inner as IntegerField).value;
                if (inner is FloatField)
                    return (inner as FloatField).value;
                throw new System.Exception($"VisualElement {name} is not a Slider, SliderInt, Scroller, IntegerField or FloatField, and therefore does not have the 'value' member.");
            }
            set
            {
                if (inner is Slider)
                    (inner as Slider).value = value;
                else if (inner is SliderInt)
                    (inner as SliderInt).value = (int)value;
                else if (inner is Scroller)
                    (inner as Scroller).value = value;
                else if (inner is IntegerField)
                    (inner as IntegerField).value = (int)value;
                else if (inner is FloatField)
                    (inner as FloatField).value = value;
                else throw new System.Exception($"VisualElement {name} is not a Slider, SliderInt, Scroller, IntegerField or FloatField, and therefore does not have the 'value' member.");
            }
        }

        public string stringValue
        {
            get
            {
                if (inner is DropdownField)
                    return (inner as DropdownField).value;
                if (inner is TextField)
                    return (inner as TextField).value;
                throw new System.Exception($"VisualElement {name} is not a DropdownField or TextField, and therefore does not have the 'stringValue' member.");
            }
            set
            {
                if (inner is DropdownField)
                    (inner as DropdownField).value = value;
                else if (inner is TextField)
                    (inner as TextField).value = value;
                else throw new System.Exception($"VisualElement {name} is not a DropdownField or TextField, and therefore does not have the 'stringValue' member.");
            }
        }

        public bool boolValue
        {
            get
            {
                if (inner is Toggle)
                    return (inner as Toggle).value;
                throw new Exception($"VisualElement {name} is not a Toggle, and therefore does not have the 'boolValue' member.");
            }
            set
            {
                if (inner is Toggle)
                    (inner as Toggle).value = value;
                else throw new Exception($"VisualElement {name} is not a Toggle, and therefore does not have the 'boolValue' member.");
            }
        }

        public VisualElementWrap horizontalScroller
        {
            get
            {
                CheckType(typeof(ScrollView), "horizontalScroller");
                return new VisualElementWrap(
                    (inner as ScrollView).horizontalScroller);
            }
        }

        public VisualElementWrap verticalScroller
        {
            get
            {
                CheckType(typeof(ScrollView), "verticalScroller");
                return new VisualElementWrap(
                    (inner as ScrollView).verticalScroller);
            }
        }

        public List<string> choices
        {
            get
            {
                CheckType(typeof(DropdownField), "choices");
                return (inner as DropdownField).choices;
            }
            set
            {
                CheckType(typeof(DropdownField), "choices");
                (inner as DropdownField).choices = value;
            }
        }

        public int index
        {
            get
            {
                CheckType(typeof(DropdownField), "index");
                return (inner as DropdownField).index;
            }
            set
            {
                CheckType(typeof(DropdownField), "index");
                (inner as DropdownField).index = value;
            }
        }

        public void SetValueWithoutNotify(string newValue)
        {
            if (inner is DropdownField)
                (inner as DropdownField).SetValueWithoutNotify(newValue);
            else if (inner is TextField)
                (inner as TextField).SetValueWithoutNotify(newValue);
            else throw new System.Exception($"VisualElement {name} is not a DropdownField or TextField, and therefore does not have the 'SetValueWithoutNotify' member.");
        }

        public void SetValueWithoutNotify(float newValue)
        {
            if (inner is Slider)
                (inner as Slider).SetValueWithoutNotify(newValue);
            else if (inner is SliderInt)
                (inner as SliderInt).SetValueWithoutNotify(
                    (int)newValue);
            else if (inner is Scroller)
                // There is no SetValueWithoutNotify method on Scroller
                (inner as Scroller).value = newValue;
            else if (inner is IntegerField)
                (inner as IntegerField).SetValueWithoutNotify(
                    (int)newValue);
            else if (inner is FloatField)
                (inner as FloatField).SetValueWithoutNotify(newValue);
            else throw new System.Exception($"VisualElement {name} is not a Slider, SliderInt, Scroller, IntegerField or FloatField, and therefore does not have the 'SetValueWithoutNotify' member.");
        }

        public void SetValueWithoutNotify(bool newValue)
        {
            if (inner is Toggle)
                (inner as Toggle).SetValueWithoutNotify(newValue);
            else throw new Exception($"VisualElement {name} is not a Toggle, and therefore does not have the 'boolValue' member.");
        }

        public void ScrollTo(VisualElementWrap child)
        {
            CheckType(typeof(ScrollView), "ScrollTo");
            (inner as ScrollView).ScrollTo(child.inner);
        }
        #endregion

        #region Events
        // https://docs.unity3d.com/2022.3/Documentation/Manual/UIE-Events-Reference.html
        public enum EventType
        {
            // Capture events: omitted

            // Change events
            ChangeBool,
            ChangeInt,
            ChangeFloat,
            ChangeString,

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
            // (mouse fires both, touchscreen only fires
            // pointer events, so we don't use the mouse events)
            PointerDown,
            PointerUp,
            PointerMove,
            PointerEnter,  // pointer enters element or any descendant
            PointerLeave,  // pointer leaves element and all descendants
            PointerOver,  // pointer enters element but not descendant
            PointerOut,  // pointer leaves element
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

        [MoonSharpHidden]
        public static System.Type EventTypeEnumToType(EventType t)
        {
            return t switch
            {
                EventType.ChangeBool => typeof(ChangeEvent<bool>),
                EventType.ChangeInt => typeof(ChangeEvent<int>),
                EventType.ChangeFloat => typeof(ChangeEvent<float>),
                EventType.ChangeString => typeof(ChangeEvent<string>),
                EventType.KeyDown => typeof(KeyDownEvent),
                EventType.KeyUp => typeof(KeyUpEvent),
                EventType.PointerDown => typeof(PointerDownEvent),
                EventType.PointerUp => typeof(PointerUpEvent),
                EventType.PointerEnter => typeof(PointerEnterEvent),
                EventType.PointerLeave => typeof(PointerLeaveEvent),
                EventType.PointerOver => typeof(PointerOverEvent),
                EventType.PointerOut => typeof(PointerOutEvent),
                EventType.Click => typeof(ClickEvent),
                EventType.FrameUpdate => typeof(FrameUpdateEvent),
                EventType.ApplicationFocus =>
                    typeof(ApplicationFocusEvent),
                _ => throw new System.Exception(
                    "Unsupported event type: " + t)
            };
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
            System.Type genericType = EventTypeEnumToType(eventType);
            switch (eventType)
            {
                case EventType.FrameUpdate:
                    UnityEventSynthesizer.AddListener
                        <FrameUpdateEvent>(inner);
                    break;
                case EventType.ApplicationFocus:
                    UnityEventSynthesizer.AddListener
                        <ApplicationFocusEvent>(inner);
                    break;
            }
            MethodInfo methodInfo = typeof(CallbackRegistry)
                .GetMethod("AddCallback",
                BindingFlags.Static | BindingFlags.Public)
                .MakeGenericMethod(genericType);
            methodInfo.Invoke(null, new object[] {
                inner, callback, data });
        }

        public void UnregisterCallback(EventType eventType,
            DynValue callback)
        {
            callback.CheckType(
                "VisualElementWrap.UnregisterCallback",
                DataType.Function);
            System.Type genericType = EventTypeEnumToType(eventType);
            switch (eventType)
            {
                case EventType.FrameUpdate:
                    UnityEventSynthesizer.RemoveListener
                        <FrameUpdateEvent>(inner);
                    break;
                case EventType.ApplicationFocus:
                    UnityEventSynthesizer.RemoveListener
                        <ApplicationFocusEvent>(inner);
                    break;
            }
            MethodInfo methodInfo = typeof(CallbackRegistry)
                .GetMethod("RemoveCallback",
                BindingFlags.Static | BindingFlags.Public)
                .MakeGenericMethod(genericType);
            methodInfo.Invoke(null, new object[] {
                inner, callback });
        }

        public void UnregisterAllCallbackOfType(EventType eventType)
        {
            System.Type genericType = EventTypeEnumToType(eventType);
            switch (eventType)
            {
                case EventType.FrameUpdate:
                    UnityEventSynthesizer.RemoveListener
                        <FrameUpdateEvent>(inner);
                    break;
                case EventType.ApplicationFocus:
                    UnityEventSynthesizer.RemoveListener
                        <ApplicationFocusEvent>(inner);
                    break;
            }
            MethodInfo methodInfo = typeof(CallbackRegistry)
                .GetMethod("RemoveAllCallbackOfTypeOn",
                BindingFlags.Static | BindingFlags.Public)
                .MakeGenericMethod(genericType);
            methodInfo.Invoke(null, new object[] { inner });
        }

        public void UnregisterAllCallback()
        {
            UnregisterAllCallback(inner);
        }

        private static void UnregisterAllCallback(VisualElement element)
        {
            UnityEventSynthesizer.RemoveListenerForAllEventTypes(
                element);
            CallbackRegistry.RemoveAllCallbackOn(element);
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

        #region USS class manipulation
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
        public bool display
        {
            get { return style.display == DisplayStyle.Flex; }
            set {
                style.display = value
                    ? DisplayStyle.Flex
                    : DisplayStyle.None;
            }
        }

        public bool visible
        {
            get { return inner.visible; }
            set { inner.visible = value; }
        }

        public Texture2D backgroundImage
        {
            get { return style.backgroundImage.value.texture; }
            set { style.backgroundImage = new StyleBackground(value); }
        }
        #endregion

        #region DOM

        public int childCount => inner.childCount;
        public VisualElementWrap parent =>
            new VisualElementWrap(inner.parent);

        public IEnumerable<VisualElementWrap> Children()
        {
            List<VisualElementWrap> children = new 
                List<VisualElementWrap>();
            foreach (VisualElement child in inner.Children())
            {
                children.Add(new VisualElementWrap(child));
            }
            return children;
        }

        public VisualElementWrap InstantiateTemplate(string name)
        {
            VisualTreeAsset treeAsset = GlobalResource
                .GetThemeContent<VisualTreeAsset>(name);
            if (treeAsset == null)
            {
                throw new System.Exception(
                    "Visual tree asset not found: " + name);
            }

            TemplateContainer templateContainer =
                treeAsset.Instantiate();
            inner.Add(templateContainer);
            return new VisualElementWrap(templateContainer);
        }

        public VisualElementWrap AddEmptyChild()
        {
            VisualElement newElement = new VisualElement();
            inner.Add(newElement);
            return new VisualElementWrap(newElement);
        }

        public void RemoveFromHierarchy()
        {
            // Recursively remove all event handlers on all children.
            Action<VisualElement> unregisterAllCallbackRecursively
                = null;
            unregisterAllCallbackRecursively =
                (VisualElement element) =>
                {
                    UnregisterAllCallback(element);
                    foreach (VisualElement child in element.Children())
                    {
                        unregisterAllCallbackRecursively(child);
                    }
                };
            unregisterAllCallbackRecursively(inner);

            inner.RemoveFromHierarchy();
        }

        public void RemoveAllChildren()
        {
            // Make a copy of the child list so we don't modify
            // the child list while enumerating it.
            List<VisualElement> children = new List<VisualElement>();
            foreach (VisualElement child in inner.Children())
            {
                children.Add(child);
            }
            foreach (VisualElement child in children)
            {
                new VisualElementWrap(child).RemoveFromHierarchy();
            }
        }

        public void AddChild(VisualElementWrap child)
        {
            inner.Add(child.inner);
        }

        public void InsertChild(int index, VisualElementWrap child)
        {
            inner.Insert(index, child.inner);
        }

        public void PlaceBehind(VisualElementWrap sibling)
        {
            inner.PlaceBehind(sibling.inner);
        }

        public void PlaceInFront(VisualElementWrap sibling)
        {
            inner.PlaceInFront(sibling.inner);
        }

        public void BringToFront()
        {
            inner.BringToFront();
        }

        public void SendToBack()
        {
            inner.SendToBack();
        }

        public bool Equals(VisualElementWrap other)
        {
            return inner == other.inner;
        }
        #endregion

        #region Custom mesh
        // Parameters: VisualElementWrap, MeshGenerationContext
        // Call VisualElementWrap.contentRect for draw boundaries.
        // (0, 0) is top left, positions are in pixels.
        //
        // When setting Vertex's, make sure to create new vectors
        // instead of modifying existing ones.
        // Correct: vertex.position = new Vector3(...)
        // Wrong: vertex.position.x = ...
        //
        // Also remember to set the tint on each vertex; it seems
        // to default to (0, 0, 0, 0).
        public void SetMeshGeneratorFunction(DynValue function)
        {
            inner.generateVisualContent =
                (MeshGenerationContext context) =>
                {
                    function.Function.Call(this, context);
                };
        }

        public void MarkDirtyRepaint()
        {
            inner.MarkDirtyRepaint();
        }
        #endregion

        #region Focus
        public void Focus()
        {
            inner.Focus();
        }

        public VisualElementWrap FocusedElement()
        {
            return new VisualElementWrap(
                inner.focusController.focusedElement as VisualElement);
        }
        #endregion

        #region Transform
        public Vector2 LocalSpaceToScreenSpace(Vector2 localSpace)
        {
            return VisualElementTransform.LocalSpaceToScreenSpace(
                inner, localSpace);
        }

        public Vector2 ScreenSpaceToLocalSpace(Vector2 screenSpace)
        {
            return VisualElementTransform
                .ScreenSpaceToLocalSpace(inner, screenSpace);
        }

        public bool ContainsPointInScreenSpace(Vector2 screenSpace)
        {
            return VisualElementTransform
                .ElementContainsPointInScreenSpace(inner, screenSpace);
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
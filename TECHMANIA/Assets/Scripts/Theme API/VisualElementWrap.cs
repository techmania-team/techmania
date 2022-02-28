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
        public bool pickable
        {
            get { return inner.pickingMode == PickingMode.Position; }
            set
            {
                inner.pickingMode = value ?
                    PickingMode.Position : PickingMode.Ignore;
            }
        }
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
        // Callback parameters:
        // 1. The VisualElementWrap receiving the event
        // 2. The data (if registered with the ..WithData variant)
        // 3. The event

        public void OnClick(DynValue callback)
        {
            callback.CheckType("VisualElementWrap.OnClick",
                DataType.Function);
            CallbackRegistry.AddCallback<ClickEvent>(
                inner, callback);
        }

        public void OnClickWithData(DynValue callback, DynValue data)
        {
            callback.CheckType("VisualElementWrap.OnClickWithData",
                DataType.Function);
            CallbackRegistry.AddCallbackWithData<ClickEvent>(
                inner, callback, data);
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
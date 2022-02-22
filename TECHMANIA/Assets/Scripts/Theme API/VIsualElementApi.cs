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
    [MoonSharpUserData]
    public class VisualElementApi
    {
        private VisualElement element;

        [MoonSharpHidden]
        public VisualElementApi(VisualElement e)
        {
            element = e;
        }

        #region Properties
        public int childCount => element.childCount;
        public bool enabledInHierarchy => element.enabledInHierarchy;
        public bool enabledSelf => element.enabledSelf;
        public string name => element.name;
        public VisualElementApi parent =>
            new VisualElementApi(element.parent);
        public bool visible => element.visible;
        #endregion

        #region Query
        // className is optional, even in Lua.
        public VisualElementApi Q(string name,
            string className = null)
        {
            return new VisualElementApi(element.Q(name, className));
        }

        // Leave out `name` to query all elements.
        public UQueryStateApi Query(string name = null,
            string className = null)
        {
            return new UQueryStateApi(element.Query(
                name, className).Build());
        }
        #endregion
    }

    [MoonSharpUserData]
    public class UQueryStateApi
    {
        private UQueryState<VisualElement> state;
        [MoonSharpHidden]
        public UQueryStateApi(UQueryState<VisualElement> s)
        {
            state = s;
        }

        public void ForEach(DynValue f)
        {
            f.CheckType("UQueryStateApi.ForEach", DataType.Function);
            state.ForEach((VisualElement e) =>
            {
                f.Function.Call(e);
            });
        }
    }
}
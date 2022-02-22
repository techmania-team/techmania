using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using MoonSharp.Interpreter;
using UnityEngine.UIElements;

namespace ThemeApi
{
    [MoonSharpUserData]
    public class Techmania
    {
        #region UIDocument exposure
        private UIDocument uiDocument => Object
            .FindObjectOfType<UIDocument>();
        public VisualTreeAsset visualTreeAsset => 
            uiDocument.visualTreeAsset;
        public VisualElementApi rootVisualElement =>
            new VisualElementApi(uiDocument.rootVisualElement);
        public PanelSettings panelSettings => 
            uiDocument.panelSettings;
        #endregion
    }
}

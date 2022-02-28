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
        private static UIDocument uiDocument;
        private static AudioManager audioManager;
        [MoonSharpHidden]
        public static void Prepare()
        {
            uiDocument = Object.FindObjectOfType<UIDocument>();
            audioManager = new AudioManager();
            CallbackRegistry.Prepare();
        }
        #region UIDocument exposure
        public VisualTreeAsset visualTreeAsset => 
            uiDocument.visualTreeAsset;
        public VisualElementWrap rootVisualElement =>
            new VisualElementWrap(uiDocument.rootVisualElement);
        public PanelSettings panelSettings => 
            uiDocument.panelSettings;
        #endregion

        #region Audio and video
        public AudioManager audio => audioManager;
        #endregion
    }
}

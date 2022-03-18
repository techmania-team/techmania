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
        private static StyleHelper styleHelper;
        [MoonSharpHidden]
        public static void Prepare()
        {
            uiDocument = Object.FindObjectOfType<UIDocument>();
            audioManager = new AudioManager();
            styleHelper = new StyleHelper();
            CallbackRegistry.Prepare();
            UnityEventSynthesizer.Prepare();
            CoroutineRunner.Prepare();
        }
        #region UIDocument exposure
        public VisualTreeAsset visualTreeAsset => 
            uiDocument.visualTreeAsset;
        public VisualElementWrap rootVisualElement =>
            new VisualElementWrap(uiDocument.rootVisualElement);
        public PanelSettings panelSettings => 
            uiDocument.panelSettings;

        public StyleHelper style => styleHelper;
        #endregion

        #region Audio and video
        public AudioManager audio => audioManager;
        #endregion

        #region Miscellaneous
        public void ExecuteScript(string name)
        {
            string script = GlobalResource
                .GetThemeContent<TextAsset>(name).text;
            ScriptSession.Execute(script);
        }

        public void StartCoroutine(DynValue function)
        {
            function.CheckType("Techmania.StartCoroutine", 
                DataType.Function);
            CoroutineRunner.Add(
                ScriptSession.session.CreateCoroutine(function)
                .Coroutine);
        }
        #endregion
    }
}

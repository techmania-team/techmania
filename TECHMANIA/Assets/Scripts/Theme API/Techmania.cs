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
        [MoonSharpHidden]
        public static AudioManager audioManager { get; private set; }
        private static StyleHelper styleHelper;
        private static ThemeL10n themeL10n;

        [MoonSharpHidden]
        public static void Prepare()
        {
            uiDocument = Object.FindObjectOfType<UIDocument>();
            audioManager = new AudioManager();
            styleHelper = new StyleHelper();
            themeL10n = new ThemeL10n(uiDocument.rootVisualElement);
            CallbackRegistry.Prepare();
            UnityEventSynthesizer.Prepare();
        }

        #region UIDocument exposure
        public VisualTreeAsset visualTreeAsset => 
            uiDocument.visualTreeAsset;
        public VisualElementWrap root =>
            new VisualElementWrap(uiDocument.rootVisualElement);
        public PanelSettings panelSettings => 
            uiDocument.panelSettings;

        public StyleHelper style => styleHelper;
        #endregion

        #region Audio and video
        public AudioManager audio => audioManager;
        #endregion

        #region Miscellaneous
        public static ThemeL10n l10n => themeL10n;
        public static Options options => Options.instance;
        public static Ruleset ruleset => Ruleset.instance;
        public static Paths paths => new Paths();

        public string LoadTextFile(string name)
        {
            return GlobalResource.GetThemeContent<TextAsset>(
                name).text;
        }

        public void ExecuteScript(string name)
        {
            string script = GlobalResource
                .GetThemeContent<TextAsset>(name).text;
            ScriptSession.Execute(script);
        }

        public int StartCoroutine(DynValue function)
        {
            function.CheckType("Techmania.StartCoroutine", 
                DataType.Function);
            return CoroutineRunner.Start(
                ScriptSession.session.CreateCoroutine(function)
                .Coroutine);
        }

        public void StopCoroutine(int id)
        {
            CoroutineRunner.Stop(id);
        }
        #endregion

        public void LogEvent(EventBase e)
        {
            string log = "bubbles: " + e.bubbles +
                "\ncurrentTarget: " + (e.currentTarget as VisualElement).name +
                "\ndispatch: " + e.dispatch +
                "\nisDefaultPrevented: " + e.isDefaultPrevented +
                "\nisImmediatePropagationStopped: " + e.isImmediatePropagationStopped +
                "\nisPropagationStopped: " + e.isPropagationStopped +
                "\npropagationPhase: " + e.propagationPhase +
                "\ntarget: " + (e.target as VisualElement).name +
                "\ntricklesDown: " + e.tricklesDown;
            Debug.Log(log);
        }
    }
}

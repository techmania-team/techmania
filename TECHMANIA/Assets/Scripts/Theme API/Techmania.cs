using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using MoonSharp.Interpreter;
using UnityEngine.UIElements;
using FantomLib;

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

        public void SetThemeStyleSheet(string name)
        {
            ThemeStyleSheet sheet = GlobalResource.GetThemeContent
                <ThemeStyleSheet>(name);
            panelSettings.themeStyleSheet = sheet;
        }
        #endregion

        #region Data classes
        public static ThemeL10n l10n => themeL10n;
        public static Options options => Options.instance;
        public static Ruleset ruleset => Ruleset.instance;
        public static Paths paths => new Paths();
        #endregion

        #region Audio and video
        public AudioManager audio => audioManager;
        #endregion

        #region System dialogs
        // Returns the selected dialog if any; null if the user
        // cancels the dialog.
        public string OpenSelectFolderDialog(
            string title, string currentDirectory)
        {
#if UNITY_ANDROID
            AndroidPlugin.OpenStorageFolder(null, "OnAndroidTracksFolderSelected", "", true);
            return null;
#else
            string[] folders = SFB.StandaloneFileBrowser
                .OpenFolderPanel(title,
                currentDirectory,
                multiselect: false);
            if (folders.Length == 1)
            {
                return folders[0];
            }
            else return null;
#endif
        }
        #endregion

        #region Miscellaneous
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

        // Returns one of "Windows", "Linux", "macOS", "Android"
        // "iOS", and "Unknown".
        public string GetPlatform()
        {
#if UNITY_EDITOR_WIN || UNITY_STANDALONE_WIN
            return "Windows";
#elif UNITY_EDITOR_LINUX || UNITY_STANDALONE_LINUX
            return "Linux";
#elif UNITY_EDITOR_OSX || UNITY_STANDALONE_OSX
            return "macOS";
#elif UNITY_ANDROID
            return "Android";
#elif UNITY_IOS
            return "iOS";
#else
            return "Unknown";
#endif
        }

        public void OpenURL(string url)
        {
            Application.OpenURL(url);
        }

        public static void Quit()
        {
#if UNITY_EDITOR
            UnityEditor.EditorApplication.isPlaying = false;
#else
            Application.Quit();
#endif
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

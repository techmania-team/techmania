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
        [MoonSharpHidden]
        public static Techmania instance { get; private set; }

        [MoonSharpHidden]
        public Techmania()
        {
            instance = this;

            uiDocument = Object.FindObjectOfType<UIDocument>();
            root = new VisualElementWrap(uiDocument.rootVisualElement);

            l10n = new ThemeL10n(uiDocument.rootVisualElement);
            resources = new GlobalResource();
            gameSetup = new GameSetup();
            game = new GameState();
            paths = new Paths();
            editor = new EditorInterface();

            audio = new AudioManager();

            CallbackRegistry.Prepare();
            UnityEventSynthesizer.Prepare();

            EditorContext.inPreview = false;
        }

        #region UIDocument exposure
        private UIDocument uiDocument;
        public VisualTreeAsset visualTreeAsset => 
            uiDocument.visualTreeAsset;
        public VisualElementWrap root { get; private set; }
        public PanelSettings panelSettings => 
            uiDocument.panelSettings;

        public void SetThemeStyleSheet(string name)
        {
            ThemeStyleSheet sheet = GlobalResource.GetThemeContent
                <ThemeStyleSheet>(name);
            panelSettings.themeStyleSheet = sheet;
        }
        #endregion

        #region Data classes
        public Options options => Options.instance;
        public Ruleset ruleset => Ruleset.instance;
        public Records records => Records.instance;
        public ThemeL10n l10n { get; private set; }
        public GlobalResource resources { get; private set; }
        public GameSetup gameSetup { get; private set; }
        public GameState game { get; private set; }
        public Paths paths { get; private set; }
        public EditorInterface editor { get; private set; }
        public SkinPreview skinPreview => SkinPreview.instance;
        #endregion

        #region Audio and video
        public AudioManager audio { get; private set; }
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
        public void ExecuteScript(string name)
        {
            string script = GlobalResource
                .GetThemeContent<TextAsset>(name).text;
            Debug.Log("Executing: " + name);
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

        // Does nothing if Discord Rich Presence is turned off
        // from options, or running on unsupported platform.
        //
        // If details and state are both set, details is shown
        // above state.
        //
        // If multiple calls are made with showElapsedTime = true,
        // the 2nd call and onwards will not reset the elapsed time.
        public void SetDiscordActivity(string details, string state,
            bool showElapsedTime = false)
        {
            DiscordController.SetActivity(details, state,
                showElapsedTime);
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

        // For ScriptSession to inject the enum table.
        public Table @enum;
    }
}

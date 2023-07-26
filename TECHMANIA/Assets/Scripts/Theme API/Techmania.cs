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
        [MoonSharpHidden]
        public static Techmania instance { get; private set; }

        [MoonSharpHidden]
        public Techmania()
        {
            instance = this;

            uiDocument = Object.FindObjectOfType<UIDocument>();
            root = new VisualElementWrap(uiDocument.rootVisualElement);

            l10n = new ThemeL10n(uiDocument.rootVisualElement);
            paths = UserData.CreateStatic(typeof(Paths));
            resources = UserData.CreateStatic(typeof(GlobalResource));
            io = UserData.CreateStatic(typeof(IO));
            gameSetup = new GameSetup();
            game = new GameState();
            editor = new EditorInterface();

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

        public void SetPanelSettings(string path)
        {
            PanelSettings settings = GlobalResource
                .GetThemeContent<PanelSettings>(path);
            uiDocument.panelSettings = settings;
        }
        #endregion

        #region Data classes
        public Options options => Options.instance;
        public Ruleset ruleset => Ruleset.instance;
        public Records records => Records.instance;
        public ThemeL10n l10n { get; private set; }
        public DynValue paths;  // Of type Paths
        public DynValue resources;  // Of type GlobalResource
        public DynValue io;  // Of type IO
        public GameSetup gameSetup { get; private set; }
        public GameState game { get; private set; }
        public EditorInterface editor { get; private set; }
        public SkinPreview skinPreview => SkinPreview.instance;
        public CalibrationPreview calibrationPreview =>
            CalibrationPreview.instance;
        public AudioSourceManager audio => AudioSourceManager.instance;
        #endregion

        #region System dialogs
        private static string SelectDialogCallback;
        // Returns the selected files if any. Returns 0 values if the
        // user cancels the dialog.
        public void OpenSelectFileDialog(
            string title, string currentDirectory, bool multiSelect,
            string[] supportedExtensionsWithoutDot, string callback)
        {
            SelectDialogCallback = callback;
#if UNITY_ANDROID
            GameObject.Find("/Main UIDocument")
                .GetComponent<AndroidHelper>()
                .OnSelectFile();
#else
            SFB.ExtensionFilter[] extensionFilters = new 
                SFB.ExtensionFilter[2];
            extensionFilters[0] = new SFB.ExtensionFilter(
                L10n.GetString(
                    "track_setup_resource_tab_import_dialog_supported_formats", 
                L10n.Instance.System),
                supportedExtensionsWithoutDot);
            extensionFilters[1] = new SFB.ExtensionFilter(
                L10n.GetString(
                    "track_setup_resource_tab_import_dialog_all_files",
                L10n.Instance.System), "*");
            Script script = ScriptSession.session;
            script.Call(
                script.Globals.Get(SelectDialogCallback),
                SFB.StandaloneFileBrowser.OpenFilePanel(
                    title,
                    currentDirectory, extensionFilters, multiSelect
                )
            );
#endif
        }
#if UNITY_ANDROID
        public static void OnAndroidFileSelected(string path)
        {
            Script script = ScriptSession.session;
            script.Call(script.Globals.Get(SelectDialogCallback), path);
        }
#endif
        // Returns the selected dialog if any; null if the user
        // cancels the dialog.
        public void OpenSelectFolderDialog(
            string title, string currentDirectory, string callback)
        {
            SelectDialogCallback = callback;
#if UNITY_IOS
            return;
#elif UNITY_ANDROID
            GameObject.Find("/Main UIDocument")
                .GetComponent<AndroidHelper>()
                .OnSelectFolder();
#else
            string[] folders = SFB.StandaloneFileBrowser
                .OpenFolderPanel(title,
                currentDirectory,
                multiselect: false);
            if (folders.Length == 1)
            {
                Script script = ScriptSession.session;
                script.Call(script.Globals.Get(SelectDialogCallback), folders[0]);
            }
#endif
        }
#if UNITY_ANDROID
        public static void OnAndroidFolderSelected(string folder)
        {
            Script script = ScriptSession.session;
            script.Call(script.Globals.Get(SelectDialogCallback), folder);
        }
#endif
        #endregion

        #region Script execution
        
        public static void ExecuteScriptFromTheme(string path)
        {
            string script = GlobalResource.GetThemeContent<TextAsset>
                (path)?.text;
            if (string.IsNullOrEmpty(script)) return;

            if (Application.isEditor)
            {
                // If in editor, construct the full path to the
                // script file, in order to provide debugging support.
                string fullPath = System.IO.Path.Combine(
                    System.IO.Path.GetDirectoryName(
                        Application.dataPath),
                    path);
                ScriptSession.Execute(script, fullPath);
            }
            else
            {
                ScriptSession.Execute(script);
            }
        }

        public static void ExecuteScript(string script)
        {
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

        public bool IsCoroutineRunning(int id)
        {
            return CoroutineRunner.IsRunning(id);
        }

        public void StopCoroutine(int id)
        {
            CoroutineRunner.Stop(id);
        }
        #endregion

        #region Miscellaneous
        public void HideVfxAndComboText()
        {
            TopLevelObjects.instance.vfxComboCanvas
                .GetComponent<CanvasGroup>().alpha = 0f;
        }

        public void RestoreVfxAndComboText()
        {
            TopLevelObjects.instance.vfxComboCanvas
                .GetComponent<CanvasGroup>().alpha = 1f;
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

        public void OpenURL(string url)
        {
            Application.OpenURL(url);
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

        public string Version()
        {
            return Application.version;
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

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

        public VisualElementWrap WrapVisualElement(VisualElement e)
        {
            return new VisualElementWrap(e);
        }

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
        public Statistics stats => Statistics.instance;
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
        public AudioManager audio => AudioManager.instance;
        #endregion

        #region System dialogs
        // DEPRECATED
        // A synchronous version of OpenSelectFileDialog. Only works
        // on Windows.
        public string[] OpenSelectFileDialog(string title,
            string currentDirectory, bool multiSelect,
            string[] supportedExtensionsWithoutDot)
        {
#if UNITY_ANDROID
            return null;
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
            return SFB.StandaloneFileBrowser.OpenFilePanel(
                title, currentDirectory,
                extensionFilters, multiSelect);
#endif
        }

        // Asynchronous because the process on Android may take
        // multiple frames.
        // If the user selects one or more files, the callback
        // will be called with an array of paths.
        // If the user cancels the dialog, the callback will not
        // be called.
        //
        // On Windows, the callback will be called in the same frame.
        // On Android, the callback may be called in a future frame.
        //     All arguments other than callback will be ignored, and
        //     the user cannot select more than 1 file.
        public void OpenSelectFileDialog(
            string title, string currentDirectory, bool multiSelect,
            string[] supportedExtensionsWithoutDot, DynValue callback)
        {
#if UNITY_ANDROID
            TopLevelObjects.instance.mainUiDocument
                .GetComponent<AndroidHelper>()
                .OnSelectFile((string path) =>
                    callback.Function.Call(new string[] { path }));
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
            string[] paths = SFB.StandaloneFileBrowser.OpenFilePanel(
                title, currentDirectory,
                extensionFilters, multiSelect);
            if (paths.Length > 0)
            {
                callback.Function.Call(paths);
            };
#endif
        }

        // DEPRECATED
        // A synchronous version of OpenSelectFolderDialog. Only works
        // on Windows.
        public string OpenSelectFolderDialog(
            string title, string currentDirectory)
        {
#if UNITY_ANDROID
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

        // Asynchronous because the process on Android may take
        // multiple frames.
        // If the user selects a folder, the callback will be called
        // with its path.
        // If the user cancels the dialog, the callback will not
        // be called.
        //
        // On Windows, the callback will be called in the same frame.
        // On Android, the callback may be called in a future frame.
        //     All arguments other than callback will be ignored.
        // On iOS, this method does nothing.
        public void OpenSelectFolderDialog(
            string title, string currentDirectory, DynValue callback)
        {
#if UNITY_IOS
            return;
#elif UNITY_ANDROID
            TopLevelObjects.instance.mainUiDocument
                .GetComponent<AndroidHelper>()
                .OnSelectFolder((string path) =>
                    callback.Function.Call(path));
#else
            string[] folders = SFB.StandaloneFileBrowser
                .OpenFolderPanel(title,
                currentDirectory,
                multiselect: false);
            if (folders.Length == 1)
            {
                callback.Function.Call(folders[0]);
            }
#endif
        }
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

        public bool InEditor()
        {
#if UNITY_EDITOR
            return true;
#else
            return false;
#endif
        }

        public enum Platform
        {
            Windows,
            Linux,
            macOS,
            Android,
            iOS,
            Unknown
        }

        // The name contains "enum" because the string version
        // came first.
        public Platform GetPlatformEnum()
        {
#if UNITY_EDITOR_WIN || UNITY_STANDALONE_WIN
            return Platform.Windows;
#elif UNITY_EDITOR_LINUX || UNITY_STANDALONE_LINUX
            return Platform.Linux;
#elif UNITY_EDITOR_OSX || UNITY_STANDALONE_OSX
            return Platform.macOS;
#elif UNITY_ANDROID
            return Platform.Android;
#elif UNITY_IOS
            return Platform.iOS;
#else
            return Platform.Unknown;
#endif
        }

        // Returns one of "Windows", "Linux", "macOS", "Android"
        // "iOS", and "Unknown". Deprecated; new code should use
        // GetPlatformEnum().
        public string GetPlatform()
        {
            return GetPlatformEnum().ToString();
        }

        public string Version()
        {
            return Application.version;
        }

        public static void Quit()
        {
            Statistics.instance.SaveToFile();
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

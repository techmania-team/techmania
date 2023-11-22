using MoonSharp.VsCodeDebugger;
using System;
using System.Collections;
using System.Collections.Generic;
using System.Net.Sockets;
using ThemeApi;
using TMPro;
using UnityEngine;
using UnityEngine.UIElements;

public class BootScreen : MonoBehaviour
{
    public TextMeshProUGUI progressLine1;
    public TextMeshProUGUI progressLine2;
    public GameObject revertButtonContainer;
    public TextMeshProUGUI revertMessage;
    public MessageDialog messageDialog;

    [Header("Components referencing Audio Clips")]
    public MenuSfx menuSfx;
    public PatternPanel patternPanel;
    public CalibrationPreview calibrationPreview;

    private bool themeDecided;
    private Coroutine revertPromptCoroutine;

    public void StartBooting()
    {
        themeDecided = false;
        revertPromptCoroutine = StartCoroutine(
            ShowRevertDefaultThemePrompt());

        StartCoroutine(BootSequence());
    }

    #region Boot sequence
    private Status loadStatus;

    private IEnumerator LoadSkins()
    {
        bool loaded = false;
        progressLine1.text = L10n.GetStringAndFormat(
            "resource_loader_loading_skins", 1, 3);

        // Attempt 1: load currently selected skins.
        if (Options.instance.noteSkin != Options.kDefaultSkin ||
            Options.instance.vfxSkin != Options.kDefaultSkin ||
            Options.instance.comboSkin != Options.kDefaultSkin ||
            Options.instance.gameUiSkin != Options.kDefaultSkin)
        {
            loadStatus = null;
            GlobalResourceLoader.GetInstance().LoadAllSkins(
                ProgressCallback, CompleteCallback);
            yield return new WaitUntil(() => loadStatus != null);

            if (loadStatus.Ok())
            {
                loaded = true;
            }
            else
            {
                // Report error and revert to default skins.
                string message = L10n.GetStringAndFormat(
                    "resource_loader_skin_error_format",
                    loadStatus.errorMessage);
                messageDialog.Show(message);
                yield return new WaitUntil(() =>
                    !messageDialog.gameObject.activeSelf);

                Options.instance.noteSkin = Options.kDefaultSkin;
                Options.instance.vfxSkin = Options.kDefaultSkin;
                Options.instance.comboSkin = Options.kDefaultSkin;
                Options.instance.gameUiSkin = Options.kDefaultSkin;
            }
        }

        // Attempt 2: load default skins in a custom location.
        if (!loaded && Options.instance.customDataLocation)
        {
            loadStatus = null;
            GlobalResourceLoader.GetInstance().LoadAllSkins(
                ProgressCallback, CompleteCallback);
            yield return new WaitUntil(() => loadStatus != null);

            if (loadStatus.Ok())
            {
                loaded = true;
            }
            else
            {
                Options.instance.customDataLocation = false;
                Paths.ApplyCustomDataLocation();
            }
        }

        // Attempt 3: load default skins at the default location.
        if (!loaded)
        {
            loadStatus = null;
            GlobalResourceLoader.GetInstance().LoadAllSkins(
                ProgressCallback, CompleteCallback);
            yield return new WaitUntil(() => loadStatus != null);

            if (loadStatus.Ok())
            {
                loaded = true;
            }
        }

        if (!loaded)
        {
            // If all attempts fail, TECHMANIA can't start.
            messageDialog.Show(L10n.GetString(
                "resource_loader_cannot_load_default_skin"), () =>
                {
                    QuitGame();
                });
            yield return new WaitWhile(() => true);
        }
    }

    private IEnumerator LoadTheme()
    {
        bool loaded = false;
        progressLine1.text = L10n.GetStringAndFormat(
            "resource_loader_loading_theme", 3, 3);

        // Attempt 1: load currently selected theme.
        if (Options.instance.theme != Options.kDefaultTheme)
        {
            loadStatus = null;
            GlobalResourceLoader.GetInstance().LoadTheme(
                ProgressCallback, CompleteCallback);
            yield return new WaitUntil(() => loadStatus != null);

            if (loadStatus.Ok())
            {
                loaded = true;
            }
            else
            {
                // Report error and revert to default theme.
                string message = L10n.GetStringAndFormat(
                    "resource_loader_theme_error_format",
                    loadStatus.errorMessage);
                messageDialog.Show(message);
                yield return new WaitUntil(() =>
                    !messageDialog.gameObject.activeSelf);

                Options.instance.theme = Options.kDefaultTheme;
            }
        }

        // Attempt 2: load default theme in a custom location.
        if (!loaded && Options.instance.customDataLocation)
        {
            loadStatus = null;
            GlobalResourceLoader.GetInstance().LoadTheme(
                ProgressCallback, CompleteCallback);
            yield return new WaitUntil(() => loadStatus != null);

            if (loadStatus.Ok())
            {
                loaded = true;
            }
            else
            {
                Options.instance.customDataLocation = false;
                Paths.ApplyCustomDataLocation();
            }
        }

        // Attempt 3: load default theme at the default location.
        if (!loaded)
        {
            loadStatus = null;
            GlobalResourceLoader.GetInstance().LoadTheme(
                ProgressCallback, CompleteCallback);
            yield return new WaitUntil(() => loadStatus != null);

            if (loadStatus.Ok())
            {
                loaded = true;
            }
        }

        // If all attempts fail, TECHMANIA can't start.
        if (!loaded)
        {
            messageDialog.Show(L10n.GetString(
                "resource_loader_cannot_load_default_theme"), () =>
                {
                    QuitGame();
                });
            yield return new WaitWhile(() => true);
        }
    }

    private IEnumerator BootSequence()
    {
        // Initialize FMOD.
        FmodManager.instance.Initialize();

        // Convert and cache in-project audio clips.
        FmodManager.instance.ConvertAndCacheAudioClip(menuSfx.select);
        FmodManager.instance.ConvertAndCacheAudioClip(menuSfx.click);
        FmodManager.instance.ConvertAndCacheAudioClip(menuSfx.back);
        FmodManager.instance.ConvertAndCacheAudioClip(
            patternPanel.metronome1);
        FmodManager.instance.ConvertAndCacheAudioClip(
            patternPanel.metronome2);
        FmodManager.instance.ConvertAndCacheAudioClip(
            patternPanel.assistTick);
        FmodManager.instance.ConvertAndCacheAudioClip(
            calibrationPreview.backingTrack);
        FmodManager.instance.ConvertAndCacheAudioClip(
            calibrationPreview.kick);
        FmodManager.instance.ConvertAndCacheAudioClip(
            calibrationPreview.snare);

        // Step 1: load skins.
        yield return LoadSkins();

        // Step 2: load track list. No sub-coroutine since errors
        // in this step are not fatal.
        progressLine1.text = L10n.GetStringAndFormat(
            "resource_loader_loading_track_list", 2, 3);
        loadStatus = null;
        GlobalResourceLoader.GetInstance().LoadTrackList(
            ProgressCallback, CompleteCallback);
        yield return new WaitUntil(() => loadStatus != null);
        yield return new WaitUntil(() => themeDecided);

        // Step 3: load theme.
        yield return LoadTheme();

        // Load main tree and main script.
        string mainTreePath = "assets/ui/maintree.uxml";
        VisualTreeAsset mainTree = GlobalResource.GetThemeContent
            <VisualTreeAsset>(mainTreePath);
        if (mainTree == null)
        {
            messageDialog.Show($"{L10n.GetString("theme_error_critical_file_missing")}\n\n{mainTreePath}\n\n{L10n.GetString("theme_error_instruction")}", () => QuitGame());
            yield break;
        }
        string mainScriptPath = "assets/ui/mainscript.txt";
        TextAsset mainScript = GlobalResource.GetThemeContent
            <TextAsset>(mainScriptPath);
        if (mainScript == null)
        {
            messageDialog.Show($"{L10n.GetString("theme_error_critical_file_missing")}\n\n{mainScriptPath}\n\n{L10n.GetString("theme_error_instruction")}", () => QuitGame());
            yield break;
        }

        // Display UIDocument.
        TopLevelObjects.instance.mainUiDocument.visualTreeAsset
            = GlobalResource.themeContent[mainTreePath]
            as VisualTreeAsset;

        // Execute main script.
        ThemeApi.ScriptSession.Prepare();
        try
        {
            ThemeApi.Techmania.ExecuteScriptFromTheme(mainScriptPath);
        }
        catch (ThemeApi.ApiNotSupportedException)
        {
            messageDialog.Show($"{L10n.GetString("theme_error_api_not_supported")}\n\n{L10n.GetString("theme_error_instruction")}", () => QuitGame());
            yield break;
        }

        // Start debug server if running in Unity editor.
        if (Application.isEditor)
        {
            StartMoonSharpDebugServer();
        }
        else
        {
            debugServer = null;
        }

        // If everything worked up to this point, we can hide
        // main canvas and finish the boot sequence.
        TopLevelObjects.instance.eventSystem.gameObject
            .SetActive(false);
        TopLevelObjects.instance.mainCanvas.gameObject
            .SetActive(false);
        TopLevelObjects.instance.editorCanvas.gameObject
            .SetActive(false);
    }

    private void ProgressCallback(string currentlyLoadingFile)
    {
        progressLine2.text = Paths
            .HidePlatformInternalPath(currentlyLoadingFile);
    }

    private void CompleteCallback(Status status)
    {
        loadStatus = status;
    }
    #endregion

    #region MoonSharp debug server
    private MoonSharpVsCodeDebugServer debugServer;

    private void StartMoonSharpDebugServer()
    {
        // MoonSharp recommended port.
        int port = 41912;

        // Is this port available?
        bool available = true;
        TcpListener tcpListener = new TcpListener(
            System.Net.IPAddress.Loopback, port);
        try
        {
            tcpListener.Start();
        }
        catch (SocketException)
        {
            Debug.LogWarning($"Port {port} unavailable for MoonSharp debug server. Looking for another port; you will need to update .vscode/launch.json in VS Code to connect.");
            available = false;
        }
        finally { tcpListener.Stop(); }

        if (!available)
        {
            // Find an available port.
            tcpListener = new TcpListener(
                System.Net.IPAddress.Loopback, 0);
            tcpListener.Start();
            port = ((System.Net.IPEndPoint)tcpListener.LocalEndpoint)
                .Port;
            tcpListener.Stop();
        }
        debugServer = new MoonSharpVsCodeDebugServer(port);
        debugServer.Start();
        debugServer.AttachToScript(ThemeApi.ScriptSession.session,
            "TECHMANIA Theme");
        Debug.Log($"Started MoonSharp debug server at port {port}.");
    }

    private void OnDisable()
    {
        debugServer?.Dispose();
    }
    #endregion

    private IEnumerator ShowRevertDefaultThemePrompt()
    {
        if (Options.instance.theme == Options.kDefaultTheme)
        {
            // Hide prompt.
            revertButtonContainer.SetActive(false);
            themeDecided = true;
            yield break;
        }

        revertButtonContainer.SetActive(true);
        for (int i = 5; i >= 1; i--)
        {
            revertMessage.text = L10n.GetStringAndFormat(
                "boot_screen_revert_default_theme_label", i);
            yield return new WaitForSeconds(1f);
        }
        revertButtonContainer.SetActive(false);
        themeDecided = true;
    }

    public void OnRevertButtonClick()
    {
        StopCoroutine(revertPromptCoroutine);
        Options.instance.theme = Options.kDefaultTheme;
        Options.instance.SaveToFile(Paths.GetOptionsFilePath());
        revertButtonContainer.SetActive(false);
        themeDecided = true;
    }

    private void QuitGame()
    {
        ThemeApi.Techmania.Quit();
    }
}

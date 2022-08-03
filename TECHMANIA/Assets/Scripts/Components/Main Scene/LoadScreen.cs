using System.Collections;
using System.Collections.Generic;
using TMPro;
using UnityEngine;
using UnityEngine.UIElements;

public class LoadScreen : MonoBehaviour
{
    public TextMeshProUGUI progressLine1;
    public TextMeshProUGUI progressLine2;
    public GameObject revertButtonContainer;
    public TextMeshProUGUI revertMessage;
    public MessageDialog messageDialog;

    public UIDocument uiDocument;

    private bool themeDecided;
    private Coroutine revertPromptCoroutine;

    public void StartLoading()
    {
        themeDecided = false;
        revertPromptCoroutine = StartCoroutine(
            ShowRevertDefaultThemePrompt());

        StartCoroutine(LoadSequence());
    }

    private IEnumerator LoadSequence()
    {
        bool loaded = false;
        GlobalResourceLoader.ProgressCallback progressCallback =
            (string currentlyLoadingFile) =>
            {
                progressLine2.text = Paths
                    .HidePlatformInternalPath(currentlyLoadingFile);
            };
        GlobalResourceLoader.CompleteCallback completeCallback =
            (status) =>
            {
                if (!status.Ok())
                {
                    messageDialog.Show(status.errorMessage);
                }
                loaded = true;
            };

        // Step 1: load skins.
        progressLine1.text = L10n.GetStringAndFormat(
            "resource_loader_loading_skins", 1, 3);
        GlobalResourceLoader.GetInstance().LoadAllSkins(
            progressCallback, completeCallback);
        yield return new WaitUntil(() => loaded);
        yield return new WaitUntil(() =>
            !messageDialog.gameObject.activeSelf);

        // Step 2: load track list.
        progressLine1.text = L10n.GetStringAndFormat(
            "resource_loader_loading_track_list", 2, 3);
        loaded = false;
        GlobalResourceLoader.GetInstance().LoadTrackList(
            progressCallback, completeCallback);
        yield return new WaitUntil(() => loaded);
        yield return new WaitUntil(() =>
            !messageDialog.gameObject.activeSelf);
        yield return new WaitUntil(() => themeDecided);

        // Step 3: load theme.
        GlobalResourceLoader.CompleteCallback themeCompleteCallback =
            (status) =>
            {
                loaded = true;
                if (status.Ok()) return;

                string errorMessage = L10n.GetStringAndFormat(
                    "resource_loader_theme_failed_to_load",
                    Options.instance.theme);
                if (Options.instance.theme != Options.kDefaultTheme)
                {
                    errorMessage += "\n" + L10n.GetString(
                        "resource_loader_revert_default_theme");
                }

                Options.instance.theme = Options.kDefaultTheme;
                Options.instance.SaveToFile(
                    Paths.GetOptionsFilePath());

                messageDialog.Show(errorMessage, () =>
                {
                    QuitGame();
                });
            };
        progressLine1.text = L10n.GetStringAndFormat(
            "resource_loader_loading_theme", 3, 3);
        loaded = false;
        GlobalResourceLoader.GetInstance().LoadTheme(
            progressCallback, themeCompleteCallback);
        yield return new WaitUntil(() => loaded);
        yield return new WaitUntil(() =>
            !messageDialog.gameObject.activeSelf);

        // Display UIDocument.
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

        uiDocument.visualTreeAsset = GlobalResource
            .themeContent[mainTreePath] as VisualTreeAsset;
        GetComponentInParent<Canvas>().gameObject.SetActive(false);
        UnityEngine.EventSystems.EventSystem.current.gameObject
            .SetActive(false);

        ThemeApi.ScriptSession.Prepare();
        try
        {
            ThemeApi.ScriptSession.Execute(mainScript.text);
        }
        catch (ThemeApi.ApiNotSupportedException)
        {
            messageDialog.Show($"{L10n.GetString("theme_error_api_not_supported")}\n\n{L10n.GetString("theme_error_instruction")}", () => QuitGame());
        }
    }

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
                "load_screen_revert_default_theme_label", i);
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

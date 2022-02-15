using System.Collections;
using System.Collections.Generic;
using TMPro;
using UnityEngine;

public class LoadScreen : MonoBehaviour
{
    public TextMeshProUGUI progressLine1;
    public TextMeshProUGUI progressLine2;
    public GameObject revertButtonContainer;
    public TextMeshProUGUI revertMessage;
    public MessageDialog messageDialog;

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
                if (!status.ok)
                {
                    messageDialog.Show(status.errorMessage);
                }
                loaded = true;
            };

        // Step 1: load skins.
        progressLine1.text = Locale.GetStringAndFormat(
            "resource_loader_loading_skins", 1, 3);
        GetComponent<GlobalResourceLoader>().LoadAllSkins(
            progressCallback, completeCallback);
        yield return new WaitUntil(() => loaded);
        yield return new WaitUntil(() =>
            !messageDialog.gameObject.activeSelf);

        // Step 2: load track list.
        progressLine1.text = Locale.GetStringAndFormat(
            "resource_loader_loading_track_list", 2, 3);
        loaded = false;
        GetComponent<GlobalResourceLoader>().LoadTrackList(
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
                if (status.ok) return;

                messageDialog.Show(status.errorMessage, () =>
                {
                    Options.instance.theme = Options.kDefaultTheme;
                    Options.instance.SaveToFile(
                        Paths.GetOptionsFilePath());
                    UnityEngine.SceneManagement.SceneManager
                        .LoadScene("Main");
                });
            };
        progressLine1.text = Locale.GetStringAndFormat(
            "resource_loader_loading_theme", 3, 3);
        loaded = false;
        GetComponent<GlobalResourceLoader>().LoadTheme(
            progressCallback, themeCompleteCallback);
        yield return new WaitUntil(() => loaded);
        yield return new WaitUntil(() =>
            !messageDialog.gameObject.activeSelf);
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
            revertMessage.text = Locale.GetStringAndFormat(
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
}

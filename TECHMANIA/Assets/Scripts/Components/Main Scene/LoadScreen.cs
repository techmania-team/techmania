using System.Collections;
using System.Collections.Generic;
using TMPro;
using UnityEngine;

public class LoadScreen : MonoBehaviour
{
    public TextMeshProUGUI progressText;
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
        // Step 1: load skins.
        string progressTextLine1 = Locale.GetStringAndFormat(
            "resource_loader_loading_skins", 1, 3);
        bool skinsLoaded = false;
        GlobalResourceLoader.ProgressCallback progressCallback =
            (string currentlyLoadingFile) =>
            {
                string progressTextLine2 = Paths
                    .HidePlatformInternalPath(currentlyLoadingFile);
                progressText.text = $"{progressTextLine1}\n{progressTextLine2}";
            };
        GlobalResourceLoader.CompleteCallback completeCallback =
            (bool success, string errorMessage) =>
            {
                if (!success)
                {
                    messageDialog.Show(errorMessage);
                }
                skinsLoaded = true;
            };
        GetComponent<GlobalResourceLoader>().LoadAllSkins(
            progressCallback, completeCallback);
        yield return new WaitUntil(() => skinsLoaded);
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

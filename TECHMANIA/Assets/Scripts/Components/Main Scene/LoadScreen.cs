using System.Collections;
using System.Collections.Generic;
using TMPro;
using UnityEngine;

public class LoadScreen : MonoBehaviour
{
    public TextMeshProUGUI progressText;
    public GameObject revertButtonContainer;
    public TextMeshProUGUI revertMessage;

    private bool themeDecided;
    private Coroutine revertPromptCoroutine;

    public void StartLoading()
    {
        themeDecided = false;
        revertPromptCoroutine = StartCoroutine(
            ShowRevertDefaultThemePrompt());
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

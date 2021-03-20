using System.Collections;
using System.Collections.Generic;
using TMPro;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

public class WelcomeMat : MonoBehaviour
{
    public CanvasGroup instructionTextCanvasGroup;
    public TextMeshProUGUI loadingText;
    public GlobalResourceLoader globalResourceLoader;
    public GameObject mainMenuButtons;
    public GameObject selectTrackPanel;
    public Selectable firstSelectable;
    public MessageDialog messageDialog;

    public static bool skipToTrackSelect;

    private bool receivedInput;
    private bool handledResourceLoaderTerminalState;

    static WelcomeMat()
    {
        skipToTrackSelect = false;
    }

    // Start is called before the first frame update
    void Start()
    {
        if (skipToTrackSelect)
        {
            skipToTrackSelect = false;

            // Immediately show menu and go to select track panel.
            mainMenuButtons.SetActive(true);
            gameObject.SetActive(false);
            GetComponentInParent<Panel>().defaultSelectable = firstSelectable;
            GetComponentInParent<Panel>().gameObject.SetActive(false);

            selectTrackPanel.SetActive(true);
        }
        else
        {
            receivedInput = false;
            loadingText.gameObject.SetActive(true);
            handledResourceLoaderTerminalState = false;
            StartCoroutine(SlowBlink());
        }
    }

    // Update is called once per frame
    void Update()
    {
        loadingText.text = globalResourceLoader.statusText;

        if (!handledResourceLoaderTerminalState &&
            globalResourceLoader.state != 
                GlobalResourceLoader.State.Loading)
        {
            handledResourceLoaderTerminalState = true;
            loadingText.gameObject.SetActive(false);
            if (globalResourceLoader.state ==
                GlobalResourceLoader.State.Error)
            {
                messageDialog.Show(globalResourceLoader.error);
            }
        }

        if (!receivedInput && !messageDialog.gameObject.activeSelf &&
            (Input.anyKeyDown || Input.touchCount > 0))
        {
            receivedInput = true;
            StopAllCoroutines();
            StartCoroutine(FastBlinkThenShowMenu());
            MenuSfx.instance.PlayGameStartSound();
        }
    }

    private IEnumerator SlowBlink()
    {
        float period = 1.5f;
        while (true)
        {
            instructionTextCanvasGroup.alpha = 0f;
            for (float t = 0f; t < period; t += Time.deltaTime)
            {
                float progress = t / period;
                instructionTextCanvasGroup.alpha =
                    Mathf.SmoothStep(0f, 1f, progress);
                yield return null;
            }
            instructionTextCanvasGroup.alpha = 1f;
            for (float t = 0f; t < period; t += Time.deltaTime)
            {
                float progress = t / period;
                instructionTextCanvasGroup.alpha =
                    Mathf.SmoothStep(1f, 0f, progress);
                yield return null;
            }
        }
    }

    private IEnumerator FastBlinkThenShowMenu()
    {
        float period = 0.2f;
        for (int i = 0; i < 3; i++)
        {
            instructionTextCanvasGroup.alpha = 0f;
            for (float t = 0f; t < period; t += Time.deltaTime)
            {
                float progress = t / period;
                instructionTextCanvasGroup.alpha =
                    Mathf.SmoothStep(0f, 1f, progress);
                yield return null;
            }
            instructionTextCanvasGroup.alpha = 1f;
            for (float t = 0f; t < period; t += Time.deltaTime)
            {
                float progress = t / period;
                instructionTextCanvasGroup.alpha =
                    Mathf.SmoothStep(1f, 0f, progress);
                yield return null;
            }
        }
        yield return new WaitForSeconds(0.2f);

        while (globalResourceLoader.state == 
            GlobalResourceLoader.State.Loading)
        {
            yield return null;
        }

        mainMenuButtons.SetActive(true);
        EventSystem.current.SetSelectedGameObject(firstSelectable.gameObject);
        gameObject.SetActive(false);
    }
}

using System.Collections;
using System.Collections.Generic;
using TMPro;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

public class WelcomeMat : MonoBehaviour
{
    public CanvasGroup instructionTextCanvasGroup;
    public MainMenuPanel mainMenuPanel;

    private bool receivedInput;

    // Start is called before the first frame update
    void Start()
    {
        receivedInput = false;
        StartCoroutine(SlowBlink());
    }

    // Update is called once per frame
    void Update()
    {
        if (!receivedInput &&
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

        mainMenuPanel.ShowLoadingText();
    }
}

using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

public class Curtain : MonoBehaviour
{
    public AudioSource sfxSource;

    private Image image;
    private bool transitioning;

    // Start is called before the first frame update
    void Start()
    {
        image = GetComponent<Image>();
        transitioning = false;
        StartCoroutine(InternalOpenCurtain());
    }

    // Update is called once per frame
    void Update()
    {
        
    }

    public static void DrawCurtainThenGoToScene(string name)
    {
        Curtain c = FindObjectOfType<Curtain>();
        if (c.transitioning) return;
        c.transitioning = true;
        c.StopAllCoroutines();
        c.StartCoroutine(c.InternalDrawCurtainThenGoToScene(name));
    }

    public static void DrawCurtainThenQuit()
    {
        Curtain c = FindObjectOfType<Curtain>();
        if (c.transitioning) return;
        c.transitioning = true;
        c.StopAllCoroutines();
        c.StartCoroutine(c.InternalDrawCurtainThenQuit());
    }

    private IEnumerator InternalOpenCurtain()
    {
        image.raycastTarget = true;

        const float length = 0.5f;
        for (float t = 0f; t < length; t += Time.deltaTime)
        {
            float progress = t / length;
            image.color = new Color(0f, 0f, 0f, 1f - progress);
            yield return null;
        }

        image.raycastTarget = false;
    }

    private IEnumerator InternalDrawCurtainThenGoToScene(string name)
    {
        image.raycastTarget = true;

        const float length = 0.5f;
        for (float t = 0f; t < length; t += Time.deltaTime)
        {
            float progress = t / length;
            image.color = new Color(0f, 0f, 0f, progress);
            yield return null;
        }
        yield return new WaitUntil(() =>
        {
            return !sfxSource.isPlaying;
        });

        SceneManager.LoadScene(name);
    }

    private IEnumerator InternalDrawCurtainThenQuit()
    {
        image.raycastTarget = true;

        const float length = 0.5f;
        for (float t = 0f; t < length; t += Time.deltaTime)
        {
            float progress = t / length;
            image.color = new Color(0f, 0f, 0f, progress);
            yield return null;
        }
        yield return new WaitUntil(() =>
        {
            return !sfxSource.isPlaying;
        });

#if UNITY_EDITOR
        UnityEditor.EditorApplication.isPlaying = false;
#else
        Application.Quit();
#endif
    }
}

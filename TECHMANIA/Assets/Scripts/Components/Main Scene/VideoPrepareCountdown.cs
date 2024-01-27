using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class VideoPrepareCountdown : MonoBehaviour
{
    private Coroutine coroutine;
    private System.Action callback;

    public void StartCountdown(System.Action callback)
    {
        this.callback = callback;
        coroutine = StartCoroutine(Countdown());
    }

    public void StopCountdown()
    {
        StopCoroutine(coroutine);
    }

    private IEnumerator Countdown()
    {
        yield return new WaitForSeconds(10);
        callback();
    }
}

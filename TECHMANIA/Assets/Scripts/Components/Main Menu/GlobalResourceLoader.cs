using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Events;

public class GlobalResourceLoader : MonoBehaviour
{
    public enum State
    {
        Loading,
        Complete,
        Error
    }
    public State state { get; private set; }
    public string error { get; private set; }
    public string statusText { get; private set; }

    public void StartLoading()
    {
        StartCoroutine(Load());
    }

    private IEnumerator Load()
    {
        state = State.Loading;
        error = null;
        statusText = "";

        // TODO: load NoteSkin from disk
        // TODO: load each sprite sheet
        yield return null;

        state = State.Complete;
    }
}

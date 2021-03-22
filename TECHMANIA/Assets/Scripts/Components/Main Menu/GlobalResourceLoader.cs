using System;
using System.Collections;
using System.Collections.Generic;
using System.IO;
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

        string noteSkinFolder = Paths.GetNoteSkinFolder(
            Options.instance.noteSkin);
        string noteSkinFilename = Path.Combine(
            noteSkinFolder, Paths.kSkinFilename);
        try
        {
            GlobalResource.noteSkin = NoteSkin.LoadFromFile(
                noteSkinFilename) as NoteSkin;
        }
        catch (Exception ex)
        {
            state = State.Error;
            error = $"An error occurred when loading note skin:\n\n{ex.Message}";
            yield break;
        }

        List<SpriteSheet> spriteSheets = GlobalResource.noteSkin
            .GetReferenceToAllSpriteSheets();
        for (int i = 0; i < spriteSheets.Count; i++)
        {
            statusText = $"Loading note skin... ({i + 1}/{spriteSheets.Count})";

            string filename = Path.Combine(noteSkinFolder,
                spriteSheets[i].filename);
            bool loaded = false;
            ResourceLoader.LoadImage(filename,
                (texture, error) =>
                {
                    loaded = true;
                    if (error != null)
                    {
                        state = State.Error;
                        this.error = error;
                    }
                    else
                    {
                        spriteSheets[i].texture = texture;
                    }
                });
            yield return new WaitUntil(() => loaded);

            if (state == State.Error) yield break;
            spriteSheets[i].GenerateSprites();
        }

        yield return null;
        state = State.Complete;
    }
}

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
        StartCoroutine(LoadResourcesOnStartUp());
    }

    private IEnumerator LoadResourcesOnStartUp()
    {
        state = State.Loading;
        error = null;
        statusText = "";

        if (GlobalResource.loaded)
        {
            state = State.Complete;
            yield break;
        }

        UnityAction<string> progressCallback = (string progress) =>
        {
            statusText = progress;
        };
        bool oneSkinLoaded = false;
        UnityAction<string> completeCallback = (string errorMessage) =>
        {
            oneSkinLoaded = true;
            if (errorMessage != null)
            {
                state = State.Error;
                error = errorMessage;
            }
        };

        oneSkinLoaded = false;
        LoadNoteSkin(progressCallback, completeCallback);
        yield return new WaitUntil(() => oneSkinLoaded);
        if (state == State.Error) yield break;

        oneSkinLoaded = false;
        LoadVfxSkin(progressCallback, completeCallback);
        yield return new WaitUntil(() => oneSkinLoaded);
        if (state == State.Error) yield break;

        oneSkinLoaded = false;
        LoadComboSkin(progressCallback, completeCallback);
        yield return new WaitUntil(() => oneSkinLoaded);
        if (state == State.Error) yield break;

        yield return null;
        GlobalResource.loaded = true;
        state = State.Complete;
    }

    // completeCallback's argument is error message; null if no error.
    public void LoadNoteSkin(UnityAction<string> progressCallback,
        UnityAction<string> completeCallback)
    {
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
            completeCallback?.Invoke($"An error occurred when loading note skin:\n\n{ex.Message}");
            return;
        }

        List<SpriteSheet> spriteSheets = GlobalResource.noteSkin
            .GetReferenceToAllSpriteSheets();
        StartCoroutine(LoadSkin(noteSkinFolder,
            spriteSheets,
            "Loading note skin...",
            progressCallback,
            completeCallback));
    }

    // completeCallback's argument is error message; null if no error.
    public void LoadVfxSkin(UnityAction<string> progressCallback,
        UnityAction<string> completeCallback)
    {
        string vfxSkinFolder = Paths.GetVfxSkinFolder(
            Options.instance.vfxSkin);
        string vfxSkinFilename = Path.Combine(
            vfxSkinFolder, Paths.kSkinFilename);
        try
        {
            GlobalResource.vfxSkin = VfxSkin.LoadFromFile(
                vfxSkinFilename) as VfxSkin;
        }
        catch (Exception ex)
        {
            completeCallback?.Invoke($"An error occurred when loading VFX skin:\n\n{ex.Message}");
            return;
        }

        List<SpriteSheet> spriteSheets = GlobalResource.vfxSkin
            .GetReferenceToAllSpriteSheets();
        StartCoroutine(LoadSkin(vfxSkinFolder,
            spriteSheets,
            "Loading VFX skin...",
            progressCallback,
            completeCallback));
    }

    // completeCallback's argument is error message; null if no error.
    public void LoadComboSkin(UnityAction<string> progressCallback,
        UnityAction<string> completeCallback)
    {
        string comboSkinFolder = Paths.GetComboSkinFolder(
            Options.instance.comboSkin);
        string comboSkinFilename = Path.Combine(
            comboSkinFolder, Paths.kSkinFilename);
        try
        {
            GlobalResource.comboSkin = ComboSkin.LoadFromFile(
                comboSkinFilename) as ComboSkin;
        }
        catch (Exception ex)
        {
            completeCallback?.Invoke($"An error occurred when loading combo skin:\n\n{ex.Message}");
            return;
        }

        List<SpriteSheet> spriteSheets = GlobalResource.comboSkin
            .GetReferenceToAllSpriteSheets();
        StartCoroutine(LoadSkin(comboSkinFolder,
            spriteSheets,
            "Loading combo skin...",
            progressCallback,
            completeCallback));
    }

    // completeCallback's argument is error message; null if no error.
    private IEnumerator LoadSkin(string skinFolder,
        List<SpriteSheet> spriteSheetReferences,
        string loadMessage,
        UnityAction<string> progressCallback,
        UnityAction<string> completeCallback)
    {
        for (int i = 0; i < spriteSheetReferences.Count; i++)
        {
            progressCallback.Invoke($"{loadMessage} ({i + 1}/{spriteSheetReferences.Count})");

            string filename = Path.Combine(skinFolder,
                spriteSheetReferences[i].filename);
            bool loaded = false;
            bool error = false;
            ResourceLoader.LoadImage(filename,
                (texture, errorMessage) =>
                {
                    loaded = true;
                    if (errorMessage != null)
                    {
                        completeCallback(errorMessage);
                        error = true;
                    }
                    else
                    {
                        spriteSheetReferences[i].texture = texture;
                    }
                });
            yield return new WaitUntil(() => loaded);

            if (error)
            {
                yield break;
            }
            spriteSheetReferences[i].GenerateSprites();
        }
        completeCallback.Invoke(null);
    }
}

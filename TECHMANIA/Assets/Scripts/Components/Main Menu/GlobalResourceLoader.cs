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
        string vfxSkinFolder = Paths.GetVfxSkinFolder(
            Options.instance.vfxSkin);
        string vfxSkinFilename = Path.Combine(vfxSkinFolder,
            Paths.kSkinFilename);
        try
        {
            GlobalResource.noteSkin = NoteSkin.LoadFromFile(
                noteSkinFilename) as NoteSkin;
            GlobalResource.vfxSkin = VfxSkin.LoadFromFile(
                vfxSkinFilename) as VfxSkin;
        }
        catch (Exception ex)
        {
            state = State.Error;
            error = $"An error occurred when loading skin:\n\n{ex.Message}";
            yield break;
        }

        List<SpriteSheet> spriteSheets = GlobalResource.noteSkin
            .GetReferenceToAllSpriteSheets();
        yield return StartCoroutine(LoadListOfSpriteSheets(
            spriteSheets, noteSkinFolder,
            "Loading note skin..."));

        spriteSheets = GlobalResource.vfxSkin
            .GetReferenceToAllSpriteSheets();
        yield return StartCoroutine(LoadListOfSpriteSheets(
            spriteSheets, vfxSkinFolder,
            "Loading VFX skin..."));

        yield return null;
        state = State.Complete;
    }

    private IEnumerator LoadListOfSpriteSheets(
        List<SpriteSheet> list, string folder, string loadMessage)
    {
        for (int i = 0; i < list.Count; i++)
        {
            statusText = $"{loadMessage} ({i + 1}/{list.Count})";

            string filename = Path.Combine(folder,
                list[i].filename);
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
                        list[i].texture = texture;
                    }
                });
            yield return new WaitUntil(() => loaded);

            if (state == State.Error) yield break;
            list[i].GenerateSprites();
        }
    }
}

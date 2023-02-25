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

    public IEnumerator LoadResources(bool reload, Action finishCallback = null)
    {
        if (reload)
        {
            GlobalResource.loaded = false;
        }

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
        string lastError = null;
        UnityAction<string> completeCallback = (string errorMessage) =>
        {
            oneSkinLoaded = true;
            if (errorMessage != null)
            {
                lastError = errorMessage;
            }
        };

        oneSkinLoaded = false;
        LoadNoteSkin(progressCallback, completeCallback);
        yield return new WaitUntil(() => oneSkinLoaded);

        oneSkinLoaded = false;
        LoadVfxSkin(progressCallback, completeCallback);
        yield return new WaitUntil(() => oneSkinLoaded);

        oneSkinLoaded = false;
        LoadComboSkin(progressCallback, completeCallback);
        yield return new WaitUntil(() => oneSkinLoaded);

        oneSkinLoaded = false;
        LoadGameUiSkin(progressCallback, completeCallback);
        yield return new WaitUntil(() => oneSkinLoaded);

        yield return null;
        if (lastError == null)
        {
            GlobalResource.loaded = true;
            state = State.Complete;
        }
        else
        {
            error = lastError;
            state = State.Error;
        }

        finishCallback?.Invoke();
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
            completeCallback?.Invoke(Locale.GetStringAndFormatIncludingPaths(
                "resource_loader_note_skin_error_format",
                ex.Message)); 
            return;
        }

        List<SpriteSheet> spriteSheets = GlobalResource.noteSkin
            .GetReferenceToAllSpriteSheets();
        StartCoroutine(LoadSkin(noteSkinFolder,
            spriteSheets,
            Locale.GetString("resource_loader_loading_note_skin"),
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
            completeCallback?.Invoke(Locale.GetStringAndFormatIncludingPaths(
                "resource_loader_vfx_skin_error_format",
                ex.Message));
            return;
        }

        List<SpriteSheet> spriteSheets = GlobalResource.vfxSkin
            .GetReferenceToAllSpriteSheets();
        StartCoroutine(LoadSkin(vfxSkinFolder,
            spriteSheets,
            Locale.GetString("resource_loader_loading_vfx_skin"),
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
            completeCallback?.Invoke(Locale.GetStringAndFormatIncludingPaths(
                "resource_loader_combo_skin_error_format",
                ex.Message));
            return;
        }

        UnityAction<string> localCallback = (string error) =>
        {
            if (error != null)
            {
                completeCallback(error);
                return;
            }
            // The game expects 10 digits in each set.
            foreach (List<SpriteSheet> list in
                GlobalResource.comboSkin.GetReferenceToDigitLists())
            {
                while (list.Count < 10)
                {
                    list.Add(SpriteSheet.MakeNewEmptySpriteSheet());
                }
            }
            completeCallback(null);
        };
        List<SpriteSheet> spriteSheets = GlobalResource.comboSkin
            .GetReferenceToAllSpriteSheets();
        StartCoroutine(LoadSkin(comboSkinFolder,
            spriteSheets,
            Locale.GetString("resource_loader_loading_combo_skin"),
            progressCallback,
            localCallback));
    }

    public void LoadGameUiSkin(UnityAction<string> progressCallback,
        UnityAction<string> completeCallback)
    {
        string gameUiSkinFolder = Paths.GetGameUiSkinFolder(
            Options.instance.gameUiSkin);
        string gameUiSkinFilename = Path.Combine(
            gameUiSkinFolder, Paths.kSkinFilename);
        try
        {
            GlobalResource.gameUiSkin = GameUISkin.LoadFromFile(
                gameUiSkinFilename) as GameUISkin;
        }
        catch (Exception ex)
        {
            completeCallback?.Invoke(Locale.GetStringAndFormatIncludingPaths(
                "resource_loader_game_ui_skin_error_format",
                ex.Message));
            return;
        }

        List<SpriteSheet> spriteSheets = GlobalResource.gameUiSkin
            .GetReferenceToAllSpriteSheets();
        StartCoroutine(LoadSkin(gameUiSkinFolder,
            spriteSheets,
            Locale.GetString("resource_loader_loading_game_ui_skin"),
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
        Options.TemporarilyDisableVSync();
        for (int i = 0; i < spriteSheetReferences.Count; i++)
        {
            progressCallback?.Invoke($"{loadMessage} ({i + 1}/{spriteSheetReferences.Count})");

            if (spriteSheetReferences[i] == null ||
                spriteSheetReferences[i].filename == null)
            {
                spriteSheetReferences[i].MakeEmpty();
                continue;
            }

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
                        completeCallback?.Invoke(errorMessage);
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
        completeCallback?.Invoke(null);
        Options.RestoreVSync();
    }
}

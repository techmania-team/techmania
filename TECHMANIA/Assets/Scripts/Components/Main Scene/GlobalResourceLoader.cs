using System;
using System.Collections;
using System.Collections.Generic;
using System.IO;
using UnityEngine;
using UnityEngine.Events;

// GlobalResource is not a MonoBehaviour but this has to be, due to
// coroutines.
public class GlobalResourceLoader : MonoBehaviour
{
    public delegate void ProgressCallback(
        string currentlyLoadingFile);
    public delegate void CompleteCallback(
        Status status);

    public void LoadAllSkins(
        ProgressCallback progressCallback,
        CompleteCallback completeCallback)
    {
        StartCoroutine(LoadAllSkinsCoroutine(
            progressCallback, completeCallback));
    }

    private IEnumerator LoadAllSkinsCoroutine(
        ProgressCallback progressCallback,
        CompleteCallback completeCallback)
    {
        bool oneSkinLoaded = false;
        Status lastError = Status.OKStatus();
        CompleteCallback localCompleteCallback = (status) =>
        {
            oneSkinLoaded = true;
            if (!status.ok)
            {
                lastError = status;
            }
        };

        oneSkinLoaded = false;
        LoadNoteSkin(progressCallback, localCompleteCallback);
        yield return new WaitUntil(() => oneSkinLoaded);

        oneSkinLoaded = false;
        LoadVfxSkin(progressCallback, localCompleteCallback);
        yield return new WaitUntil(() => oneSkinLoaded);

        oneSkinLoaded = false;
        LoadComboSkin(progressCallback, localCompleteCallback);
        yield return new WaitUntil(() => oneSkinLoaded);

        oneSkinLoaded = false;
        LoadGameUiSkin(progressCallback, localCompleteCallback);
        yield return new WaitUntil(() => oneSkinLoaded);

        completeCallback?.Invoke(lastError);
    }

    public void LoadNoteSkin(ProgressCallback progressCallback,
        CompleteCallback completeCallback)
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
            completeCallback?.Invoke(Status.Error(
                Locale.GetStringAndFormatIncludingPaths(
                "resource_loader_note_skin_error_format",
                ex.Message))); 
            return;
        }

        List<SpriteSheet> spriteSheets = GlobalResource.noteSkin
            .GetReferenceToAllSpriteSheets();
        StartCoroutine(LoadSkin(noteSkinFolder,
            spriteSheets,
            progressCallback,
            completeCallback));
    }

    public void LoadVfxSkin(ProgressCallback progressCallback,
        CompleteCallback completeCallback)
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
            completeCallback?.Invoke(Status.Error(
                Locale.GetStringAndFormatIncludingPaths(
                "resource_loader_vfx_skin_error_format",
                ex.Message)));
            return;
        }

        List<SpriteSheet> spriteSheets = GlobalResource.vfxSkin
            .GetReferenceToAllSpriteSheets();
        StartCoroutine(LoadSkin(vfxSkinFolder,
            spriteSheets,
            progressCallback,
            completeCallback));
    }

    public void LoadComboSkin(ProgressCallback progressCallback,
        CompleteCallback completeCallback)
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
            completeCallback?.Invoke(Status.Error(
                Locale.GetStringAndFormatIncludingPaths(
                "resource_loader_combo_skin_error_format",
                ex.Message)));
            return;
        }

        CompleteCallback localCallback = (status) =>
        {
            if (!status.ok)
            {
                completeCallback(status);
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
            completeCallback(Status.OKStatus());
        };
        List<SpriteSheet> spriteSheets = GlobalResource.comboSkin
            .GetReferenceToAllSpriteSheets();
        StartCoroutine(LoadSkin(comboSkinFolder,
            spriteSheets,
            progressCallback,
            localCallback));
    }

    public void LoadGameUiSkin(ProgressCallback progressCallback,
        CompleteCallback completeCallback)
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
            completeCallback?.Invoke(Status.Error(
                Locale.GetStringAndFormatIncludingPaths(
                "resource_loader_game_ui_skin_error_format",
                ex.Message)));
            return;
        }

        List<SpriteSheet> spriteSheets = GlobalResource.gameUiSkin
            .GetReferenceToAllSpriteSheets();
        StartCoroutine(LoadSkin(gameUiSkinFolder,
            spriteSheets,
            progressCallback,
            completeCallback));
    }

    private IEnumerator LoadSkin(string skinFolder,
        List<SpriteSheet> spriteSheetReferences,
        ProgressCallback progressCallback,
        CompleteCallback completeCallback)
    {
        Options.TemporarilyDisableVSync();
        for (int i = 0; i < spriteSheetReferences.Count; i++)
        {
            if (spriteSheetReferences[i] == null ||
                spriteSheetReferences[i].filename == null)
            {
                spriteSheetReferences[i].MakeEmpty();
                continue;
            }

            string filename = Path.Combine(skinFolder,
                spriteSheetReferences[i].filename);
            progressCallback?.Invoke(filename);
            bool loaded = false;
            bool error = false;
            ResourceLoader.LoadImage(filename,
                (status, texture) =>
                {
                    loaded = true;
                    error = !status.ok;
                    if (status.ok)
                    {
                        spriteSheetReferences[i].texture = texture;
                    }
                    else
                    {
                        completeCallback?.Invoke(status);
                    }
                });
            yield return new WaitUntil(() => loaded);

            if (error)
            {
                yield break;
            }
            spriteSheetReferences[i].GenerateSprites();
        }
        completeCallback?.Invoke(Status.OKStatus());
        Options.RestoreVSync();
    }
}

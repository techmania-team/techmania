using System;
using System.Collections;
using System.Collections.Generic;
using System.ComponentModel;
using System.IO;
using UnityEngine;
using UnityEngine.Events;

// GlobalResource is not a MonoBehaviour but this has to be, due to
// coroutines.
public class GlobalResourceLoader : MonoBehaviour
{
    private static GlobalResourceLoader instance;

    public static GlobalResourceLoader GetInstance()
    {
        if (instance == null)
        {
            instance = FindObjectOfType<GlobalResourceLoader>();
        }
        return instance;
    }

    public delegate void ProgressCallback(
        string currentlyLoadingFile);
    public delegate void CompleteCallback(
        Status status);

    #region Skins
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
            if (!status.Ok())
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
        string noteSkinFolder = Paths.GetSkinFolder(SkinType.Note,
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
            completeCallback?.Invoke(Status.FromException(
                ex, noteSkinFilename));
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
        string vfxSkinFolder = Paths.GetSkinFolder(SkinType.Vfx,
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
            completeCallback?.Invoke(Status.FromException(
                ex, vfxSkinFilename));
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
        string comboSkinFolder = Paths.GetSkinFolder(SkinType.Combo,
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
            completeCallback?.Invoke(Status.FromException(
                ex, comboSkinFilename));
            return;
        }

        CompleteCallback localCallback = (status) =>
        {
            if (!status.Ok())
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
        string gameUiSkinFolder = Paths.GetSkinFolder(SkinType.GameUI,
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
            completeCallback?.Invoke(Status.FromException(
                ex, gameUiSkinFilename));
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
                    error = !status.Ok();
                    if (status.Ok())
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
                Options.RestoreVSync();
                yield break;
            }
            spriteSheetReferences[i].GenerateSprites();
        }
        completeCallback?.Invoke(Status.OKStatus());
        Options.RestoreVSync();
    }
    #endregion

    #region Track list
    private BackgroundWorker trackListBuilder;
    private class BackgroundWorkerArgument
    {
        public bool upgradeVersion;
    }

    public void LoadTrackList(
        ProgressCallback progressCallback,
        CompleteCallback completeCallback)
    {
        StartCoroutine(LoadTrackListCoroutine(progressCallback,
            completeCallback, upgradeVersion: false));
    }

    public void UpdateTrackVersions(
        ProgressCallback progressCallback,
        CompleteCallback completeCallback)
    {
        StartCoroutine(LoadTrackListCoroutine(progressCallback,
            completeCallback, upgradeVersion: true));
    }

    private IEnumerator LoadTrackListCoroutine(
        ProgressCallback progressCallback,
        CompleteCallback completeCallback,
        bool upgradeVersion)
    {
        GlobalResource.trackSubfolderList = new Dictionary<
            string, List<GlobalResource.TrackSubfolder>>();
        GlobalResource.trackList = new Dictionary<
            string, List<GlobalResource.TrackInFolder>>();
        GlobalResource.trackWithErrorList = new Dictionary<
            string, List<GlobalResource.TrackWithError>>();
        GlobalResource.anyOutdatedTrack = false;

        trackListBuilder = new BackgroundWorker();
        trackListBuilder.WorkerReportsProgress = true;
        Status builderStatus = null;
        trackListBuilder.DoWork += TrackListBuilderDoWork;
        trackListBuilder.ProgressChanged +=
            (object _, ProgressChangedEventArgs userState) =>
            {
                string currentlyLoadingFile = userState.UserState
                    as string;
                progressCallback?.Invoke(currentlyLoadingFile);
            };
        trackListBuilder.RunWorkerCompleted +=
            (object _, RunWorkerCompletedEventArgs userState) =>
            {
                if (userState.Error == null)
                {
                    builderStatus = Status.OKStatus();
                    return;
                }
                builderStatus = Status.FromException(userState.Error);
            };

        trackListBuilder.RunWorkerAsync(
            new BackgroundWorkerArgument()
            {
                upgradeVersion = upgradeVersion
            });
        do
        {
            yield return null;
        } while (builderStatus == null);

        completeCallback?.Invoke(builderStatus);
    }

    public static void ClearCachedTrackList()
    {
        GlobalResource.trackSubfolderList.Clear();
        GlobalResource.trackList.Clear();
        GlobalResource.trackWithErrorList.Clear();
        GlobalResource.anyOutdatedTrack = false;
    }

    private void TrackListBuilderDoWork(object sender,
        DoWorkEventArgs e)
    {
        BackgroundWorker worker = sender as BackgroundWorker;
        BuildTrackList(worker, Paths.GetTrackRootFolder(),
            (e.Argument as BackgroundWorkerArgument).upgradeVersion);
        BuildStreamingTrackList(worker);
    }

    private void BuildTrackList(BackgroundWorker worker,
        string folder, bool upgradeVersion)
    {
        GlobalResource.trackSubfolderList.Add(folder,
            new List<GlobalResource.TrackSubfolder>());
        GlobalResource.trackList.Add(folder,
            new List<GlobalResource.TrackInFolder>());
        GlobalResource.trackWithErrorList.Add(folder,
            new List<GlobalResource.TrackWithError>());

        foreach (string file in Directory.EnumerateFiles(
            folder, "*.zip"))
        {
            // Attempt to extract this archive.
            worker.ReportProgress(0,
                Paths.HidePlatformInternalPath(file));
            try
            {
                ExtractZipFile(file);
            }
            catch (Exception ex)
            {
                // Log error and move on.
                Debug.LogError(ex.ToString());
            }
        }

        foreach (string dir in Directory.EnumerateDirectories(
            folder))
        {
            worker.ReportProgress(0,
                Paths.HidePlatformInternalPath(dir));
            DateTime modifiedTime = new DirectoryInfo(dir).LastWriteTime;

            // Is there a track?
            string possibleTrackFile = Path.Combine(
                dir, Paths.kTrackFilename);
            if (!File.Exists(possibleTrackFile))
            {
                // Treat as a subfolder.
                GlobalResource.TrackSubfolder subfolder =
                    new GlobalResource.TrackSubfolder()
                {
                    name = Path.GetFileName(dir),
                    modifiedTime = modifiedTime,
                    fullPath = dir
                };

                // Look for eyecatch, if any.
                string pngEyecatch = Path.Combine(dir,
                    Paths.kSubfolderEyecatchPngFilename);
                if (File.Exists(pngEyecatch))
                {
                    subfolder.eyecatchFullPath = pngEyecatch;
                }
                string jpgEyecatch = Path.Combine(dir,
                    Paths.kSubfolderEyecatchJpgFilename);
                if (File.Exists(jpgEyecatch))
                {
                    subfolder.eyecatchFullPath = jpgEyecatch;
                }

                // Record as a subfolder.
                if (folder.Equals(
                    Paths.GetTrackRootFolder(streamingAssets: true)))
                {
                    GlobalResource.trackSubfolderList[
                        Paths.GetTrackRootFolder()]
                        .Add(subfolder);
                }
                else
                {
                    GlobalResource.trackSubfolderList[
                        folder].Add(subfolder);
                }

                // Build recursively.
                BuildTrackList(worker, dir, upgradeVersion);

                continue;
            }

            // Attempt to load track.
            Track track = null;
            bool upgradedWhenLoading;
            try
            {
                track = Track.LoadFromFile(possibleTrackFile,
                    out upgradedWhenLoading) as Track;
            }
            catch (Exception ex)
            {
                GlobalResource.trackWithErrorList[folder].Add(
                    new GlobalResource.TrackWithError()
                    {
                        typeEnum = GlobalResource.TrackWithError.Type.Load,
                        status = Status.FromException(
                            ex, possibleTrackFile)
                    });
                continue;
            }
            if (upgradedWhenLoading)
            {
                // If upgrading, write the track back to disk.
                if (upgradeVersion)
                {
                    Debug.Log(possibleTrackFile +
                        " is being upgraded.");
                    try
                    {
                        track.SaveToFile(possibleTrackFile);
                    }
                    catch (Exception ex)
                    {
                        GlobalResource.trackWithErrorList[folder]
                            .Add(new GlobalResource.TrackWithError()
                        {
                            typeEnum = GlobalResource.TrackWithError
                                .Type.Upgrade,
                            status = Status.FromException(
                                ex, possibleTrackFile)
                        });
                        continue;
                    }
                }
                else
                {
                    Debug.Log(possibleTrackFile + " is outdated.");
                    GlobalResource.anyOutdatedTrack = true;
                }
            }

            GlobalResource.trackList[folder].Add(
                new GlobalResource.TrackInFolder()
            {
                folder = dir,
                modifiedTime = modifiedTime,
                minimizedTrack = Track.Minimize(track)
            });
        }
    }

    private void BuildStreamingTrackList(BackgroundWorker worker)
    {
        // We can't enumerate directories in streaming assets,
        // so instead we enumerate track.tech files and process each
        // directory above them.
        if (!BetterStreamingAssets.DirectoryExists(
                Paths.RelativePathInStreamingAssets(
                    Paths.GetTrackRootFolder(streamingAssets: true))))
        {
            return;
        }

        // Get all track.tech files.
        string[] relativeTrackFiles = BetterStreamingAssets.GetFiles(
            Paths.RelativePathInStreamingAssets(
                Paths.GetTrackRootFolder(streamingAssets: true)
            ),
            Paths.kTrackFilename,
            SearchOption.AllDirectories
        );

        // Get all directories above them.
        foreach (string relativeTrackFile in relativeTrackFiles)
        {
            // Get absolute track.tech file path.
            string absoluteTrackFile = Paths
                .AbsolutePathInStreamingAssets(relativeTrackFile);
            // Get relative directory path.
            string relativeTrackFolder = Path
                .GetDirectoryName(relativeTrackFile);
            // Get absolute directory path.
            string absoluteTrackFolder = Paths
                .AbsolutePathInStreamingAssets(relativeTrackFolder);

            // These two start as the folder above track folder.
            string processingRelativeFolder = Path
                .GetDirectoryName(relativeTrackFolder);
            string processingAbsoluteFolder = Paths
                .AbsolutePathInStreamingAssets(
                processingRelativeFolder);

            if (processingAbsoluteFolder == Paths.GetTrackRootFolder(
                streamingAssets: true))
            {
                processingAbsoluteFolder = Paths
                    .GetTrackRootFolder();
            }

            worker.ReportProgress(0, Paths.HidePlatformInternalPath(
                absoluteTrackFolder));

            Track t = null;
            try
            {
                t = TrackBase.LoadFromFile(absoluteTrackFile)
                    as Track;
                if (!GlobalResource.trackList.ContainsKey(
                    processingAbsoluteFolder))
                {
                    GlobalResource.trackList.Add(
                        processingAbsoluteFolder,
                        new List<GlobalResource.TrackInFolder>());
                }
                GlobalResource.trackList[processingAbsoluteFolder]
                    .Add(new GlobalResource.TrackInFolder()
                {
                    minimizedTrack = Track.Minimize(t),
                    modifiedTime = DateTime.UnixEpoch,
                    folder = absoluteTrackFolder
                });
            }
            catch (Exception ex)
            {
                if (!GlobalResource.trackWithErrorList.ContainsKey(
                    processingAbsoluteFolder))
                {
                    GlobalResource.trackWithErrorList.Add(
                        processingAbsoluteFolder,
                        new List<GlobalResource.TrackWithError>());
                }
                GlobalResource.trackWithErrorList[
                    processingAbsoluteFolder].Add(
                    new GlobalResource.TrackWithError()
                {
                    typeEnum = GlobalResource.TrackWithError.Type.Load,
                    status = Status.FromException(
                            ex, absoluteTrackFile)
                });
            }

            // Process folders upward from processingAbsoluteFolder.
            while (processingAbsoluteFolder != Paths
                .GetTrackRootFolder(streamingAssets: true))
            {
                string processingRelativeParentFolder = Path
                    .GetDirectoryName(processingRelativeFolder);
                string processingAbsoluteParentFolder = Paths
                    .AbsolutePathInStreamingAssets(
                    processingRelativeParentFolder);
                string dirKey = processingAbsoluteParentFolder;

                if (processingAbsoluteParentFolder == Paths
                    .GetTrackRootFolder(streamingAssets: true))
                {
                    dirKey = Paths.GetTrackRootFolder();
                }
                if (!GlobalResource.trackSubfolderList.ContainsKey(
                    dirKey))
                {
                    GlobalResource.trackSubfolderList.Add(dirKey,
                        new List<GlobalResource.TrackSubfolder>());
                }
                if (!GlobalResource.trackSubfolderList[dirKey]
                    .Exists(
                    (GlobalResource.TrackSubfolder s) =>
                    {
                        return s.fullPath == processingAbsoluteFolder;
                    }))
                {
                    GlobalResource.TrackSubfolder s = new 
                        GlobalResource.TrackSubfolder()
                    {
                        name = Path.GetFileName(
                            processingAbsoluteFolder),
                        modifiedTime = DateTime.UnixEpoch,
                        fullPath = processingAbsoluteFolder
                    };
                    string pngEyecatch = Path.Combine(
                        processingRelativeFolder,
                        Paths.kSubfolderEyecatchPngFilename);
                    if (BetterStreamingAssets.FileExists(
                        pngEyecatch))
                    {
                        s.eyecatchFullPath = Paths.
                            AbsolutePathInStreamingAssets(
                            pngEyecatch);
                    }
                    string jpgEyecatch = Path.Combine(
                        processingRelativeFolder,
                        Paths.kSubfolderEyecatchJpgFilename);
                    if (BetterStreamingAssets.FileExists(
                        jpgEyecatch))
                    {
                        s.eyecatchFullPath = Paths.
                            AbsolutePathInStreamingAssets(
                            jpgEyecatch);
                    }
                    GlobalResource.trackSubfolderList[dirKey].Add(s);
                }
                processingRelativeFolder = 
                    processingRelativeParentFolder;
                processingAbsoluteFolder = 
                    processingAbsoluteParentFolder;
            }
        }
    }

    private void ExtractZipFile(string zipFilename)
    {
        Debug.Log("Extracting: " + zipFilename);
        string zipLocation = Path.GetDirectoryName(zipFilename);

        using (FileStream fileStream = File.OpenRead(zipFilename))
        using (ICSharpCode.SharpZipLib.Zip.ZipFile zipFile = new
            ICSharpCode.SharpZipLib.Zip.ZipFile(fileStream))
        {
            byte[] buffer = new byte[4096];  // Recommended length

            // 1st pass: find track.tech, determine whether we need
            // to create a folder to extract into.
            bool foundTrackFile = false;
            foreach (ICSharpCode.SharpZipLib.Zip.ZipEntry entry in
                zipFile)
            {
                if (Path.GetFileName(entry.Name) != Paths.kTrackFilename)
                { 
                    continue;
                }
                foundTrackFile = true;

                if (!string.IsNullOrEmpty(Path.GetDirectoryName(
                    entry.Name)))
                {
                    // Track file is in a folder, no need to
                    // create a new one.
                    break;
                }

                // Track file is not in a folder, we need to
                // create a track folder to extract into.
                // In order to do that, extract the track.tech file
                // to memory, then deserialize.
                using var inputStream = zipFile.GetInputStream(entry);
                using MemoryStream outputStream = new MemoryStream();
                ICSharpCode.SharpZipLib.Core.StreamUtils.Copy(
                    inputStream, outputStream, buffer);
                outputStream.Position = 0;
                using StreamReader reader = new StreamReader(
                    outputStream);
                string trackFileContent = reader.ReadToEnd();
                Track track = TrackBase.Deserialize(
                    trackFileContent) as Track;
                zipLocation = Path.Combine(zipLocation,
                    ThemeApi.EditorInterface.TrackToDirectoryName(
                        track.trackMetadata.title, 
                        track.trackMetadata.artist));
                Directory.CreateDirectory(zipLocation);
                break;
            }
            if (!foundTrackFile)
            {
                throw new Exception(Paths.kTrackFilename + " is not found in the zip file. Unable to extract.");
            }

            // 2nd pass: actually extract files to disk.
            foreach (ICSharpCode.SharpZipLib.Zip.ZipEntry entry in
                zipFile)
            {
                if (entry.IsDirectory)
                {
                    continue;
                }

                string extractedFilename = Path.Combine(
                    zipLocation, entry.Name);
                Debug.Log($"Extracting {entry.Name} in {zipFilename} to: {extractedFilename}");

                Directory.CreateDirectory(Path.GetDirectoryName(
                    extractedFilename));
                using var inputStream = zipFile.GetInputStream(
                    entry);
                using FileStream outputStream = File.Create(
                    extractedFilename);
                ICSharpCode.SharpZipLib.Core.StreamUtils.Copy(
                    inputStream, outputStream, buffer);
            }
        }

        Debug.Log($"Extract successful. Deleting: {zipFilename}");
        File.Delete(zipFilename);
    }
    #endregion

    #region Theme
    public void LoadTheme(
        ProgressCallback progressCallback,
        CompleteCallback completeCallback)
    {
        string themePath;
        if (Application.isEditor &&
            Options.instance.theme == Options.kDefaultTheme)
        {
            // Load from project.
            themePath = Path.Combine(
                Paths.kAssetBundleFolder,
                Paths.kDefaultBundleName);
        }
        else
        {
            // Load from theme folder.
            themePath = Paths.GetThemeFilePath(
                Options.instance.theme);
        }
        if (!File.Exists(themePath))
        {
            completeCallback?.Invoke(
                Status.Error(Status.Code.NotFound));
        }
        else
        {
            StartCoroutine(LoadThemeCoroutine(themePath,
                progressCallback, completeCallback));
        }
    }

    private IEnumerator LoadThemeCoroutine(
        string path,
        ProgressCallback progressCallback,
        CompleteCallback completeCallback)
    {
        Options.TemporarilyDisableVSync();
        GlobalResource.themeContent =
            new Dictionary<string, UnityEngine.Object>();
        progressCallback?.Invoke(path);
        AssetBundleCreateRequest bundleRequest = 
            AssetBundle.LoadFromFileAsync(path);
        yield return bundleRequest;

        AssetBundle bundle = bundleRequest.assetBundle;
        Action reportFailedToLoadError = () =>
        {
            Options.RestoreVSync();
            completeCallback?.Invoke(Status.Error(
                Status.Code.OtherError));
        };
        if (bundle == null)
        {
            reportFailedToLoadError();
            yield break;
        }

        foreach (string name in bundle.GetAllAssetNames())
        {
            progressCallback?.Invoke(name);
            AssetBundleRequest request = bundle.LoadAssetAsync(name);
            yield return request;
            if (request.asset == null)
            {
                reportFailedToLoadError();
                yield break;
            }

            GlobalResource.themeContent.Add(name, request.asset);
        }
        Options.RestoreVSync();
        completeCallback?.Invoke(Status.OKStatus());
    }
    #endregion
}

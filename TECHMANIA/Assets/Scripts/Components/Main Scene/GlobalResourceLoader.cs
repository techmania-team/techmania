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

            // Convert animation from custom format to Unity format.
            GlobalResource.comboAnimationCurvesAndAttributes = new
                List<Tuple<AnimationCurve, string>>();
            foreach (SkinAnimationCurve customCurve in
                GlobalResource.comboSkin.animationCurves)
            {
                GlobalResource.comboAnimationCurvesAndAttributes.Add(
                    customCurve.ToUnityCurveAndAttribute());
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
        CompleteCallback completeCallback,
        bool upgradeVersion)
    {
        GlobalResource.trackSubfolderList = new Dictionary<
            string, List<GlobalResource.Subfolder>>();
        GlobalResource.trackList = new Dictionary<
            string, List<GlobalResource.TrackInFolder>>();
        GlobalResource.trackWithErrorList = new Dictionary<
            string, List<GlobalResource.ResourceWithError>>();
        GlobalResource.anyOutdatedTrack = false;

        trackListBuilder = new BackgroundWorker();
        trackListBuilder.DoWork += TrackListBuilderDoWork;
        StartCoroutine(BuildListCoroutine(
            trackListBuilder,
            progressCallback, completeCallback, upgradeVersion));
    }

    // Shared between tracks and setlists. Caller should set up
    // the worker's DoWork handler.
    private IEnumerator BuildListCoroutine(
        BackgroundWorker worker,
        ProgressCallback progressCallback,
        CompleteCallback completeCallback,
        bool upgradeVersion)
    {
        Status builderStatus = null;

        worker.WorkerReportsProgress = true;
        worker.ProgressChanged +=
            (object _, ProgressChangedEventArgs userState) =>
            {
                string currentlyLoadingFile = userState.UserState
                    as string;
                progressCallback?.Invoke(currentlyLoadingFile);
            };
        worker.RunWorkerCompleted +=
            (object _, RunWorkerCompletedEventArgs userState) =>
            {
                if (userState.Error == null)
                {
                    builderStatus = Status.OKStatus();
                    return;
                }
                builderStatus = Status.FromException(userState.Error);
            };

        worker.RunWorkerAsync(
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
            new List<GlobalResource.Subfolder>());
        GlobalResource.trackList.Add(folder,
            new List<GlobalResource.TrackInFolder>());
        GlobalResource.trackWithErrorList.Add(folder,
            new List<GlobalResource.ResourceWithError>());

        foreach (string file in Directory.EnumerateFiles(
            folder, "*.zip"))
        {
            // Attempt to extract this archive.
            worker.ReportProgress(0,
                Paths.HidePlatformInternalPath(file));
            try
            {
                ExtractZipFileAsTrack(file);
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
                GlobalResource.Subfolder subfolder =
                    new GlobalResource.Subfolder()
                {
                    name = Path.GetFileName(dir),
                    modifiedTime = modifiedTime,
                    fullPath = dir
                };

                // Look for eyecatch, if any.
                subfolder.FindEyecatch();

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
                    new GlobalResource.ResourceWithError()
                    {
                        typeEnum = GlobalResource.ResourceWithError
                            .Type.Load,
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
                            .Add(new GlobalResource.ResourceWithError()
                        {
                            typeEnum = GlobalResource.ResourceWithError
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
        // so instead we enumerate track.tech files and process them
        // as tracks; then, process each folder above the track folder
        // until the root, each as a track subfolder.

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

        // Get all directories above them, and process each one
        // as a track subfolder.
        foreach (string relativeTrackFile in relativeTrackFiles)
        {
            string absoluteTrackFile = Paths
                .AbsolutePathInStreamingAssets(relativeTrackFile);
            string relativeTrackFolder = Path
                .GetDirectoryName(relativeTrackFile);
            string absoluteTrackFolder = Paths
                .AbsolutePathInStreamingAssets(relativeTrackFolder);

            // These two start as the folder above track folder. They
            // will go up one level on each loop.
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
                        new List<GlobalResource.ResourceWithError>());
                }
                GlobalResource.trackWithErrorList[
                    processingAbsoluteFolder].Add(
                    new GlobalResource.ResourceWithError()
                {
                    typeEnum = GlobalResource.ResourceWithError.Type.Load,
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
                        new List<GlobalResource.Subfolder>());
                }
                if (!GlobalResource.trackSubfolderList[dirKey]
                    .Exists(
                    (GlobalResource.Subfolder s) =>
                    {
                        return s.fullPath == processingAbsoluteFolder;
                    }))
                {
                    GlobalResource.Subfolder s = new 
                        GlobalResource.Subfolder()
                    {
                        name = Path.GetFileName(
                            processingAbsoluteFolder),
                        modifiedTime = DateTime.UnixEpoch,
                        fullPath = processingAbsoluteFolder
                    };
                    s.FindStreamingEyecatch(processingRelativeFolder);
                    GlobalResource.trackSubfolderList[dirKey].Add(s);
                }
                processingRelativeFolder = 
                    processingRelativeParentFolder;
                processingAbsoluteFolder = 
                    processingAbsoluteParentFolder;
            }
        }
    }

    private void ExtractZipFileAsTrack(string zipFilename)
    {
        ExtractZipFile(zipFilename,
            expectedFilename: Paths.kTrackFilename,
            fileContentToFolderName: (string trackFileContent) =>
            {
                Track track = TrackBase.Deserialize(
                    trackFileContent) as Track;
                return ThemeApi.EditorInterface.TrackToDirectoryName(
                    track.trackMetadata.title,
                    track.trackMetadata.artist);
            });
    }

    // Shared by tracks and setlists.
    //
    // Extract the file at zipFilename to the folder it's at.
    // If a file named "expectedFilename" is found in the zip and it's
    // NOT in a folder, this method will call "fileContentToFolderName"
    // to produce a folder name, create it, and extract the zip into it.
    private void ExtractZipFile(string zipFilename,
        string expectedFilename,
        Func<string, string> fileContentToFolderName)
    {
        Debug.Log("Extracting: " + zipFilename);
        string zipLocation = Path.GetDirectoryName(zipFilename);

        using (FileStream fileStream = File.OpenRead(zipFilename))
        using (ICSharpCode.SharpZipLib.Zip.ZipFile zipFile = new
            ICSharpCode.SharpZipLib.Zip.ZipFile(fileStream))
        {
            byte[] buffer = new byte[4096];  // Recommended length

            // 1st pass: find the expected file, determine whether we need
            // to create a folder to extract into.
            bool foundExpectedFile = false;
            foreach (ICSharpCode.SharpZipLib.Zip.ZipEntry entry in
                zipFile)
            {
                if (Path.GetFileName(entry.Name) != expectedFilename)
                {
                    continue;
                }
                foundExpectedFile = true;

                if (!string.IsNullOrEmpty(Path.GetDirectoryName(
                    entry.Name)))
                {
                    // Expected file is in a folder, no need to
                    // create a new one.
                    break;
                }

                // Expected file is not in a folder, we need to
                // create a new folder to extract into.
                // In order to do that, extract the expected file
                // to memory, then call fileContentToFolderName.
                using var inputStream = zipFile.GetInputStream(entry);
                using MemoryStream outputStream = new MemoryStream();
                ICSharpCode.SharpZipLib.Core.StreamUtils.Copy(
                    inputStream, outputStream, buffer);
                outputStream.Position = 0;
                using StreamReader reader = new StreamReader(
                    outputStream);
                string expectedFileContent = reader.ReadToEnd();
                zipLocation = Path.Combine(zipLocation,
                    fileContentToFolderName(expectedFileContent));
                Directory.CreateDirectory(zipLocation);
                break;
            }
            if (!foundExpectedFile)
            {
                throw new Exception(expectedFilename + " is not found in the zip file. Unable to extract.");
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

    #region Setlist list
    private BackgroundWorker setlistListBuilder;

    public void LoadSetlistList(
        ProgressCallback progressCallback,
        CompleteCallback completeCallback,
        bool upgradeVersion)
    {
        GlobalResource.setlistSubfolderList = new Dictionary<
            string, List<GlobalResource.Subfolder>>();
        GlobalResource.setlistList = new Dictionary<
            string, List<GlobalResource.SetlistInFolder>>();
        GlobalResource.setlistWithErrorList = new Dictionary<
            string, List<GlobalResource.ResourceWithError>>();
        GlobalResource.anyOutdatedSetlist = false;

        setlistListBuilder = new BackgroundWorker();
        setlistListBuilder.DoWork += SetlistListBuilderDoWork;
        StartCoroutine(BuildListCoroutine(
            setlistListBuilder,
            progressCallback, completeCallback, upgradeVersion));
    }

    public static void ClearCachedSetlistList()
    {
        GlobalResource.setlistSubfolderList.Clear();
        GlobalResource.setlistList.Clear();
        GlobalResource.setlistWithErrorList.Clear();
        GlobalResource.anyOutdatedSetlist = false;
    }

    private void SetlistListBuilderDoWork(object sender,
        DoWorkEventArgs e)
    {
        BackgroundWorker worker = sender as BackgroundWorker;
        BuildSetlistList(worker, Paths.GetSetlistRootFolder(),
            (e.Argument as BackgroundWorkerArgument).upgradeVersion);
        BuildStreamingSetlistList(worker);
    }

    private void BuildSetlistList(BackgroundWorker worker,
        string folder, bool upgradeVersion)
    {
        GlobalResource.setlistSubfolderList.Add(folder,
            new List<GlobalResource.Subfolder>());
        GlobalResource.setlistList.Add(folder,
            new List<GlobalResource.SetlistInFolder>());
        GlobalResource.setlistWithErrorList.Add(folder,
            new List<GlobalResource.ResourceWithError>());

        foreach (string file in Directory.EnumerateFiles(
            folder, "*.zip"))
        {
            // Attempt to extract this archive.
            worker.ReportProgress(0,
                Paths.HidePlatformInternalPath(file));
            try
            {
                ExtractZipFileAsSetlist(file);
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

            // Is there a setlist?
            string possibleSetlistFile = Path.Combine(
                dir, Paths.kSetlistFilename);
            if (!File.Exists(possibleSetlistFile))
            {
                // Treat as a subfolder.
                GlobalResource.Subfolder subfolder =
                    new GlobalResource.Subfolder()
                    {
                        name = Path.GetFileName(dir),
                        modifiedTime = modifiedTime,
                        fullPath = dir
                    };

                // Look for eyecatch, if any.
                subfolder.FindEyecatch();

                // Record as a subfolder.
                if (folder.Equals(
                    Paths.GetSetlistRootFolder(streamingAssets: true)))
                {
                    GlobalResource.setlistSubfolderList[
                        Paths.GetSetlistRootFolder()]
                        .Add(subfolder);
                }
                else
                {
                    GlobalResource.setlistSubfolderList[
                        folder].Add(subfolder);
                }

                // Build recursively.
                BuildSetlistList(worker, dir, upgradeVersion);

                continue;
            }

            // Attempt to load setlist.
            Setlist setlist = null;
            bool upgradedWhenLoading;
            try
            {
                setlist = Setlist.LoadFromFile(possibleSetlistFile,
                    out upgradedWhenLoading) as Setlist;
            }
            catch (Exception ex)
            {
                GlobalResource.setlistWithErrorList[folder].Add(
                    new GlobalResource.ResourceWithError()
                    {
                        typeEnum = GlobalResource.ResourceWithError
                            .Type.Load,
                        status = Status.FromException(
                            ex, possibleSetlistFile)
                    });
                continue;
            }
            if (upgradedWhenLoading)
            {
                // If upgrading, write the setlist back to disk.
                if (upgradeVersion)
                {
                    Debug.Log(possibleSetlistFile +
                        " is being upgraded.");
                    try
                    {
                        setlist.SaveToFile(possibleSetlistFile);
                    }
                    catch (Exception ex)
                    {
                        GlobalResource.setlistWithErrorList[folder]
                            .Add(new GlobalResource.ResourceWithError()
                        {
                            typeEnum = GlobalResource.ResourceWithError
                                .Type.Upgrade,
                            status = Status.FromException(
                                ex, possibleSetlistFile)
                        });
                        continue;
                    }
                }
                else
                {
                    Debug.Log(possibleSetlistFile + " is outdated.");
                    GlobalResource.anyOutdatedSetlist = true;
                }
            }

            GlobalResource.setlistList[folder].Add(
                new GlobalResource.SetlistInFolder()
                {
                    folder = dir,
                    modifiedTime = modifiedTime,
                    setlist = setlist
                });
        }
    }

    private void BuildStreamingSetlistList(BackgroundWorker worker)
    {
        // We can't enumerate directories in streaming assets,
        // so instead we enumerate setlist.tech files and process them
        // as setlists; then, process each folder above the setlist folder
        // until the root, each as a setlist subfolder.

        if (!BetterStreamingAssets.DirectoryExists(
                Paths.RelativePathInStreamingAssets(
                    Paths.GetSetlistRootFolder(streamingAssets: true))))
        {
            return;
        }

        // Get all setlist.tech files.
        string[] relativeSetlistFiles = BetterStreamingAssets.GetFiles(
            Paths.RelativePathInStreamingAssets(
                Paths.GetSetlistRootFolder(streamingAssets: true)
            ),
            Paths.kSetlistFilename,
            SearchOption.AllDirectories
        );

        // Get all directories above them, and process each one
        // as a setlist subfolder.
        foreach (string relativeSetlistFile in relativeSetlistFiles)
        {
            string absoluteSetlistFile = Paths
                .AbsolutePathInStreamingAssets(relativeSetlistFile);
            string relativeSetlistFolder = Path
                .GetDirectoryName(relativeSetlistFile);
            string absoluteSetlistFolder = Paths
                .AbsolutePathInStreamingAssets(relativeSetlistFolder);

            // These two start as the folder above setlist folder. They
            // will go up one level on each loop.
            string processingRelativeFolder = Path
                .GetDirectoryName(relativeSetlistFolder);
            string processingAbsoluteFolder = Paths
                .AbsolutePathInStreamingAssets(
                processingRelativeFolder);

            if (processingAbsoluteFolder == Paths.GetSetlistRootFolder(
                streamingAssets: true))
            {
                processingAbsoluteFolder = Paths
                    .GetSetlistRootFolder();
            }

            worker.ReportProgress(0, Paths.HidePlatformInternalPath(
                absoluteSetlistFolder));

            Setlist t = null;
            try
            {
                t = SetlistBase.LoadFromFile(absoluteSetlistFile)
                    as Setlist;
                if (!GlobalResource.setlistList.ContainsKey(
                    processingAbsoluteFolder))
                {
                    GlobalResource.setlistList.Add(
                        processingAbsoluteFolder,
                        new List<GlobalResource.SetlistInFolder>());
                }
                GlobalResource.setlistList[processingAbsoluteFolder]
                    .Add(new GlobalResource.SetlistInFolder()
                    {
                        setlist = t,
                        modifiedTime = DateTime.UnixEpoch,
                        folder = absoluteSetlistFolder
                    });
            }
            catch (Exception ex)
            {
                if (!GlobalResource.setlistWithErrorList.ContainsKey(
                    processingAbsoluteFolder))
                {
                    GlobalResource.setlistWithErrorList.Add(
                        processingAbsoluteFolder,
                        new List<GlobalResource.ResourceWithError>());
                }
                GlobalResource.setlistWithErrorList[
                    processingAbsoluteFolder].Add(
                    new GlobalResource.ResourceWithError()
                    {
                        typeEnum = GlobalResource.ResourceWithError
                            .Type.Load,
                        status = Status.FromException(
                            ex, absoluteSetlistFile)
                    });
            }

            // Process folders upward from processingAbsoluteFolder.
            while (processingAbsoluteFolder != Paths
                .GetSetlistRootFolder(streamingAssets: true))
            {
                string processingRelativeParentFolder = Path
                    .GetDirectoryName(processingRelativeFolder);
                string processingAbsoluteParentFolder = Paths
                    .AbsolutePathInStreamingAssets(
                    processingRelativeParentFolder);
                string dirKey = processingAbsoluteParentFolder;

                if (processingAbsoluteParentFolder == Paths
                    .GetSetlistRootFolder(streamingAssets: true))
                {
                    dirKey = Paths.GetSetlistRootFolder();
                }
                if (!GlobalResource.setlistSubfolderList.ContainsKey(
                    dirKey))
                {
                    GlobalResource.setlistSubfolderList.Add(dirKey,
                        new List<GlobalResource.Subfolder>());
                }
                if (!GlobalResource.setlistSubfolderList[dirKey]
                    .Exists(
                    (GlobalResource.Subfolder s) =>
                    {
                        return s.fullPath == processingAbsoluteFolder;
                    }))
                {
                    GlobalResource.Subfolder s = new
                        GlobalResource.Subfolder()
                    {
                        name = Path.GetFileName(
                            processingAbsoluteFolder),
                        modifiedTime = DateTime.UnixEpoch,
                        fullPath = processingAbsoluteFolder
                    };
                    s.FindStreamingEyecatch(processingRelativeFolder);
                    GlobalResource.setlistSubfolderList[dirKey].Add(s);
                }
                processingRelativeFolder =
                    processingRelativeParentFolder;
                processingAbsoluteFolder =
                    processingAbsoluteParentFolder;
            }
        }
    }

    private void ExtractZipFileAsSetlist(string zipFilename)
    {
        ExtractZipFile(zipFilename,
            expectedFilename: Paths.kSetlistFilename,
            fileContentToFolderName: (string setlistFileContent) =>
            {
                Setlist setlist = SetlistBase.Deserialize(
                    setlistFileContent) as Setlist;
                return ThemeApi.EditorInterface.SetlistToDirectoryName(
                    setlist.setlistMetadata.title);
            });
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
        if (!UniversalIO.FileExists(themePath))
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
            new Dictionary<string, object>();
        progressCallback?.Invoke(path);
        AssetBundleCreateRequest bundleRequest = 
            AssetBundle.LoadFromFileAsync(path);
        yield return bundleRequest;

        AssetBundle bundle = bundleRequest.assetBundle;
        Action<string> reportFailedToLoadError = (string path) =>
        {
            Options.RestoreVSync();
            completeCallback?.Invoke(Status.Error(
                Status.Code.OtherError, null, path));
        };
        if (bundle == null)
        {
            // Sadly AssetBundleCreateRequest does not report error.
            Debug.LogError("AssetBundleCreateRequest returned empty bundle.");
            reportFailedToLoadError(path);
            yield break;
        }

        foreach (string name in bundle.GetAllAssetNames())
        {
            progressCallback?.Invoke(name);
            AssetBundleRequest request = bundle.LoadAssetAsync(name);
            yield return request;
            if (request.asset == null)
            {
                Debug.LogError($"Failed to load {name} from within the bundle.");
                reportFailedToLoadError(name);
                yield break;
            }

            // Special handling of AudioClips: convert them to
            // FmodSoundWrap.
            if (request.asset is AudioClip)
            {
                FmodSoundWrap sound = FmodManager
                    .CreateSoundFromAudioClip(
                        request.asset as AudioClip);
                GlobalResource.themeContent.Add(name, sound);
            }
            else
            {
                GlobalResource.themeContent.Add(name, request.asset);
            }
        }
        Options.RestoreVSync();
        completeCallback?.Invoke(Status.OKStatus());
    }
    #endregion
}

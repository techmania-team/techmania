using System;
using System.Collections;
using System.Collections.Generic;
using System.IO;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

public class TrackSetupPanel : MonoBehaviour
{
    public MessageDialog messageDialog;
    public ConfirmDialog confirmDialog;
    public Tabs tabs;

    #region Filename caching
    private List<string> audioFilesCache;
    private List<string> imageFilesCache;
    private List<string> videoFilesCache;

    private void RefreshFilenameCaches()
    {
        audioFilesCache = Paths.GetAllAudioFiles(
            EditorContext.trackFolder);
        imageFilesCache = Paths.GetAllImageFiles(
            EditorContext.trackFolder);
        videoFilesCache = Paths.GetAllVideoFiles(
            EditorContext.trackFolder);
    }

    private void RefreshFilenameCachesIfNull()
    {
        if (audioFilesCache == null ||
            imageFilesCache == null ||
            videoFilesCache == null)
        {
            RefreshFilenameCaches();
        }
    }
    #endregion

    #region Refreshing
    private void OnEnable()
    {
        Tabs.tabChanged += Refresh;
        EditorContext.UndoInvoked += Refresh;
        PatternRadioList.SelectedPatternChanged += SelectedPatternChanged;
        selectedPattern = null;
        Refresh();
    }

    private void OnDisable()
    {
        Tabs.tabChanged -= Refresh;
        EditorContext.UndoInvoked -= Refresh;
        PatternRadioList.SelectedPatternChanged -= SelectedPatternChanged;
    }

    private void OnApplicationFocus(bool focus)
    {
        if (focus)
        {
            Refresh();
        }
    }

    private void Refresh()
    {
        switch (tabs.CurrentTab)
        {
            case 0:
                RefreshResourcesTab();
                break;
            case 1:
                RefreshMetadataTab();
                break;
            case 2:
                RefreshPatternsTab();
                break;
        }
    }
    #endregion

    #region Resources tab
    [Header("Resources tab")]
    public TextMeshProUGUI audioFilesDisplay;
    public TextMeshProUGUI imageFilesDisplay;
    public TextMeshProUGUI videoFilesDisplay;

    public void OnImportButtonClick()
    {
        SFB.ExtensionFilter[] extensionFilters =
            new SFB.ExtensionFilter[2];
        extensionFilters[0] = new SFB.ExtensionFilter(
            "Supported files", new string[] {
                "wav",
                "ogg",
                "jpg",
                "png",
                "mp4",
                "wmv"
            });
        extensionFilters[1] = new SFB.ExtensionFilter(
            "All files", new string[] { "*" });
        string[] sources = SFB.StandaloneFileBrowser.OpenFilePanel(
            "Select resource to import", "",
            extensionFilters, multiselect: true);
        string trackFolder = EditorContext.trackFolder;

        List<Tuple<string, string>> pairs = new List<Tuple<string, string>>();
        List<string> filesToBeOverwritten = new List<string>();
        foreach (string source in sources)
        {
            FileInfo fileInfo = new FileInfo(source);
            if (fileInfo.DirectoryName == trackFolder) continue;
            string destination = Path.Combine(trackFolder, fileInfo.Name);

            if (File.Exists(destination))
            {
                filesToBeOverwritten.Add(fileInfo.Name);
            }
            pairs.Add(new Tuple<string, string>(source, destination));
        }

        if (filesToBeOverwritten.Count > 0)
        {
            string fileList = "";
            for (int i = 0; i < filesToBeOverwritten.Count; i++)
            {
                if (i == 10)
                {
                    fileList += $"... and {filesToBeOverwritten.Count - 10} more.\n";
                    break;
                }
                else
                {
                    fileList += filesToBeOverwritten[i] + "\n";
                }
            }
            confirmDialog.Show(
                $"The following files will be overwritten. Continue?\n\n{fileList}\nIf you choose to cancel, no file will be copied.",
                "overwrite", "cancel", () =>
                {
                    StartCopy(pairs);
                });
        }
        else
        {
            StartCopy(pairs);
        }
    }

    private void StartCopy(List<Tuple<string, string>> pairs)
    {
        foreach (Tuple<string, string> pair in pairs)
        {
            try
            {
                File.Copy(pair.Item1, pair.Item2, overwrite: true);
            }
            catch (Exception e)
            {
                messageDialog.Show(
                    $"An error occurred when copying {pair.Item1} to {pair.Item2}:\n\n"
                    + e.Message);
                break;
            }
        }
        
        RefreshResourcesTab();
    }

    private string CondenseFileList(List<string> fullPaths)
    {
        string str = "";
        foreach (string file in fullPaths)
        {
            str += new FileInfo(file).Name + "\n";
        }
        return str.TrimEnd('\n');
    }

    public void RefreshResourcesTab()
    {
        RefreshFilenameCaches();
        audioFilesDisplay.text = CondenseFileList(
            audioFilesCache);
        imageFilesDisplay.text = CondenseFileList(
            imageFilesCache);
        videoFilesDisplay.text = CondenseFileList(
            videoFilesCache);
    }
    #endregion

    #region Metadata tab
    [Header("Metadata tab")]
    public TMP_InputField trackTitle;
    public TMP_InputField artist;
    public TMP_InputField genre;
    public TMP_InputField additionalCredits;
    public EyecatchSelfLoader eyecatchPreview;
    public TMP_Dropdown eyecatchImage;
    public TMP_Dropdown previewTrack;
    public TMP_InputField startTime;
    public TMP_InputField endTime;
    public PreviewTrackPlayer previewTrackPlayer;

    public void RefreshMetadataTab()
    {
        TrackMetadata metadata = EditorContext.track.trackMetadata;
        RefreshFilenameCachesIfNull();

        trackTitle.SetTextWithoutNotify(metadata.title);
        artist.SetTextWithoutNotify(metadata.artist);
        genre.SetTextWithoutNotify(metadata.genre);
        additionalCredits.SetTextWithoutNotify(
            metadata.additionalCredits);

        UIUtils.MemoryToDropdown(eyecatchImage,
            metadata.eyecatchImage, imageFilesCache);
        UIUtils.MemoryToDropdown(previewTrack,
            metadata.previewTrack, audioFilesCache);
        startTime.SetTextWithoutNotify(metadata.previewStartTime.ToString());
        endTime.SetTextWithoutNotify(metadata.previewEndTime.ToString());

        foreach (TMP_InputField field in new List<TMP_InputField>()
        {
            trackTitle,
            artist,
            genre,
            additionalCredits,
            startTime,
            endTime
        })
        {
            field.GetComponent<MaterialTextField>().RefreshMiniLabel();
        }

        RefreshEyecatchPreview();
    }

    public void OnMetadataUpdated()
    {
        TrackMetadata metadata = EditorContext.track.trackMetadata;
        bool madeChange = false;

        UIUtils.UpdatePropertyInMemory(
            ref metadata.title, trackTitle.text, ref madeChange);
        UIUtils.UpdatePropertyInMemory(
            ref metadata.artist, artist.text, ref madeChange);
        UIUtils.UpdatePropertyInMemory(
            ref metadata.genre, genre.text, ref madeChange);
        UIUtils.UpdatePropertyInMemory(
            ref metadata.additionalCredits,
            additionalCredits.text, ref madeChange);

        UIUtils.UpdatePropertyInMemory(
            ref metadata.eyecatchImage, eyecatchImage, ref madeChange);
        UIUtils.UpdatePropertyInMemory(
            ref metadata.previewTrack, previewTrack, ref madeChange);
        UIUtils.ClampInputField(startTime, 0.0, double.MaxValue);
        UIUtils.UpdatePropertyInMemory(
            ref metadata.previewStartTime, startTime.text, ref madeChange);
        UIUtils.ClampInputField(endTime, 0.0, double.MaxValue);
        UIUtils.UpdatePropertyInMemory(
            ref metadata.previewEndTime, endTime.text, ref madeChange);

        if (madeChange)
        {
            EditorContext.DoneWithChange();
        }
    }

    public void OnEyecatchUpdated()
    {
        RefreshEyecatchPreview();
    }

    public void RefreshEyecatchPreview()
    {
        eyecatchPreview.LoadImage(EditorContext.trackFolder,
            EditorContext.track.trackMetadata);
    }

    public void OnPlayPreviewButtonClick()
    {
        previewTrackPlayer.Play(
            EditorContext.trackFolder,
            EditorContext.track.trackMetadata,
            loop: false);
    }

    public void OnDeleteTrackButtonClick()
    {
        confirmDialog.Show(
            $"This will permanently delete \"{EditorContext.trackFolder}\" and every file in it. Continue?",
            "delete",
            "cancel",
            () =>
            {
                Directory.Delete(EditorContext.trackFolder, recursive: true);
                GetComponentInChildren<TransitionToPanelWhenNotDirty>().ForceTransition();
            });
    }

    #endregion

    #region Patterns tab
    [Header("Patterns tab")]
    public PatternRadioList patternList;
    public GameObject patternMetadata;
    public GameObject patternButtons;
    public GameObject noPatternSelectedNotice;
    public TMP_InputField patternName;
    public TMP_InputField patternAuthor;
    public TMP_Dropdown controlScheme;
    public TMP_InputField patternLevel;
    public TMP_Dropdown patternBackingTrack;
    public TMP_Dropdown backgroundImage;
    public TMP_Dropdown backgroundVideo;
    public TMP_InputField bgaOffset;
    public TMP_InputField firstBeatOffset;
    public TMP_InputField initialBpm;
    public TMP_InputField bps;
    public TransitionToPanel transitionToPatternPanel;

    // Keep a reference to the selected pattern, so we can
    // re-select it when the pattern list refreshes.
    //
    // However this reference will be invalidated upon undo/redo.
    // Must refresh it upon undo/redo.
    private Pattern selectedPattern;

    private void RefreshPatternsTab()
    {
        RefreshFilenameCachesIfNull();
        if (selectedPattern != null)
        {
            selectedPattern = EditorContext.track.FindPatternByGuid(
                selectedPattern.patternMetadata.guid);
        }
        RefreshPatternList();
        RefreshPatternMetadata();
    }

    private void RefreshPatternList()
    {
        patternList.InitializeAndReturnFirstPatternObject(
            EditorContext.track, selectedPattern);
    }

    private void RefreshPatternMetadata()
    {
        patternMetadata.SetActive(selectedPattern != null);
        patternButtons.SetActive(selectedPattern != null);
        noPatternSelectedNotice.SetActive(selectedPattern == null);
        if (selectedPattern == null) return;

        PatternMetadata m = selectedPattern.patternMetadata;
        patternName.SetTextWithoutNotify(m.patternName);
        patternAuthor.SetTextWithoutNotify(m.author);
        controlScheme.SetValueWithoutNotify((int)m.controlScheme);
        patternLevel.SetTextWithoutNotify(m.level.ToString());

        UIUtils.MemoryToDropdown(patternBackingTrack,
            m.backingTrack, audioFilesCache);
        UIUtils.MemoryToDropdown(backgroundImage,
            m.backImage, imageFilesCache);
        UIUtils.MemoryToDropdown(backgroundVideo,
            m.bga, videoFilesCache);
        bgaOffset.SetTextWithoutNotify(m.bgaOffset.ToString());

        firstBeatOffset.SetTextWithoutNotify(m.firstBeatOffset.ToString());
        initialBpm.SetTextWithoutNotify(m.initBpm.ToString());
        bps.SetTextWithoutNotify(m.bps.ToString());

        foreach (TMP_InputField field in new List<TMP_InputField>()
        {
            patternName,
            patternAuthor,
            patternLevel,
            bgaOffset,
            firstBeatOffset,
            initialBpm,
            bps
        })
        {
            field.GetComponent<MaterialTextField>().RefreshMiniLabel();
        }
    }

    private void SelectedPatternChanged(Pattern newSelection)
    {
        selectedPattern = newSelection;
        RefreshPatternMetadata();
    }

    public void OnPatternMetadataChanged()
    {
        PatternMetadata m = selectedPattern.patternMetadata;
        bool madeChange = false;

        UIUtils.UpdatePropertyInMemory(ref m.patternName,
            patternName.text, ref madeChange);
        UIUtils.UpdatePropertyInMemory(ref m.author,
            patternAuthor.text, ref madeChange);
        UIUtils.ClampInputField(patternLevel,
            Pattern.minLevel, Pattern.maxLevel);
        UIUtils.UpdatePropertyInMemory(ref m.level,
            patternLevel.text, ref madeChange);

        // Special handling for control scheme
        if ((int)m.controlScheme != controlScheme.value)
        {
            if (!madeChange)
            {
                EditorContext.PrepareForChange();
                madeChange = true;
            }
            m.controlScheme = (ControlScheme)controlScheme.value;
        }

        UIUtils.UpdatePropertyInMemory(ref m.backingTrack,
            patternBackingTrack, ref madeChange);
        UIUtils.UpdatePropertyInMemory(
            ref m.backImage, backgroundImage, ref madeChange);
        UIUtils.UpdatePropertyInMemory(
            ref m.bga, backgroundVideo, ref madeChange);
        UIUtils.UpdatePropertyInMemory(
            ref m.bgaOffset, bgaOffset.text, ref madeChange);

        UIUtils.UpdatePropertyInMemory(
            ref m.firstBeatOffset, firstBeatOffset.text,
            ref madeChange);
        UIUtils.ClampInputField(initialBpm,
            Pattern.minBpm, float.MaxValue);
        UIUtils.UpdatePropertyInMemory(
            ref m.initBpm, initialBpm.text, ref madeChange);
        UIUtils.ClampInputField(bps, Pattern.minBps, Pattern.maxBps);
        UIUtils.UpdatePropertyInMemory(
            ref m.bps, bps.text, ref madeChange);

        if (madeChange)
        {
            EditorContext.track.SortPatterns();
            EditorContext.DoneWithChange();
            RefreshPatternList();
        }
    }

    public void OnNewPatternButtonClick()
    {
        EditorContext.PrepareForChange();
        EditorContext.track.patterns.Add(new Pattern());
        EditorContext.track.SortPatterns();
        EditorContext.DoneWithChange();

        RefreshPatternList();
    }

    public void OnDeletePatternButtonClick()
    {
        // This is undoable, so no need for confirmation.
        EditorContext.PrepareForChange();
        EditorContext.track.patterns.Remove(selectedPattern);
        EditorContext.DoneWithChange();

        selectedPattern = null;
        RefreshPatternList();
        RefreshPatternMetadata();
    }

    public void OnDuplicatePatternButtonClick()
    {
        EditorContext.PrepareForChange();
        EditorContext.track.patterns.Add(selectedPattern.CloneWithDifferentGuid());
        EditorContext.track.SortPatterns();
        EditorContext.DoneWithChange();

        RefreshPatternList();
    }

    public void OnOpenPatternButtonClick()
    {
        int index = EditorContext.track.FindPatternIndexByGuid(
            selectedPattern.patternMetadata.guid);
        if (index < 0) return;
        EditorContext.patternIndex = index;

        transitionToPatternPanel.Invoke();
    }
    #endregion
}

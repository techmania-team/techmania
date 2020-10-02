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

    #region Refreshing
    private void OnEnable()
    {
        Tabs.tabChanged += Refresh;
        EditorContext.UndoneOrRedone += Refresh;
        EditorContext.DirtynessUpdated += RefreshTitle;
        EditorContext.UndoRedoStackUpdated += RefreshUndoRedoButtons;
        PatternRadioList.SelectedPatternChanged += SelectedPatternChanged;
        selectedPattern = null;
        Refresh();
        RefreshTitle(EditorContext.Dirty);
        RefreshUndoRedoButtons();
    }

    private void OnDisable()
    {
        Tabs.tabChanged -= Refresh;
        EditorContext.UndoneOrRedone -= Refresh;
        EditorContext.DirtynessUpdated -= RefreshTitle;
        EditorContext.UndoRedoStackUpdated -= RefreshUndoRedoButtons;
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

    #region Top bar
    [Header("Top bar")]
    public TextMeshProUGUI title;
    public Button undoButton;
    public Button redoButton;

    public void Save()
    {
        EditorContext.Save();
    }

    public void Undo()
    {
        EditorContext.Undo();
    }

    public void Redo()
    {
        EditorContext.Redo();
    }

    private void RefreshTitle(bool dirty)
    {
        string titleText = title.text.TrimEnd('*');
        if (dirty) titleText = titleText + '*';
        title.text = titleText;
    }

    private void RefreshUndoRedoButtons()
    {
        undoButton.interactable = EditorContext.CanUndo();
        redoButton.interactable = EditorContext.CanRedo();
    }

    void Update()
    {
        if (Input.GetKey(KeyCode.LeftControl) || Input.GetKey(KeyCode.RightControl))
        {
            if (Input.GetKeyDown(KeyCode.S))
            {
                Save();
            }
            if (Input.GetKeyDown(KeyCode.Z))
            {
                Undo();
            }
            if (Input.GetKeyDown(KeyCode.Y))
            {
                Redo();
            }
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
        string trackFolder = EditorContext.trackFolder;

        foreach (string source in SFB.StandaloneFileBrowser.OpenFilePanel(
            "Select resource to import", "", "wav;*.png;*.mp4", multiselect: true))
        {
            FileInfo fileInfo = new FileInfo(source);
            if (fileInfo.DirectoryName == trackFolder) continue;
            string destination = $"{trackFolder}\\{fileInfo.Name}";

            try
            {
                File.Copy(source, destination, overwrite: true);
            }
            catch (Exception e)
            {
                messageDialog.Show(
                    $"An error occurred when copying {source} to {destination}:\n\n"
                    + e.Message);
                return;
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
        string trackFolder = EditorContext.trackFolder;
        audioFilesDisplay.text = CondenseFileList(
            Paths.GetAllAudioFiles(trackFolder));
        imageFilesDisplay.text = CondenseFileList(
            Paths.GetAllImageFiles(trackFolder));
        videoFilesDisplay.text = CondenseFileList(
            Paths.GetAllVideoFiles(trackFolder));
    }
    #endregion

    #region Metadata tab
    [Header("Metadata tab")]
    public TMP_InputField trackTitle;
    public TMP_InputField artist;
    public TMP_InputField genre;
    public EyecatchSelfLoader eyecatchPreview;
    public TMP_Dropdown eyecatchImage;
    public TMP_Dropdown previewTrack;
    public TMP_InputField startTime;
    public TMP_InputField endTime;
    public TMP_Dropdown backgroundImage;
    public TMP_Dropdown backgroundVideo;
    public TMP_InputField bgaOffset;
    public PreviewTrackPlayer previewTrackPlayer;

    private List<string> audioFilesCache;
    private List<string> imageFilesCache;
    private List<string> videoFilesCache;

    public void RefreshMetadataTab()
    {
        TrackMetadata metadata = EditorContext.track.trackMetadata;
        audioFilesCache = Paths.GetAllAudioFiles(EditorContext.trackFolder);
        imageFilesCache = Paths.GetAllImageFiles(EditorContext.trackFolder);
        videoFilesCache = Paths.GetAllVideoFiles(EditorContext.trackFolder);

        trackTitle.SetTextWithoutNotify(metadata.title);
        artist.SetTextWithoutNotify(metadata.artist);
        genre.SetTextWithoutNotify(metadata.genre);

        UIUtils.MemoryToDropdown(eyecatchImage,
            metadata.eyecatchImage, imageFilesCache);
        UIUtils.MemoryToDropdown(previewTrack,
            metadata.previewTrack, audioFilesCache);
        startTime.SetTextWithoutNotify(metadata.previewStartTime.ToString());
        endTime.SetTextWithoutNotify(metadata.previewEndTime.ToString());

        UIUtils.MemoryToDropdown(backgroundImage,
            metadata.backImage, imageFilesCache);
        UIUtils.MemoryToDropdown(backgroundVideo,
            metadata.bga, videoFilesCache);
        bgaOffset.SetTextWithoutNotify(metadata.bgaOffset.ToString());

        foreach (TMP_InputField field in new List<TMP_InputField>()
        {
            trackTitle,
            artist,
            genre,
            startTime,
            endTime,
            bgaOffset
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
            ref metadata.eyecatchImage, eyecatchImage, ref madeChange);
        UIUtils.UpdatePropertyInMemory(
            ref metadata.previewTrack, previewTrack, ref madeChange);
        UIUtils.ClampInputField(startTime, 0.0, double.MaxValue);
        UIUtils.UpdatePropertyInMemory(
            ref metadata.previewStartTime, startTime.text, ref madeChange);
        UIUtils.ClampInputField(endTime, 0.0, double.MaxValue);
        UIUtils.UpdatePropertyInMemory(
            ref metadata.previewEndTime, endTime.text, ref madeChange);

        UIUtils.UpdatePropertyInMemory(
            ref metadata.backImage, backgroundImage, ref madeChange);
        UIUtils.UpdatePropertyInMemory(
            ref metadata.bga, backgroundVideo, ref madeChange);
        UIUtils.UpdatePropertyInMemory(
            ref metadata.bgaOffset, bgaOffset.text, ref madeChange);

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
            EditorContext.track.trackMetadata.previewTrack,
            EditorContext.track.trackMetadata.previewStartTime,
            EditorContext.track.trackMetadata.previewEndTime,
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
    public GameObject noPatternSelectedNotice;
    public TMP_InputField patternName;
    public TMP_InputField patternAuthor;
    public TMP_Dropdown controlScheme;
    public TMP_InputField patternLevel;
    public TMP_Dropdown patternBackingTrack;
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
        audioFilesCache = Paths.GetAllAudioFiles(EditorContext.trackFolder);
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
        noPatternSelectedNotice.SetActive(selectedPattern == null);
        if (selectedPattern == null) return;

        PatternMetadata m = selectedPattern.patternMetadata;
        patternName.SetTextWithoutNotify(m.patternName);
        patternAuthor.SetTextWithoutNotify(m.author);
        controlScheme.SetValueWithoutNotify((int)m.controlScheme);
        patternLevel.SetTextWithoutNotify(m.level.ToString());

        UIUtils.MemoryToDropdown(patternBackingTrack,
            m.backingTrack, audioFilesCache);

        firstBeatOffset.SetTextWithoutNotify(m.firstBeatOffset.ToString());
        initialBpm.SetTextWithoutNotify(m.initBpm.ToString());
        bps.SetTextWithoutNotify(m.bps.ToString());

        foreach (TMP_InputField field in new List<TMP_InputField>()
        {
            patternName,
            patternAuthor,
            patternLevel,
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
            ref m.firstBeatOffset, firstBeatOffset.text, ref madeChange);
        UIUtils.ClampInputField(initialBpm, Pattern.minBpm, Pattern.maxBpm);
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

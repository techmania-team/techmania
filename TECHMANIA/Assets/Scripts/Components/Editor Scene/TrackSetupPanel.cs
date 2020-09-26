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
    public Tabs tabs;

    #region Refreshing
    private void OnEnable()
    {
        Tabs.tabChanged += Refresh;
        EditorContext.StateUpdated += Refresh;
        EditorContext.DirtynessUpdated += RefreshTitle;
        Refresh();
    }

    private void OnDisable()
    {
        Tabs.tabChanged -= Refresh;
        EditorContext.StateUpdated -= Refresh;
        EditorContext.DirtynessUpdated -= RefreshTitle;
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
        }
    }
    #endregion

    #region Top bar
    [Header("Top bar")]
    public TextMeshProUGUI title;

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

    }

    public void OnDeleteTrackButtonClick()
    {

    }

    #endregion
}

using System;
using System.Collections;
using System.Collections.Generic;
using System.IO;
using TMPro;
using UnityEngine;

public class EditSetlistPanel : MonoBehaviour
{
    public MessageDialog messageDialog;
    public ConfirmDialog confirmDialog;

    #region Filename caching
    private List<string> imageFilesCache;

    private void RefreshFilenameCaches()
    {
        imageFilesCache = Paths.GetAllImageFiles(
            EditorContext.setlistFolder);
    }
    #endregion

    #region Refreshing
    private void OnEnable()
    {
        EditorContext.UndoInvoked += OnUndoOrRedo;
        EditorContext.RedoInvoked += OnUndoOrRedo;
        RefreshFilenameCaches();
        Refresh();

        DiscordController.SetActivity(DiscordActivityType.EditorSetlist);
    }

    private void OnDisable()
    {
        EditorContext.UndoInvoked -= OnUndoOrRedo;
        EditorContext.RedoInvoked -= OnUndoOrRedo;
    }

    private void OnApplicationFocus(bool focus)
    {
        if (focus)
        {
            RefreshFilenameCaches();
            Refresh();
        }
    }

    private void OnUndoOrRedo(EditTransaction t)
    {
        Refresh();
    }

    private void Refresh()
    {
        RefreshResourceSection();
        RefreshMetadataSection();
        RefreshSelectablePatterns();
        RefreshHiddenPatterns();
    }
    #endregion

    #region Resources
    [Header("Resources")]
    public TextMeshProUGUI imageFilesDisplay;

    public void OnImportButtonClick()
    {
        EditorUtilities.ImportResource(new string[]
            { "jpg", "png" },
            copyDestinationFolder: EditorContext.setlistFolder,
            messageDialog, confirmDialog,
            completeCopyCallback: () =>
            {
                RefreshFilenameCaches();
                RefreshResourceSection();
                RefreshMetadataSection();
            });
    }

    public void RefreshResourceSection()
    {
        imageFilesDisplay.text = EditorUtilities.CondenseFileList(
            imageFilesCache, EditorContext.setlistFolder);
    }
    #endregion

    #region Metadata
    [Header("Metadata")]
    public TMP_InputField title;
    public TMP_InputField description;
    public TMP_Dropdown controlScheme;
    public TMP_Dropdown backgroundImage;
    public TMP_Dropdown eyecatchImage;
    public EyecatchSelfLoader eyecatchPreview;

    public void RefreshMetadataSection()
    {
        SetlistMetadata metadata = EditorContext.setlist.setlistMetadata;

        title.SetTextWithoutNotify(metadata.title);
        title.GetComponent<MaterialTextField>().RefreshMiniLabel();
        description.SetTextWithoutNotify(metadata.description);
        description.GetComponent<MaterialTextField>().RefreshMiniLabel();

        controlScheme.options.Clear();
        controlScheme.options.Add(new TMP_Dropdown.OptionData(
            L10n.GetString("control_scheme_touch")));
        controlScheme.options.Add(new TMP_Dropdown.OptionData(
            L10n.GetString("control_scheme_keys")));
        controlScheme.options.Add(new TMP_Dropdown.OptionData(
            L10n.GetString("control_scheme_km")));
        controlScheme.SetValueWithoutNotify((int)metadata.controlScheme);

        UIUtils.MemoryToDropdown(backgroundImage, 
            metadata.backImage, imageFilesCache,
            EditorContext.setlistFolder);
        UIUtils.MemoryToDropdown(eyecatchImage,
            metadata.eyecatchImage, imageFilesCache,
            EditorContext.setlistFolder);

        RefreshEyecatchPreview();
    }

    public void OnMetadataUpdated()
    {
        SetlistMetadata metadata = EditorContext.setlist.setlistMetadata;
        bool madeChange = false;

        UIUtils.UpdateSetlistMetadataInMemory(
            ref metadata.title, title.text, ref madeChange);
        UIUtils.UpdateSetlistMetadataInMemory(
            ref metadata.description, description.text, ref madeChange);

        // Special handling for control scheme
        if ((int)metadata.controlScheme != controlScheme.value)
        {
            if (!madeChange)
            {
                EditorContext.PrepareToModifySetlist();
                madeChange = true;
            }
            metadata.controlScheme = (ControlScheme)controlScheme.value;
        }

        UIUtils.UpdateSetlistMetadataInMemory(
            ref metadata.backImage, backgroundImage, ref madeChange);
        UIUtils.UpdateSetlistMetadataInMemory(
            ref metadata.eyecatchImage, eyecatchImage, ref madeChange);
    }

    public void OnControlSchemeUpdated()
    {
        RefreshSelectablePatterns();
        RefreshHiddenPatterns();
    }

    public void OnEyecatchUpdated()
    {
        RefreshEyecatchPreview();
    }

    public void RefreshEyecatchPreview()
    {
        eyecatchPreview.LoadImage(EditorContext.setlistFolder,
            EditorContext.setlist.setlistMetadata);
    }

    public void OnDeleteSetlistButtonClick()
    {
        confirmDialog.Show(
            L10n.GetStringAndFormatIncludingPaths(
                "edit_setlist_panel_delete_setlist_confirmation",
                EditorContext.setlistFolder),
            L10n.GetString(
                "edit_setlist_panel_delete_setlist_confirm"),
            L10n.GetString(
                "edit_setlist_panel_delete_setlist_cancel"),
            () =>
            {
                // Delete from disk
                Directory.Delete(EditorContext.setlistFolder,
                    recursive: true);

                // Delete from RAM
                string parent = Path.GetDirectoryName(
                    EditorContext.setlistFolder);
                GlobalResource.setlistList[parent].RemoveAll(
                    (GlobalResource.SetlistInFolder t) =>
                    {
                        return t.folder == EditorContext.setlistFolder;
                    });

                GetComponentInChildren<
                    CustomTransitionFromEditSetlistPanel>().
                    Transition();
            });
    }
    #endregion

    #region Selectable patterns
    [Header("Selectable patterns")]
    public TrackAndPatternSideSheet sidesheet;
    public RectTransform selectablePatternContainer;
    public GameObject selectablePatternPrefab;
    public GameObject patternDividerPrefab;
    public GameObject selectablePatternNotEnoughLabel;

    public void RefreshSelectablePatterns()
    {
        for (int i = 0; i < selectablePatternContainer.childCount; i++)
        {
            Destroy(selectablePatternContainer.GetChild(i).gameObject);
        }

        int numPatterns = EditorContext.setlist
            .selectablePatterns.Count;
        for (int i = 0; i < numPatterns; i++)
        {
            Setlist.PatternReference reference = EditorContext.setlist
                .selectablePatterns[i];

            SelectablePatternInSetlist selectablePattern = Instantiate(
                selectablePatternPrefab, selectablePatternContainer)
                .GetComponent<SelectablePatternInSetlist>();

            GlobalResource.TrackInFolder trackInFolder;
            Pattern pattern;
            Status status = GlobalResource.SearchForPatternReference(
                reference, out trackInFolder, out pattern);
            if (!status.Ok())
            {
                selectablePattern.SetUpNonExistant(this, i, numPatterns);
            }
            else
            {
                selectablePattern.SetUp(this,
                    trackInFolder.minimizedTrack.trackMetadata.title,
                    pattern.patternMetadata, i, numPatterns);
            }

            if (i < numPatterns - 1)
            {
                Instantiate(patternDividerPrefab, 
                    selectablePatternContainer);
            }
        }

        selectablePatternNotEnoughLabel.SetActive(
            EditorContext.setlist.selectablePatterns.Count < 3);
    }

    public void OnAddSelectablePatternButtonClick()
    {
        sidesheet.callback = (Setlist.PatternReference reference) =>
        {
            EditorContext.PrepareToModifySetlist();
            EditorContext.setlist.selectablePatterns.Add(reference);
            RefreshSelectablePatterns();
        };
        sidesheet.startingTrack = null;
        sidesheet.GetComponent<Sidesheet>().FadeIn();
    }

    public void DeleteSelectablePattern(int index)
    {
        EditorContext.PrepareToModifySetlist();
        EditorContext.setlist.selectablePatterns.RemoveAt(index);
        RefreshSelectablePatterns();
    }

    public void MoveSelectablePattern(int index, int direction)
    {
        EditorContext.PrepareToModifySetlist();
        Setlist.PatternReference reference = EditorContext.setlist
            .selectablePatterns[index];
        EditorContext.setlist.selectablePatterns.RemoveAt(index);
        EditorContext.setlist.selectablePatterns.Insert(
            index + direction, reference);
        RefreshSelectablePatterns();
    }

    public void ReplaceSelectablePattern(int index, bool changeTrack)
    {
        sidesheet.callback = (Setlist.PatternReference reference) =>
        {
            EditorContext.PrepareToModifySetlist();
            EditorContext.setlist.selectablePatterns[index] = reference;
            RefreshSelectablePatterns();
        };
        // If changing pattern within the same track, need to tell
        // the sidesheet about the track.
        sidesheet.startingTrack = changeTrack ? null :
            EditorContext.setlist.selectablePatterns[index];
        sidesheet.GetComponent<Sidesheet>().FadeIn();
    }
    #endregion

    #region Hidden patterns
    [Header("Hidden patterns")]
    public RectTransform hiddenPatternContainer;
    public GameObject hiddenPatternPrefab;
    public GameObject hiddenPatternNotEnoughLabel;

    public void RefreshHiddenPatterns()
    {
        for (int i = 0; i < hiddenPatternContainer.childCount; i++)
        {
            Destroy(hiddenPatternContainer.GetChild(i).gameObject);
        }

        int numPatterns = EditorContext.setlist
            .hiddenPatterns.Count;
        for (int i = 0; i < numPatterns; i++)
        {
            Setlist.HiddenPattern hiddenPattern =
                EditorContext.setlist.hiddenPatterns[i];
            Setlist.PatternReference reference = hiddenPattern.reference;

            HiddenPatternInSetlist patternButton = Instantiate(
                hiddenPatternPrefab, hiddenPatternContainer)
                .GetComponent<HiddenPatternInSetlist>();

            GlobalResource.TrackInFolder trackInFolder;
            Pattern pattern;
            Status status = GlobalResource.SearchForPatternReference(
                reference, out trackInFolder, out pattern);
            if (!status.Ok())
            {
                patternButton.SetUpNonExistant(this, hiddenPattern,
                    i, numPatterns);
            }
            else
            {
                patternButton.SetUp(this,
                    trackInFolder.minimizedTrack.trackMetadata.title,
                    pattern.patternMetadata,
                    hiddenPattern,
                    i, numPatterns);
            }

            if (i < numPatterns - 1)
            {
                Instantiate(patternDividerPrefab,
                    hiddenPatternContainer);
            }
        }

        hiddenPatternNotEnoughLabel.SetActive(EditorContext.setlist
            .hiddenPatterns.Count < 1);
    }

    public void OnAddHiddenPatternButtonClick()
    {
        sidesheet.startingTrack = null;
        sidesheet.callback = (Setlist.PatternReference reference) =>
        {
            EditorContext.PrepareToModifySetlist();
            Setlist.HiddenPattern hiddenPattern =
                new Setlist.HiddenPattern()
                {
                    reference = reference
                };
            EditorContext.setlist.hiddenPatterns.Add(hiddenPattern);
            RefreshHiddenPatterns();
        };
        sidesheet.GetComponent<Sidesheet>().FadeIn();
    }

    public void DeleteHiddenPattern(int index)
    {
        EditorContext.PrepareToModifySetlist();
        EditorContext.setlist.hiddenPatterns.RemoveAt(index);
        RefreshHiddenPatterns();
    }

    public void MoveHiddenPattern(int index, int direction)
    {
        EditorContext.PrepareToModifySetlist();
        Setlist.HiddenPattern hiddenPattern = EditorContext.setlist
            .hiddenPatterns[index];
        EditorContext.setlist.hiddenPatterns.RemoveAt(index);
        EditorContext.setlist.hiddenPatterns.Insert(
            index + direction, hiddenPattern);
        RefreshHiddenPatterns();
    }

    public void ReplaceHiddenPattern(int index, bool changeTrack)
    {
        sidesheet.callback = (Setlist.PatternReference reference) =>
        {
            EditorContext.PrepareToModifySetlist();
            Setlist.HiddenPattern hiddenPattern =
                new Setlist.HiddenPattern()
                {
                    reference = reference
                };
            EditorContext.setlist.hiddenPatterns[index] = hiddenPattern;
            RefreshHiddenPatterns();
        };
        // If changing pattern within the same track, need to tell
        // the sidesheet about the track.
        sidesheet.startingTrack = changeTrack ? null :
            EditorContext.setlist.hiddenPatterns[index].reference;
        sidesheet.GetComponent<Sidesheet>().FadeIn();
    }

    public void ChangeCriteriaType(int index, 
        Setlist.HiddenPatternCriteriaType newType)
    {
        EditorContext.PrepareToModifySetlist();
        EditorContext.setlist.hiddenPatterns[index]
            .criteriaType = newType;
        // No need to refresh
    }

    public void ChangeCriteriaDirection(int index,
        Setlist.HiddenPatternCriteriaDirection newDirection)
    {
        EditorContext.PrepareToModifySetlist();
        EditorContext.setlist.hiddenPatterns[index]
            .criteriaDirection = newDirection;
        // No need to refresh
    }

    public void ChangeCriteriaValue(int index, int newValue)
    {
        EditorContext.PrepareToModifySetlist();
        EditorContext.setlist.hiddenPatterns[index]
            .criteriaValue = newValue;
        // No need to refresh
    }
    #endregion
}

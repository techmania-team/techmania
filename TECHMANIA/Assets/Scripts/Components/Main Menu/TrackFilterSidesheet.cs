using System.Collections;
using System.Collections.Generic;
using TMPro;
using UnityEngine;
using UnityEngine.Events;
using UnityEngine.UI;

public class TrackFilterSidesheet : MonoBehaviour
{
    public TMP_InputField keywordField;
    public Toggle showTracksInAllFolders;
    public TMP_Dropdown sortBasis;
    public MaterialRadioButton ascendingButton;
    public MaterialRadioButton descendingButton;

    public static event UnityAction trackFilterChanged;
    public string searchKeyword { get; private set; }

    public void ResetSearchKeyword()
    {
        searchKeyword = "";
    }

    private void OnEnable()
    {
        MemoryToUI();
    }

    private void OnDisable()
    {
        Options.instance.SaveToFile(Paths.GetOptionsFilePath());
    }

    private void MemoryToUI()
    {
        UIUtils.InitializeDropdownWithLocalizedOptions(
            sortBasis,
            TrackFilter.sortBasisDisplayKeys);

        keywordField.SetTextWithoutNotify(searchKeyword);
        showTracksInAllFolders.SetIsOnWithoutNotify(
            TrackFilter.instance.showTracksInAllFolders);
        sortBasis.SetValueWithoutNotify((int)
            TrackFilter.instance.sortBasis);
        sortBasis.RefreshShownValue();

        RefreshRadioButtons();
    }

    private void RefreshRadioButtons()
    {
        ascendingButton.SetIsOn(
            TrackFilter.instance.sortOrder == 
            TrackFilter.SortOrder.Ascending);
        descendingButton.SetIsOn(
            TrackFilter.instance.sortOrder ==
            TrackFilter.SortOrder.Descending);
    }

    public void OnAscendingButtonClick()
    {
        if (TrackFilter.instance.sortOrder == 
            TrackFilter.SortOrder.Ascending)
        {
            // Do nothing.
            return;
        }

        TrackFilter.instance.sortOrder = 
            TrackFilter.SortOrder.Ascending;
        RefreshRadioButtons();
        trackFilterChanged?.Invoke();
    }

    public void OnDescendingButtonClick()
    {
        if (TrackFilter.instance.sortOrder ==
            TrackFilter.SortOrder.Descending)
        {
            // Do nothing.
            return;
        }

        TrackFilter.instance.sortOrder =
            TrackFilter.SortOrder.Descending;
        RefreshRadioButtons();
        trackFilterChanged?.Invoke();
    }

    public void UIToMemory()
    {
        // trackFilterChanged causes the select track panel to
        // refresh, so we only fire it when absolutely necessary.
        bool anyChange = false;

        if (searchKeyword != keywordField.text)
        {
            anyChange = true;
            searchKeyword = keywordField.text;
        }

        if (TrackFilter.instance.showTracksInAllFolders !=
            showTracksInAllFolders.isOn)
        {
            anyChange = true;
            TrackFilter.instance.showTracksInAllFolders =
            showTracksInAllFolders.isOn;
        }

        if ((int)TrackFilter.instance.sortBasis !=
            sortBasis.value)
        {
            anyChange = true;
            TrackFilter.instance.sortBasis = (TrackFilter.SortBasis)
            sortBasis.value;
        }

        if (anyChange)
        {
            trackFilterChanged?.Invoke();
        }
    }
}

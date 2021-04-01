using System.Collections;
using System.Collections.Generic;
using System.IO;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

public class SelectSkinPanel : MonoBehaviour
{
    public BackButton backButton;
    public GlobalResourceLoader resourceLoader;
    public MessageDialog messageDialog;

    public TMP_Dropdown noteSkinDropdown;
    public TMP_Dropdown vfxSkinDropdown;
    public TMP_Dropdown comboSkinDropdown;
    public Toggle reloadSkinsToggle;

    public GameObject notePreview;
    public GameObject vfxPrefab;
    public GameObject comboTextPreview;

    private void InitializeDropdown(TMP_Dropdown dropdown,
        string skinFolder, string currentSkinName)
    {
        dropdown.options.Clear();
        int value = 0, index = 0;
        foreach (string folder in
            Directory.EnumerateDirectories(skinFolder))
        {
            // folder does not end in directory separator.
            string skinName = Path.GetFileName(folder);
            if (skinName == currentSkinName)
            {
                value = index;
            }
            index++;
            dropdown.options.Add(new TMP_Dropdown.OptionData(skinName));
        }

        if (dropdown.options.Count == 0)
        {
            dropdown.options.Add(new TMP_Dropdown.OptionData(
                UIUtils.kNone));
        }

        dropdown.SetValueWithoutNotify(value);
        dropdown.RefreshShownValue();
    }

    private void OnEnable()
    {
        InitializeDropdown(noteSkinDropdown,
            Paths.GetNoteSkinRootFolder(), Options.instance.noteSkin);
        InitializeDropdown(vfxSkinDropdown,
            Paths.GetVfxSkinRootFolder(), Options.instance.vfxSkin);
        InitializeDropdown(comboSkinDropdown,
            Paths.GetComboSkinRootFolder(), Options.instance.comboSkin);
        reloadSkinsToggle.SetIsOnWithoutNotify(
            Options.instance.reloadSkinsWhenLoadingPattern);
    }

    private void OnDisable()
    {
        Options.instance.SaveToFile(Paths.GetOptionsFilePath());
    }

    public void OnNoteSkinChanged()
    {
        UIToMemory();

        StopPreview();
        backButton.GetComponent<Button>().interactable = false;
        resourceLoader.LoadNoteSkin(progressCallback: null,
            completeCallback: OnSkinLoaded);
    }

    public void OnVfxSkinChanged()
    {
        UIToMemory();

        StopPreview();
        backButton.GetComponent<Button>().interactable = false;
        resourceLoader.LoadVfxSkin(progressCallback: null,
            completeCallback: OnSkinLoaded);
    }

    public void OnComboSkinChanged()
    {
        UIToMemory();

        StopPreview();
        backButton.GetComponent<Button>().interactable = false;
        resourceLoader.LoadComboSkin(progressCallback: null,
            completeCallback: OnSkinLoaded);
    }

    public void OnSkinLoaded(string error)
    {
        if (error != null)
        {
            messageDialog.Show(error);
        }
        RestartPreview();
        backButton.GetComponent<Button>().interactable = true;
    }

    private void StopPreview()
    {

    }

    private void RestartPreview()
    {

    }

    public void UIToMemory()
    {
        Options.instance.noteSkin = noteSkinDropdown.options[
            noteSkinDropdown.value].text;
        Options.instance.vfxSkin = vfxSkinDropdown.options[
            vfxSkinDropdown.value].text;
        Options.instance.comboSkin = comboSkinDropdown.options[
            comboSkinDropdown.value].text;
        Options.instance.reloadSkinsWhenLoadingPattern =
            reloadSkinsToggle.isOn;
    }
}

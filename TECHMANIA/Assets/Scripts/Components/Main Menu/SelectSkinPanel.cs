using System.Collections;
using System.Collections.Generic;
using System.IO;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

public class SelectSkinPanel : MonoBehaviour
{
    public BackButton backButton;

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
        // TODO: reload note
    }

    public void OnVfxSkinChanged()
    {
        UIToMemory();
        // TODO: reload VFX
    }

    public void OnComboSkinChanged()
    {
        UIToMemory();
        // TODO: reload combo
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

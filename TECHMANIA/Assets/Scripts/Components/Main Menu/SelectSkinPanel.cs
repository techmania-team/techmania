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
    public TMP_Dropdown gameUiSkinDropdown;
    public Toggle reloadSkinsToggle;

    public Image scanlinePreview;
    public Image notePreview;
    public GameObject vfxPrefab;
    public Transform vfxContainer;
    public ComboText comboPreview;

    private System.Diagnostics.Stopwatch stopwatch;
    private const float beatPerSecond = 1.5f;
    private float previousBeat;
    private bool showPreview;
    private List<GameObject> vfxInstances;

    private void InitializeDropdown(TMP_Dropdown dropdown,
        string skinFolder, string currentSkinName)
    {
        dropdown.options.Clear();
        int value = 0, index = 0;
        bool foundOption = false;
        foreach (string folder in
            Directory.EnumerateDirectories(skinFolder))
        {
            // folder does not end in directory separator.
            string skinName = Path.GetFileName(folder);
            if (skinName == currentSkinName)
            {
                value = index;
                foundOption = true;
            }
            index++;
            dropdown.options.Add(new TMP_Dropdown.OptionData(skinName));
        }

        if (dropdown.options.Count == 0)
        {
            dropdown.options.Add(new TMP_Dropdown.OptionData(
                UIUtils.NoneOptionInDropdowns()));
        }
        if (!foundOption)
        {
            dropdown.onValueChanged.Invoke(0);  // This causes a reload
        }
        else
        {
            dropdown.SetValueWithoutNotify(value);
        }

        dropdown.RefreshShownValue();
    }

    private void OnEnable()
    {
        InitializeDropdown(noteSkinDropdown,
            Paths.GetNoteSkinRootFolder(), Options.instance.noteSkin);
        InitializeDropdown(vfxSkinDropdown,
            Paths.GetVfxSkinRootFolder(), Options.instance.vfxSkin);
        InitializeDropdown(comboSkinDropdown,
            Paths.GetComboSkinRootFolder(), 
            Options.instance.comboSkin);
        InitializeDropdown(gameUiSkinDropdown,
            Paths.GetGameUiSkinRootFolder(),
            Options.instance.gameUiSkin);
        reloadSkinsToggle.SetIsOnWithoutNotify(
            Options.instance.reloadSkinsWhenLoadingPattern);

        Scan.InjectLaneHeight(120f);
        stopwatch = new System.Diagnostics.Stopwatch();
        vfxInstances = new List<GameObject>();
        RestartPreview();
    }

    private void OnDisable()
    {
        Options.instance.SaveToFile(Paths.GetOptionsFilePath());
    }

    private void Update()
    {
        Game.InjectBaseTimeAndOffset(
            (float)stopwatch.Elapsed.TotalSeconds,
            offset: 0f);
        float beat = Game.Time * beatPerSecond;
        while (beat > 4f) beat -= 4f;
        bool noteVisible = beat < 2f;
        bool resolveNote = (previousBeat < 2f && beat >= 2f);

        if (showPreview &&
            GlobalResource.noteSkin != null &&
            GlobalResource.vfxSkin != null &&
            GlobalResource.comboSkin != null &&
            GlobalResource.gameUiSkin != null)
        {
            RectTransform scanlineRect = scanlinePreview
                .GetComponent<RectTransform>();
            scanlineRect.anchoredPosition = new Vector2(
                beat * 250f - 500f,
                scanlineRect.anchoredPosition.y);

            scanlinePreview.sprite = GlobalResource.gameUiSkin
                .scanline.GetSpriteForFloatBeat(beat);
            notePreview.sprite = GlobalResource.noteSkin.basic.
                GetSpriteForFloatBeat(beat);
            if (resolveNote)
            {
                // VFX
                List<GameObject> remainingVfxInstances = new 
                    List<GameObject>();
                foreach (GameObject instance in vfxInstances)
                {
                    if (instance != null)
                    {
                        remainingVfxInstances.Add(instance);
                    }
                }
                foreach (SpriteSheet layer in
                    GlobalResource.vfxSkin.basicMax)
                {
                    GameObject vfx = Instantiate(
                        vfxPrefab, vfxContainer);
                    vfx.GetComponent<VFXDrawer>().Initialize(
                        notePreview.transform.position,
                        layer, loop: false);
                    remainingVfxInstances.Add(vfx);
                }
                vfxInstances = remainingVfxInstances;

                // Combo
                Game.InjectFeverAndCombo(Game.FeverState.Idle,
                    currentCombo: 123);
                comboPreview.Show(null, Judgement.RainbowMax);
            }

            scanlinePreview.color = Color.white;
            if (noteVisible)
            {
                notePreview.color = Color.white;
            }
            else
            {
                notePreview.color = Color.clear;
            }
        }
        else
        {
            scanlinePreview.color = Color.clear;
            notePreview.color = Color.clear;
        }

        previousBeat = beat;
    }

    public void OnNoteSkinChanged()
    {
        UIToMemory();

        PrepareToLoadSkin();
        resourceLoader.LoadNoteSkin(progressCallback: null,
            completeCallback: OnSkinLoaded);
    }

    public void OnVfxSkinChanged()
    {
        UIToMemory();

        PrepareToLoadSkin();
        resourceLoader.LoadVfxSkin(progressCallback: null,
            completeCallback: OnSkinLoaded);
    }

    public void OnComboSkinChanged()
    {
        UIToMemory();

        PrepareToLoadSkin();
        resourceLoader.LoadComboSkin(progressCallback: null,
            completeCallback: OnSkinLoaded);
    }

    public void OnGameUiSkinChanged()
    {
        UIToMemory();

        PrepareToLoadSkin();
        resourceLoader.LoadGameUiSkin(progressCallback: null,
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
        noteSkinDropdown.interactable = true;
        vfxSkinDropdown.interactable = true;
        comboSkinDropdown.interactable = true;
        gameUiSkinDropdown.interactable = true;
    }

    private void PrepareToLoadSkin()
    {
        showPreview = false;

        // Controls
        backButton.GetComponent<Button>().interactable = false;
        noteSkinDropdown.interactable = false;
        vfxSkinDropdown.interactable = false;
        comboSkinDropdown.interactable = false;
        gameUiSkinDropdown.interactable = false;

        // VFX preview
        if (vfxInstances != null)
        {
            foreach (GameObject o in vfxInstances)
            {
                Destroy(o);
            }
            vfxInstances.Clear();
        }

        // Combo preview
        comboPreview.Hide();
    }

    private void RestartPreview()
    {
        showPreview = true;

        // Note preview
        if (GlobalResource.noteSkin != null)
        {
            float noteScale = GlobalResource.noteSkin.basic.scale;
            float noteSize = Scan.laneHeight * noteScale;
            notePreview.GetComponent<RectTransform>().sizeDelta =
                new Vector2(noteSize, noteSize);
        }

        // Combo preview
        if (GlobalResource.comboSkin != null)
        {
            comboPreview.ResetSizes();
        }

        // Timing
        stopwatch.Restart();
        previousBeat = 0f;
    }

    public void UIToMemory()
    {
        Options.instance.noteSkin = noteSkinDropdown.options[
            noteSkinDropdown.value].text;
        Options.instance.vfxSkin = vfxSkinDropdown.options[
            vfxSkinDropdown.value].text;
        Options.instance.comboSkin = comboSkinDropdown.options[
            comboSkinDropdown.value].text;
        Options.instance.gameUiSkin = gameUiSkinDropdown.options[
            gameUiSkinDropdown.value].text;
        Options.instance.reloadSkinsWhenLoadingPattern =
            reloadSkinsToggle.isOn;
    }
}

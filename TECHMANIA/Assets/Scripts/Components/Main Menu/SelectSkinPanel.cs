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
    private int skinsBeingLoaded;
    private List<GameObject> vfxInstances;

    private void InitializeDropdown(TMP_Dropdown dropdown,
        string skinFolder, string skinStreamingFolder,
        string currentSkinName)
    {
        dropdown.options.Clear();
        HashSet<string> skinNames = new HashSet<string>();

        // Enumerate skins in the skin folder.
        try
        {
            foreach (string folder in
                Directory.EnumerateDirectories(skinFolder))
            {
                // folder does not end in directory separator.
                string skinName = Path.GetFileName(folder);
                skinNames.Add(skinName);
            }
        }
        catch (DirectoryNotFoundException)
        {
            // Silently ignore.
        }

        // Enumerate skins in the streaming assets folder.
        if (BetterStreamingAssets.DirectoryExists(
            Paths.RelativePathInStreamingAssets(skinStreamingFolder)))
        {
            foreach (string relativeFilename in
                BetterStreamingAssets.GetFiles(
                Paths.RelativePathInStreamingAssets(
                    skinStreamingFolder),
                Paths.kSkinFilename,
                SearchOption.AllDirectories))
            {
                string folder = Path.GetDirectoryName(
                    relativeFilename);
                string skinName = Path.GetFileName(folder);
                skinNames.Add(skinName);
            }
        }

        if (skinNames.Count == 0)
        {
            skinNames.Add(UIUtils.NoneOptionInDropdowns());
        }

        // Prepare the dropdown, and also find the index of the
        // current skin.
        int? currentSkinValue = null;
        int index = 0;
        foreach (string name in skinNames)
        {
            dropdown.options.Add(new TMP_Dropdown.OptionData(name));
            if (name == currentSkinName)
            {
                currentSkinValue = index;
            }
            index++;
        }

        if (currentSkinValue == null)
        {
            // This causes a reload
            dropdown.value = 0;
        }
        else
        {
            dropdown.SetValueWithoutNotify(currentSkinValue.Value);
        }

        dropdown.RefreshShownValue();
    }

    private void OnEnable()
    {
        Scan.InjectLaneHeight(120f);
        stopwatch = new System.Diagnostics.Stopwatch();
        vfxInstances = new List<GameObject>();
        skinsBeingLoaded = 0;
        RestartPreview();

        InitializeDropdown(noteSkinDropdown,
            Paths.GetNoteSkinRootFolder(), 
            Paths.GetStreamingNoteSkinRootFolder(), 
            Options.instance.noteSkin);
        InitializeDropdown(vfxSkinDropdown,
            Paths.GetVfxSkinRootFolder(), 
            Paths.GetStreamingVfxSkinRootFolder(), 
            Options.instance.vfxSkin);
        InitializeDropdown(comboSkinDropdown,
            Paths.GetComboSkinRootFolder(),
            Paths.GetStreamingComboSkinRootFolder(), 
            Options.instance.comboSkin);
        InitializeDropdown(gameUiSkinDropdown,
            Paths.GetGameUiSkinRootFolder(),
            Paths.GetStreamingGameUiSkinRootFolder(),
            Options.instance.gameUiSkin);
        reloadSkinsToggle.SetIsOnWithoutNotify(
            Options.instance.reloadSkinsWhenLoadingPattern);
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

        if (skinsBeingLoaded == 0 &&
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

            UIUtils.SetSpriteAndAspectRatio(scanlinePreview,
                GlobalResource.gameUiSkin
                .scanline.GetSpriteAtFloatIndex(beat * 0.25f));
            notePreview.sprite = GlobalResource.noteSkin.basic.
                GetSpriteAtFloatIndex(beat);
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
        if (skinsBeingLoaded == 0)
        {
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

        skinsBeingLoaded++;
    }

    private void RestartPreview()
    {
        skinsBeingLoaded--;

        if (skinsBeingLoaded <= 0)
        {
            skinsBeingLoaded = 0;

            // Note preview
            if (GlobalResource.noteSkin != null)
            {
                float noteScale = 
                    GlobalResource.noteSkin.basic.scale;
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
    }

    public void UIToMemory()
    {
        if (noteSkinDropdown.value <
            noteSkinDropdown.options.Count)
        {
            Options.instance.noteSkin = noteSkinDropdown.options[
                noteSkinDropdown.value].text;
        }
        if (vfxSkinDropdown.value <
            vfxSkinDropdown.options.Count)
        {
            Options.instance.vfxSkin = vfxSkinDropdown.options[
                vfxSkinDropdown.value].text;
        }
        if (comboSkinDropdown.value < 
            comboSkinDropdown.options.Count)
        {
            Options.instance.comboSkin = comboSkinDropdown.options[
                comboSkinDropdown.value].text;
        }
        if (gameUiSkinDropdown.value <
            gameUiSkinDropdown.options.Count)
        {
            Options.instance.gameUiSkin = gameUiSkinDropdown.options[
                gameUiSkinDropdown.value].text;
        }
        Options.instance.reloadSkinsWhenLoadingPattern =
            reloadSkinsToggle.isOn;
    }
}

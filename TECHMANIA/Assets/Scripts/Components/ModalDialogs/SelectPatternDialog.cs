using System.Collections;
using System.Collections.Generic;
using System.IO;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

public class SelectPatternDialog : ModalDialog
{
    private static SelectPatternDialog instance;
    private static SelectPatternDialog GetInstance()
    {
        if (instance == null)
        {
            instance = FindObjectOfType<Canvas>().GetComponentInChildren<SelectPatternDialog>(includeInactive: true);
        }
        return instance;
    }

    public static void Show()
    {
        GetInstance().InternalShow();
    }
    public static bool IsResolved()
    {
        return GetInstance().resolved;
    }

    public Image eyecatchImage;
    public Text titleText;
    public VerticalLayoutGroup patternList;
    public GameObject patternTemplate;
    public Toggle noFailToggle;
    public Toggle autoPlayToggle;
    public Button playButton;

    private Dictionary<GameObject, Pattern> objectToPattern;
    private GameObject selectedPatternObject;

    private void Update()
    {
        if (Input.GetKeyDown(KeyCode.Escape))
        {
            OnBackButtonClick();
        }
    }

    private void InternalShow()
    {
        resolved = false;
        gameObject.SetActive(true);

        // Show track metadata.
        Track track = GameSetup.track;
        string dir = new FileInfo(GameSetup.trackPath).DirectoryName;
        if (track.trackMetadata.eyecatchImage != UIUtils.kNone)
        {
            string eyecatchPath = dir + "\\" + track.trackMetadata.eyecatchImage;
            eyecatchImage.GetComponent<ImageSelfLoader>().LoadImage(
                eyecatchPath);
        }
        string textOnObject = $"<b>{track.trackMetadata.title}</b>";
        if (track.trackMetadata.genre != "")
        {
            textOnObject = $"<size=20>{track.trackMetadata.genre}</size>\n"
                + textOnObject;
        }
        if (track.trackMetadata.artist != "")
        {
            textOnObject = textOnObject + $"\n<size=20>{track.trackMetadata.artist}</size>";
        }
        titleText.text = textOnObject;

        // Remove all patterns from list, except for template.
        for (int i = 0; i < patternList.transform.childCount; i++)
        {
            GameObject o = patternList.transform.GetChild(i).gameObject;
            if (o == patternTemplate) continue;
            Destroy(o);
        }

        // Rebuild pattern list.
        objectToPattern = new Dictionary<GameObject, Pattern>();
        selectedPatternObject = null;
        foreach (Pattern p in track.patterns)
        {
            // Instantiate pattern representation.
            GameObject patternObject = Instantiate(patternTemplate, patternList.transform);
            patternObject.name = "Pattern";
            string displayName = $"{p.patternMetadata.controlScheme} / " +
                $"{p.patternMetadata.level} / " +
                $"{p.patternMetadata.patternName}";
            patternObject.GetComponentInChildren<Text>().text = displayName;
            patternObject.SetActive(true);

            // Record mapping.
            objectToPattern.Add(patternObject, p);

            // Bind click event.
            patternObject.GetComponent<Button>().onClick.AddListener(() =>
            {
                OnPatternObjectClick(patternObject);
            });
        }

        // Other UI elements.
        noFailToggle.SetIsOnWithoutNotify(GameSetup.noFail);
        autoPlayToggle.SetIsOnWithoutNotify(GameSetup.autoPlay);
        RefreshPlayButton();
    }

    private void RefreshPlayButton()
    {
        playButton.interactable = selectedPatternObject != null;
    }
    
    private void OnPatternObjectClick(GameObject o)
    {
        if (selectedPatternObject != null)
        {
            selectedPatternObject.transform.Find("Selection").gameObject.SetActive(false);
        }
        if (!objectToPattern.ContainsKey(o))
        {
            selectedPatternObject = null;
        }
        else
        {
            selectedPatternObject = o;
            selectedPatternObject.transform.Find("Selection").gameObject.SetActive(true);
        }
        RefreshPlayButton();
    }

    public void OnBackButtonClick()
    {
        resolved = true;
        gameObject.SetActive(false);
    }

    public void OnPlayButtonClick()
    {
        GameSetup.pattern = objectToPattern[selectedPatternObject];
        SceneManager.LoadScene("Game");
    }

    public void OnNoFailToggleChange()
    {
        GameSetup.noFail = noFailToggle.isOn;
    }

    public void OnAutoPlayToggleChange()
    {
        GameSetup.autoPlay = autoPlayToggle.isOn;
    }
}

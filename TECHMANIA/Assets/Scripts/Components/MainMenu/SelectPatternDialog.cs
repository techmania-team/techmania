using System.Collections;
using System.Collections.Generic;
using System.IO;
using TMPro;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

public class SelectPatternDialog : MonoBehaviour
{
    public EyecatchSelfLoader eyecatchImage;
    public TextMeshProUGUI genreText;
    public TextMeshProUGUI titleText;
    public TextMeshProUGUI artistText;
    public VerticalLayoutGroup patternList;
    public GameObject patternTemplate;
    public GameObject noPatternText;
    public Button backButton;
    public Button playButton;

    private Dictionary<GameObject, Pattern> objectToPattern;
    private GameObject selectedPatternObject;

    public void Show()
    {
        GetComponent<Dialog>().FadeIn();

        // Show track metadata.
        Track track = GameSetup.track;
        string dir = new FileInfo(GameSetup.trackPath).DirectoryName;
        eyecatchImage.LoadImage(dir, track.trackMetadata);
        genreText.text = track.trackMetadata.genre;
        titleText.text = track.trackMetadata.title;
        artistText.text = track.trackMetadata.artist;

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
        GameObject firstObject = null;
        foreach (Pattern p in track.patterns)
        {
            // Instantiate pattern representation.
            GameObject patternObject = Instantiate(patternTemplate, patternList.transform);
            patternObject.name = "Pattern Radio Button";
            patternObject.GetComponent<PatternRadioButton>().Initialize(
                p.patternMetadata);
            patternObject.SetActive(true);
            if (firstObject == null)
            {
                firstObject = patternObject;
                EventSystem.current.SetSelectedGameObject(firstObject);
            }

            // Record mapping.
            objectToPattern.Add(patternObject, p);

            // Bind click event.
            patternObject.GetComponent<Button>().onClick.AddListener(() =>
            {
                OnPatternObjectClick(patternObject);
            });
        }

        // Other UI elements.
        RefreshPlayButton();
        noPatternText.SetActive(objectToPattern.Count == 0);
        if (firstObject == null)
        {
            EventSystem.current.SetSelectedGameObject(backButton.gameObject);
        }
    }

    private void RefreshPlayButton()
    {
        playButton.interactable = selectedPatternObject != null;
    }
    
    private void OnPatternObjectClick(GameObject o)
    {
        if (selectedPatternObject != null)
        {
            selectedPatternObject.GetComponent<MaterialRadioButton>().SetIsOn(false);
        }
        if (!objectToPattern.ContainsKey(o))
        {
            selectedPatternObject = null;
        }
        else
        {
            selectedPatternObject = o;
            selectedPatternObject.GetComponent<MaterialRadioButton>().SetIsOn(true);
        }
        RefreshPlayButton();
    }

    public void OnPlayButtonClick()
    {
        GameSetup.pattern = objectToPattern[selectedPatternObject];
        Curtain.DrawCurtainThenGoToScene("Game");
    }
}

using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Events;
using UnityEngine.EventSystems;
using UnityEngine.UI;

public class PatternRadioList : MonoBehaviour
{
    public VerticalLayoutGroup list;
    public GameObject patternTemplate;
    public GameObject noPatternText;

    private Dictionary<GameObject, PatternV1> objectToPattern;
    private GameObject selectedPatternObject;
    public static event UnityAction<PatternV1> SelectedPatternChanged;

    public GameObject InitializeAndReturnFirstPatternObject(TrackV1 t,
        PatternV1 initialSelectedPattern = null)
    {
        // Remove all patterns from list, except for template.
        for (int i = 0; i < list.transform.childCount; i++)
        {
            GameObject o = list.transform.GetChild(i).gameObject;
            if (o == patternTemplate) continue;
            Destroy(o);
        }

        // Rebuild pattern list.
        objectToPattern = new Dictionary<GameObject, PatternV1>();
        selectedPatternObject = null;
        GameObject firstObject = null;
        foreach (PatternV1 p in t.patterns)
        {
            // Instantiate pattern representation.
            GameObject patternObject = Instantiate(patternTemplate, list.transform);
            patternObject.name = "Pattern Radio Button";
            patternObject.GetComponentInChildren<PatternBanner>().Initialize(
                p.patternMetadata);
            patternObject.SetActive(true);
            if (firstObject == null)
            {
                firstObject = patternObject;
            }
            if (p == initialSelectedPattern)
            {
                selectedPatternObject = patternObject;
            }
            patternObject.GetComponentInChildren<MaterialRadioButton>().
                SetIsOn(p == initialSelectedPattern);

            // Record mapping.
            objectToPattern.Add(patternObject, p);

            // Bind click event.
            patternObject.GetComponent<Button>().onClick.AddListener(() =>
            {
                OnPatternObjectClick(patternObject);
            });
        }

        noPatternText.SetActive(objectToPattern.Count == 0);
        return firstObject;
    }

    public PatternV1 GetSelectedPattern()
    {
        if (selectedPatternObject == null)
        {
            return null;
        }
        return objectToPattern[selectedPatternObject];
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
        SelectedPatternChanged?.Invoke(GetSelectedPattern());
    }
}

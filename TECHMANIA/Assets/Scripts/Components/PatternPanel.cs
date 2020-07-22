using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class PatternPanel : MonoBehaviour
{
    public Transform patternContainer;
    public GameObject markerTemplate;
    public GameObject lineTemplate;
    public GameObject dottedLineTemplate;

    // Start is called before the first frame update
    void Start()
    {
        // This sets the top-left
        GameObject marker = Instantiate(markerTemplate, patternContainer);
        marker.SetActive(true);
        marker.transform.localPosition = new Vector3(0f, 0f, 0f);

        GameObject line = Instantiate(lineTemplate, patternContainer);
        line.SetActive(true);
        line.transform.localPosition = new Vector3(1000f, 0f, 0f);

        markerTemplate.transform.localPosition = new Vector3(0f, 0f, 0f);
        lineTemplate.transform.localPosition = new Vector3(2000f, 0f, 0f);
        dottedLineTemplate.transform.localPosition = new Vector3(4000f, 0f, 0f);
    }

    // Update is called once per frame
    void Update()
    {
        
    }
}

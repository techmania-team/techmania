using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

public class TouchClickFeedbackManager : MonoBehaviour
{
    public Game game;
    public GameObject template;
    public Material additiveMaterial;

    // In Keys, reuse lane number as finger#.
    private Dictionary<int, GameObject> fingerToFeedback;
    private List<int> numKeysHeldOnLane;

    // Start is called before the first frame update
    void Start()
    {
        fingerToFeedback = new Dictionary<int, GameObject>();
        numKeysHeldOnLane = new List<int>();
        for (int i = 0; i < Game.playableLanes; i++)
        {
            numKeysHeldOnLane.Add(0);
        }
    }

    // Update is called once per frame
    void Update()
    {
        if (Game.autoPlay)
        {
            if (fingerToFeedback.Count > 0)
            {
                DestroyAllFeedback();
            }
            return;
        }
        switch (InternalGameSetup.patternAfterModifier.patternMetadata.controlScheme)
        {
            case ControlScheme.Touch:
                for (int i = 0; i < Input.touchCount; i++)
                {
                    Touch t = Input.GetTouch(i);
                    switch (t.phase)
                    {
                        case TouchPhase.Began:
                            SpawnFeedback(t.fingerId, t.position);
                            break;
                        case TouchPhase.Moved:
                        case TouchPhase.Stationary:
                            MoveFeedback(t.fingerId, t.position);
                            break;
                        case TouchPhase.Canceled:
                        case TouchPhase.Ended:
                            DestroyFeedback(t.fingerId);
                            break;
                    }
                }
                break;
            case ControlScheme.Keys:
                for (int i = 0; i < Game.playableLanes; i++)
                {
                    if (Game.keysForLane == null ||
                        Game.keysForLane[i] == null) continue;
                    foreach (KeyCode c in Game.keysForLane[i])
                    {
                        if (Input.GetKeyDown(c))
                        {
                            numKeysHeldOnLane[i]++;
                        }
                        if (Input.GetKeyUp(c))
                        {
                            numKeysHeldOnLane[i]--;
                        }
                    }
                    if (numKeysHeldOnLane[i] > 0)
                    {
                        MoveFeedback(i, game
                            .GetScreenPositionOnCurrentScanline(i));
                    }
                    else
                    {
                        DestroyFeedback(i);
                    }
                }
                break;
            case ControlScheme.KM:
                if (Input.GetMouseButtonDown(0))
                {
                    SpawnFeedback(0, Input.mousePosition);
                }
                else if (Input.GetMouseButtonUp(0))
                {
                    DestroyFeedback(0);
                }
                else if (Input.GetMouseButton(0))
                {
                    MoveFeedback(0, Input.mousePosition);
                }
                break;
        }
    }

    private Vector2 TouchPositionToAnchoredPosition(
        Vector2 touchPosition)
    {
        Vector2 anchoredPosition = Vector2.zero;
        RectTransformUtility.ScreenPointToLocalPointInRectangle(
            GetComponent<RectTransform>(),
            touchPosition,
            null,
            out anchoredPosition);
        return anchoredPosition;
    }

    private void SpawnFeedback(int fingerId, Vector2 position)
    {
        if (fingerToFeedback.ContainsKey(fingerId)) return;

        float size = GlobalResource.gameUiSkin.touchClickFeedbackSize;
        GameObject feedback = Instantiate(template, transform);
        feedback.SetActive(true);
        RectTransform rect = feedback.GetComponent<RectTransform>();
        rect.sizeDelta = new Vector2(size, size);
        rect.anchoredPosition = TouchPositionToAnchoredPosition(
            position);
        if (GlobalResource.gameUiSkin.touchClickFeedback
            .additiveShader)
        {
            feedback.GetComponent<Image>().material = additiveMaterial;
        }
        fingerToFeedback.Add(fingerId, feedback);
    }

    private void MoveFeedback(int fingerId, Vector2 position)
    {
        if (!fingerToFeedback.ContainsKey(fingerId))
        {
            SpawnFeedback(fingerId, position);
            return;
        }
        fingerToFeedback[fingerId].GetComponent<RectTransform>()
            .anchoredPosition =
            TouchPositionToAnchoredPosition(position);
    }

    private void DestroyFeedback(int fingerId)
    {
        if (!fingerToFeedback.ContainsKey(fingerId)) return;
        Destroy(fingerToFeedback[fingerId]);
        fingerToFeedback.Remove(fingerId);
    }

    private void DestroyAllFeedback()
    {
        if (fingerToFeedback == null) return;
        foreach (GameObject o in fingerToFeedback.Values)
        {
            Destroy(o);
        }
        fingerToFeedback.Clear();
    }

    private void OnDisable()
    {
        foreach (GameObject o in fingerToFeedback.Values)
        {
            Destroy(o);
        }
        fingerToFeedback.Clear();
    }
}

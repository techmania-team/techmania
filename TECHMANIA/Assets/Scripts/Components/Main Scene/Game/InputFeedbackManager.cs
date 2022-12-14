using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UIElements;

public class InputFeedbackManager
{
    private class InputFeedback
    {
        private TemplateContainer templateContainer;
        private VisualElement element;
        private float spawnTime;

        public InputFeedback(TemplateContainer templateContainer)
        {
            this.templateContainer = templateContainer;
            this.templateContainer.AddToClassList(
                "input-feedback-anchor");
            element = templateContainer.Q("input-feedback");
            spawnTime = Time.time;
        }

        public void ResetSize()
        {
            element.style.width = GlobalResource.gameUiSkin
                .touchClickFeedbackSize;
            element.style.height = GlobalResource.gameUiSkin
                .touchClickFeedbackSize;
        }

        public void SetPosition(Vector2 position)
        {
            templateContainer.style.left = position.x;
            templateContainer.style.top = position.y;
        }

        public void Dispose()
        {
            templateContainer.RemoveFromHierarchy();
        }

        public void UpdateSprite()
        {
            float timeSinceSpawn = Time.time - spawnTime;
            element.style.backgroundImage = new
                StyleBackground(
                GlobalResource.gameUiSkin.touchClickFeedback
                .GetSpriteForTime(timeSinceSpawn, loop: true));
        }
    }

    private VisualElement feedbackContainer;
    private VisualTreeAsset feedbackTemplate;
    private GameInputManager inputManager;

    private ControlScheme controlScheme;
    private int lanes;

    // For Touch and KM, ID is the finger ID.
    // For Keys, ID is the lane number.
    private Dictionary<int, InputFeedback> idToFeedback;
    private const int kMouseId = 0;
    private List<int> numKeysHeldOnLane;
    private int numMouseButtonsHeld;

    public InputFeedbackManager(VisualElement feedbackContainer,
        VisualTreeAsset feedbackTemplate,
        GameInputManager inputManager)
    {
        this.feedbackContainer = feedbackContainer;
        this.feedbackTemplate = feedbackTemplate;
        this.inputManager = inputManager;
    }

    public void Prepare(PatternMetadata metadata)
    {
        controlScheme = metadata.controlScheme;
        lanes = metadata.playableLanes;

        idToFeedback = new Dictionary<int, InputFeedback>();
        numKeysHeldOnLane = new List<int>();
        for (int i = 0; i < lanes; i++) numKeysHeldOnLane.Add(0);
        numMouseButtonsHeld = 0;
    }

    #region Update
    public void Update()
    {
        UpdateWithInput();
        UpdateExistingFeedbacks();
    }

    private void UpdateWithInput()
    {
        if (GameController.autoPlay)
        {
            foreach (InputFeedback feedback in idToFeedback.Values)
            {
                feedback.Dispose();
            }
            idToFeedback.Clear();
            return;
        }

        switch (controlScheme)
        {
            case ControlScheme.Touch:
                for (int i = 0; i < Input.touchCount; i++)
                {
                    Touch t = Input.GetTouch(i);
                    Vector2 position = ThemeApi.VisualElementTransform
                        .ScreenSpaceToElementLocalSpace(
                        feedbackContainer, t.position);
                    switch (t.phase)
                    {
                        case TouchPhase.Began:
                            SpawnFeedback(t.fingerId, position);
                            break;
                        case TouchPhase.Moved:
                            MoveFeedback(t.fingerId, position);
                            break;
                        case TouchPhase.Canceled:
                        case TouchPhase.Ended:
                            DisposeFeedback(t.fingerId);
                            break;
                    }
                }
                break;
            case ControlScheme.Keys:
                // TODO
                break;
            case ControlScheme.KM:
                {
                    Vector2 position = ThemeApi.VisualElementTransform
                        .ScreenSpaceToElementLocalSpace(
                        feedbackContainer, Input.mousePosition);
                    int oldButtonsHeld = numMouseButtonsHeld;
                    for (int i = 0; i < 2; i++)
                    {
                        if (Input.GetMouseButtonDown(i))
                        {
                            numMouseButtonsHeld++;
                        }
                        if (Input.GetMouseButtonUp(i))
                        {
                            numMouseButtonsHeld--;
                        }
                    }
                    
                    if (oldButtonsHeld <= 0 &&
                        numMouseButtonsHeld > 0)
                    {
                        SpawnFeedback(kMouseId, position);
                    }
                    else if (oldButtonsHeld > 0 &&
                        numMouseButtonsHeld <= 0)
                    {
                        DisposeFeedback(kMouseId);
                    }
                    else if (numMouseButtonsHeld > 0)
                    {
                        MoveFeedback(kMouseId, position);
                    }
                }
                break;
        }
    }

    private void UpdateExistingFeedbacks()
    {
        foreach (InputFeedback feedback in idToFeedback.Values)
        {
            feedback.UpdateSprite();
        }
    }
    #endregion

    #region Spawn, move, dispose
    private void SpawnFeedback(int id, Vector2 position)
    {
        TemplateContainer templateContainer = feedbackTemplate
            .Instantiate();
        feedbackContainer.Add(templateContainer);
        InputFeedback feedback = new InputFeedback(templateContainer);
        feedback.ResetSize();
        feedback.SetPosition(position);
        idToFeedback.Add(id, feedback);
    }

    private void MoveFeedback(int id, Vector2 position)
    {
        idToFeedback[id].SetPosition(position);
    }

    private void DisposeFeedback(int id)
    {
        idToFeedback[id].Dispose();
        idToFeedback.Remove(id);
    }
    #endregion
}

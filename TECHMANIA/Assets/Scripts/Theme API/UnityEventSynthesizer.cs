using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using MoonSharp.Interpreter;
using UnityEngine.UIElements;
using System;

namespace ThemeApi
{
    // This component receives MonoBehaviour-based events,
    // synthesizes UI Toolkit events and sends them out to
    // registered listeners.
    public class UnityEventSynthesizer : MonoBehaviour
    {
        private static Dictionary<Type, HashSet<VisualElement>>
            eventListeners;

        public static void Prepare()
        {
            eventListeners =
                new Dictionary<Type, HashSet<VisualElement>>();
            eventListeners.Add(
                typeof(FrameUpdateEvent),
                new HashSet<VisualElement>());
            eventListeners.Add(
                typeof(ApplicationFocusEvent),
                new HashSet<VisualElement>());
        }

        #region Add/Remove Listeners
        public static void AddListener<EventType>(
            VisualElement element)
        {
            eventListeners[typeof(EventType)].Add(element);
        }

        public static void RemoveListener<EventType>(
            VisualElement element)
        {
            // No exception if the element doesn't exist in the HashSet.
            eventListeners[typeof(EventType)].Remove(element);
        }

        public static void RemoveListenerForAllEventTypes(
            VisualElement element)
        {
            RemoveListener<FrameUpdateEvent>(element);
            RemoveListener<ApplicationFocusEvent>(element);
        }
        #endregion

        #region Event synthesizing
        private static bool HasListeners<EventType>()
        {
            if (eventListeners == null) return false;
            return eventListeners[typeof(EventType)].Count > 0;
        }

        void Update()
        {
            // TODO: GC.Collect causes lag spikes. Why?
            if (!HasListeners<FrameUpdateEvent>()) return;
            if (Time.frameCount % 60 == 0)
            {
                Debug.Log("FrameUpdateEvent listeners: " +
                    eventListeners[typeof(FrameUpdateEvent)].Count);
            }
            foreach (VisualElement element in
                eventListeners[typeof(FrameUpdateEvent)])
            {
                using (FrameUpdateEvent e = FrameUpdateEvent.GetPooled())
                {
                    e.target = element;
                    element.SendEvent(e);
                }
            }
        }

        private void OnApplicationFocus(bool focus)
        {
            if (!HasListeners<ApplicationFocusEvent>()) return;
            foreach (VisualElement element in
                eventListeners[typeof(ApplicationFocusEvent)])
            {
                using (ApplicationFocusEvent e =
                    ApplicationFocusEvent.GetPooled())
                {
                    e.target = element;
                    element.SendEvent(e);
                }
            }
        }
        #endregion
    }

    [MoonSharpUserData]
    public class FrameUpdateEvent :
        EventBase<FrameUpdateEvent>
    { }

    [MoonSharpUserData]
    public class ApplicationFocusEvent : 
        EventBase<ApplicationFocusEvent>
    {
        public bool focus;
    }
}
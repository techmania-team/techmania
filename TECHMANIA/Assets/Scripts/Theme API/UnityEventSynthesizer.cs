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
            eventListeners[typeof(EventType)].Remove(element);
        }
        #endregion

        #region Event synthesizing
        private static bool HasListeners<EventType>()
        {
            if (eventListeners == null) return false;
            return eventListeners[typeof(EventType)].Count > 0;
        }

        private static void RunCallbacks(EventBase eventObject)
        {
            foreach (VisualElement e in
                eventListeners[eventObject.GetType()])
            {
                if (!e.enabledInHierarchy) continue;
                eventObject.target = e;
                e.SendEvent(eventObject);
            }
        }

        void Update()
        {
            if (!HasListeners<FrameUpdateEvent>()) return;
            using (FrameUpdateEvent e = FrameUpdateEvent.GetPooled())
                RunCallbacks(e);
        }

        private void OnApplicationFocus(bool focus)
        {
            if (!HasListeners<ApplicationFocusEvent>()) return;
            using (ApplicationFocusEvent e = 
                ApplicationFocusEvent.GetPooled())
            {
                e.focus = focus;
                RunCallbacks(e);
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
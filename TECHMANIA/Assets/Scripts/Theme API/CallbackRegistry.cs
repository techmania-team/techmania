using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using MoonSharp.Interpreter;
using UnityEngine.UIElements;
using System;

namespace ThemeApi
{
    // This class works around UI Toolkit's limitation that there
    // can be no more than 1 callback for each element-type
    // combination.
    //
    // This class is not exposed to Lua.
    public class CallbackRegistry
    {
        // Type is event type.
        private static Dictionary<
            Tuple<VisualElement, Type>,
            HashSet<DynValue>> 
            callbacks;
        private static Dictionary<
            Tuple<VisualElement, Type>,
            // Tuple content is callback and data.
            HashSet<Tuple<DynValue, DynValue>>>
            callbacksWithData;

        public static void Prepare()
        {
            callbacks = new Dictionary<
                Tuple<VisualElement, Type>, HashSet<DynValue>>();
            callbacksWithData = new Dictionary<
                Tuple<VisualElement, Type>,
                HashSet<Tuple<DynValue, DynValue>>>();
        }

        private static void CheckCallback<TEventType>
            (Tuple<VisualElement, Type> key)
            where TEventType : EventBase<TEventType>, new()
        {
            if (callbacks.ContainsKey(key) ||
                callbacksWithData.ContainsKey(key)) return;

            callbacks.Add(key, new HashSet<DynValue>());
            callbacksWithData.Add(key,
                new HashSet<Tuple<DynValue, DynValue>>());
            key.Item1.RegisterCallback((TEventType e) =>
            {
                foreach (DynValue c in callbacks[key])
                {
                    c.Function.Call(
                        new VisualElementWrap(key.Item1),
                        e);
                }
                foreach (Tuple<DynValue, DynValue> tuple
                    in callbacksWithData[key])
                {
                    tuple.Item1.Function.Call(
                        new VisualElementWrap(key.Item1), 
                        tuple.Item2,
                        e);
                }
            });
        }

        // Callback parameters: element, event.
        public static void AddCallback<TEventType>(
            VisualElement element, DynValue callback)
            where TEventType : EventBase<TEventType>, new()
        {
            Tuple<VisualElement, Type> key =
                new Tuple<VisualElement, Type>(
                    element, typeof(TEventType));
            CheckCallback<TEventType>(key);
            callbacks[key].Add(callback);
        }

        // Callback parameters: element, data, event.
        public static void AddCallbackWithData<TEventType>(
            VisualElement element, DynValue callback,
            DynValue data)
            where TEventType : EventBase<TEventType>, new()
        {
            Tuple<VisualElement, Type> key =
                new Tuple<VisualElement, Type>(
                    element, typeof(TEventType));
            CheckCallback<TEventType>(key);
            callbacksWithData[key].Add(
                new Tuple<DynValue, DynValue>(callback, data));
        }
    }
}
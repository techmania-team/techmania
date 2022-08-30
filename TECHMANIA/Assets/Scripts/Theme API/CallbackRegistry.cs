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
        // Key: <VisualElement, event type>
        // Value: <Callback, data>
        // For callbacks without data, the data element is of
        // type Void.
        private static Dictionary<
            Tuple<VisualElement, Type>,
            HashSet<Tuple<DynValue, DynValue>>>
            callbacks;

        public static void Prepare()
        {
            callbacks = new Dictionary<
                Tuple<VisualElement, Type>,
                HashSet<Tuple<DynValue, DynValue>>>();
        }

        private static void CheckCallback<TEventType>
            (Tuple<VisualElement, Type> key)
            where TEventType : EventBase<TEventType>, new()
        {
            if (callbacks.ContainsKey(key)) return;

            callbacks.Add(key,
                new HashSet<Tuple<DynValue, DynValue>>());
            key.Item1.RegisterCallback((TEventType e) =>
            {
                foreach (Tuple<DynValue, DynValue> tuple
                    in callbacks[key])
                {
                    tuple.Item1.Function.Call(
                        new VisualElementWrap(key.Item1),
                        e,
                        tuple.Item2);
                }
            });
        }

        // Callback parameters: element, event, data.
        public static void AddCallback<TEventType>(
            VisualElement element, DynValue callback,
            DynValue data)
            where TEventType : EventBase<TEventType>, new()
        {
            Tuple<VisualElement, Type> key =
                new Tuple<VisualElement, Type>(
                    element, typeof(TEventType));
            CheckCallback<TEventType>(key);
            callbacks[key].Add(
                new Tuple<DynValue, DynValue>(callback, data));
        }

        public static void RemoveCallback<TEventType>(
            VisualElement element, DynValue callback)
            where TEventType : EventBase<TEventType>, new()
        {
            Tuple<VisualElement, Type> key =
                new Tuple<VisualElement, Type>(
                    element, typeof(TEventType));
            if (!callbacks.ContainsKey(key)) return;
            HashSet<Tuple<DynValue, DynValue>> remainingCallbacks
                = new HashSet<Tuple<DynValue, DynValue>>();
            foreach (Tuple<DynValue, DynValue> tuple in
                callbacks[key])
            {
                if (tuple.Item1.Function.EntryPointByteCodeLocation
                    != callback.Function.EntryPointByteCodeLocation)
                {
                    remainingCallbacks.Add(tuple);
                }
            }
            callbacks[key] = remainingCallbacks;
        }

        public static void RemoveAllCallbackOfTypeOn<TEventType>(
            VisualElement element)
            where TEventType : EventBase<TEventType>, new()
        {
            Tuple<VisualElement, Type> key =
                new Tuple<VisualElement, Type>(
                    element, typeof(TEventType));
            callbacks.Remove(key);  // No exception if key not found
        }

        // Does not unregister the callbacks on VisualElement.
        public static void RemoveAllCallbackOn(
            VisualElement element)
        {
            Dictionary<Tuple<VisualElement, Type>,
                HashSet<Tuple<DynValue, DynValue>>>
                remainingCallbacks = new Dictionary<
                    Tuple<VisualElement, Type>, 
                    HashSet<Tuple<DynValue, DynValue>>>();
            foreach (var pair in callbacks)
            {
                if (pair.Key.Item1 == element) continue;
                remainingCallbacks.Add(pair.Key, pair.Value);
            }
            callbacks = remainingCallbacks;
        }
    }
}
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class LocaleSwitcher : MonoBehaviour
{
    // Update is called once per frame
    void Update()
    {
#if UNITY_EDITOR
        if (Input.GetKey(KeyCode.LeftShift) &&
            Input.GetKeyDown(KeyCode.Alpha1))
        {
            Locale.SetLocale("en");
        }
        if (Input.GetKey(KeyCode.LeftShift) &&
            Input.GetKeyDown(KeyCode.Alpha2))
        {
            Locale.SetLocale("zh-Hans");
        }
        if (Input.GetKey(KeyCode.LeftShift) &&
            Input.GetKeyDown(KeyCode.Alpha3))
        {
            Locale.SetLocale("zh-Hant");
        }
        if (Input.GetKey(KeyCode.LeftShift) &&
            Input.GetKeyDown(KeyCode.Alpha4))
        {
            Locale.SetLocale("ja");
        }
        if (Input.GetKey(KeyCode.LeftShift) &&
            Input.GetKeyDown(KeyCode.Alpha5))
        {
            Locale.SetLocale("ko");
        }
#endif
    }
}

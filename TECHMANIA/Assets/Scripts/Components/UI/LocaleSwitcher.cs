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
            L10n.SetLocale("en", L10n.Instance.System);
        }
        if (Input.GetKey(KeyCode.LeftShift) &&
            Input.GetKeyDown(KeyCode.Alpha2))
        {
            L10n.SetLocale("zh-Hans", L10n.Instance.System);
        }
        if (Input.GetKey(KeyCode.LeftShift) &&
            Input.GetKeyDown(KeyCode.Alpha3))
        {
            L10n.SetLocale("zh-Hant", L10n.Instance.System);
        }
        if (Input.GetKey(KeyCode.LeftShift) &&
            Input.GetKeyDown(KeyCode.Alpha4))
        {
            L10n.SetLocale("ja", L10n.Instance.System);
        }
        if (Input.GetKey(KeyCode.LeftShift) &&
            Input.GetKeyDown(KeyCode.Alpha5))
        {
            L10n.SetLocale("ko", L10n.Instance.System);
        }
#endif
    }
}

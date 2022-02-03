using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class HideOnAndroid : MonoBehaviour
{
    // Start is called before the first frame update
    void Start()
    {
#if UNITY_ANDROID
        gameObject.SetActive(false);
#else
        gameObject.SetActive(true);
#endif
    }
}

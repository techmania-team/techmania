using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class HideOnIOS : MonoBehaviour
{
    // Start is called before the first frame update
    void Start()
    {
#if UNITY_IOS
        gameObject.SetActive(false);
#else
        gameObject.SetActive(true);
#endif
    }
}

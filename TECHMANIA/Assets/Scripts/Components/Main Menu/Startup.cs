using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class Startup : MonoBehaviour
{
    // Start is called before the first frame update
    void Start()
    {
        Input.simulateMouseWithTouches = false;
        Paths.PrepareFolders();
        OptionsPanel.ApplyOptionsOnStartUp();
        SpriteSheet.PrepareEmptySpriteSheet();
        GetComponent<GlobalResourceLoader>().StartLoading();
    }
}

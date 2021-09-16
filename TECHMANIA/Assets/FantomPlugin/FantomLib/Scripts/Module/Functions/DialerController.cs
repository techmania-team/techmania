using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using UnityEngine.Events;

namespace FantomLib
{
    /// <summary>
    /// Dialer Controller
    /// </summary>
    public class DialerController : MonoBehaviour
    {
        //Inspector Settings
        public string phoneNumber = "0123456789";    //not necessary: '-' (will be removed)


        // Use this for initialization
        private void Start()
        {

        }

        // Update is called once per frame
        //private void Update()
        //{

        //}


        //Show Dialer with local number
        public void Show()
        {
            phoneNumber = phoneNumber.Replace("-", "");
            string uri = "tel:" + phoneNumber;
#if UNITY_EDITOR
            Debug.Log(name + ".Show : uri = " + uri);
#elif UNITY_ANDROID
            AndroidPlugin.StartActionURI("android.intent.action.DIAL", uri);
#endif
        }

        //Set phoneNumber dynamically and show Dialer (current phoneNumber will be overwritten)
        public void Show(string phoneNumber)
        {
            this.phoneNumber = phoneNumber;
            Show();
        }
    }
}

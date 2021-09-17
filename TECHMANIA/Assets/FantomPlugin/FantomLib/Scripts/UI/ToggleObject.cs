using System;
using UnityEngine;
using UnityEngine.Events;

namespace FantomLib
{
    /// <summary>
    /// Switch the display of the some objects.
    ///·Mainly used for on / off switching.
    /// 
    /// いくつかのオブジェクトの表示を切り替える
    ///・主にオン/オフ切り替えなどに用いる。
    /// </summary>
    public class ToggleObject : MonoBehaviour {

        [Serializable]
        public enum ToggleType
        {
            OnOff,      //onObject or offObject
            Index,      //objects
        }
        [SerializeField] protected ToggleType toggleType = ToggleType.OnOff;

        [SerializeField] protected bool isOn = true;     //Toggle visible on/off

        public GameObject onObject;                 //Object to display when isOn = true
        public GameObject offObject;                //Object to display when isOn = false
        
        [SerializeField] protected int index = 0;   //Index of the displayed object
        public GameObject[] objects;                //Object to display with index

        //Callbacks
        [Serializable] public class ToggleHandler : UnityEvent<bool> { }  //isOn
        public ToggleHandler OnToggleChanged;

        [Serializable] public class ToggleIndexHandler : UnityEvent<int> { }  //indx
        public ToggleIndexHandler OnToggleIndexChanged;

#region Properties and Local values Section

        //Set On/Off
        //･If there is a change, 'OnToggleChanged' event callback will occur.
        public bool IsOn {
            get { return isOn; }
            set {
                if (isOn != value)
                {
                    isOn = value;
                    UpdateVisible();

                    if (OnToggleChanged != null)
                        OnToggleChanged.Invoke(isOn);
                }
            }
        }

        //Set index
        //･If there is a change, 'OnToggleIndexChanged' event callback will occur.
        public int Index {
            get { return index; }
            set {
                if (index != value)
                {
                    index = value;
                    UpdateVisible();

                    if (OnToggleIndexChanged != null)
                        OnToggleIndexChanged.Invoke(index);
                }
            }
        }


        //Flip On/Off or next Index
        //･'OnToggleChanged' event callback will occur.
        public void Toggle()
        {
            switch (toggleType)
            {
                case ToggleType.OnOff:
                    IsOn = !IsOn;
                    break;
                case ToggleType.Index:
                    Index = (int)Mathf.Repeat(index + 1, objects.Length);
                    break;
            }
        }


        //Update the UI visible status.
        protected virtual void UpdateVisible()
        {
            switch (toggleType)
            {
                case ToggleType.OnOff:
                    SetOnObjectVisible(isOn);
                    SetOffObjectVisible(!isOn);
                    break;
                case ToggleType.Index:
                    SetObjectsVisible(index);
                    break;
            }
        }

        //'ON object' On/Off
        protected virtual void SetOnObjectVisible(bool visible)
        {
            if (onObject != null)
                onObject.SetActive(visible);
        }

        //'OFF object' On/Off
        protected virtual void SetOffObjectVisible(bool visible)
        {
            if (offObject != null)
                offObject.SetActive(visible);
        }

        //'objects' visibility with index
        protected virtual void SetObjectsVisible(int idx)
        {
            if (objects == null || objects.Length == 0)
                return;

            for (int i = 0; i < objects.Length; i++)
                objects[i].SetActive(i == index);
        }

#endregion

        // Use this for initialization
        protected void Start()
        {
            UpdateVisible();
        }

        // Update is called once per frame
        //protected void Update()
        //{

        //}

    }
}

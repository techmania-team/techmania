using UnityEngine.UI;

namespace FantomLib
{
    /// <summary>
    /// Switch the display of the some buttons.
    ///·Mainly used for on / off switching.
    /// 
    /// いくつかのボタンの表示を切り替える
    ///・主にオン/オフ切り替えなどに用いる。
    /// </summary>
    public class ToggleButton : ToggleObject {

        public Button targetButton;         //Mainly switch images of targetGraphic

        public Image onImage;               //Object to display when isOn = true
        public Image offImage;              //Object to display when isOn = false

        public Image[] images;              //Object to display with index

#region Properties and Local values Section

        //'ON object' On/Off
        protected override void SetOnObjectVisible(bool visible)
        {
            if (visible && targetButton != null && onImage != null)
                targetButton.targetGraphic = onImage;

            base.SetOnObjectVisible(visible);
        }

        //'OFF object' On/Off
        protected override void SetOffObjectVisible(bool visible)
        {
            if (visible && targetButton != null && offImage != null)
                targetButton.targetGraphic = offImage;

            base.SetOffObjectVisible(visible);
        }

        //'objects' visibility with index
        protected override void SetObjectsVisible(int idx)
        {
            if (targetButton != null && images != null 
                && idx < images.Length && images[idx] != null)
            {
                targetButton.targetGraphic = images[idx];
            }

            base.SetObjectsVisible(idx);
        }

#endregion

        // Use this for initialization
        protected new void Start () {
            base.Start();
        }

    }
}

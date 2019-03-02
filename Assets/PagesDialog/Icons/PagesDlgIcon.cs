using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using BaroqueUI;
using UnityEngine.UI;


namespace MyTest
{
    public abstract class PageDlg : MonoBehaviour
    {
        public abstract void Activate();
    }

    public class PagesDlgIcon : MonoBehaviour
    {
        public Transform scaleIcon, scaleGrayIcon;
        public bool pageActive;
        public PageDlg panel;
        public float hoveringFactor;

        const float HOV_REGULAR = 1f;
        const float HOV_HIGHLIGHTED = 1.2f;
        const float HOV_PRESSED = 1.3f;

        private void Start()
        {
            hoveringFactor = HOV_REGULAR;
            GetComponent<ButtonWithTransition>().onStateTransition += Bt_onStateTransition;
            UpdateState();
        }

        private void Bt_onStateTransition(ButtonWithTransition.TransitionState state)
        {
            switch (state)
            {
                case ButtonWithTransition.TransitionState.Highlighted:
                    hoveringFactor = HOV_HIGHLIGHTED;
                    break;

                case ButtonWithTransition.TransitionState.Pressed:
                    hoveringFactor = HOV_PRESSED;
                    break;

                default:
                    hoveringFactor = HOV_REGULAR;
                    break;
            }
            UpdateState();
        }

        public void OnClick()
        {
            foreach (var pdi in transform.parent.GetComponentsInChildren<PagesDlgIcon>())
            {
                pdi.pageActive = (pdi == this);
                pdi.UpdateState();
            }
        }

        void UpdateState()
        {
            if (pageActive)
            {
                panel.Activate();

                var title = gameObject.name;
                transform.parent.GetComponentInChildren<PopupDialogTitle>().GetComponent<Text>().text = title;
            }

            panel.gameObject.SetActive(pageActive);
            scaleIcon.gameObject.SetActive(pageActive);
            scaleGrayIcon.gameObject.SetActive(!pageActive);

            float hov = hoveringFactor;
            if (pageActive)
                hov = Mathf.Max(hov, HOV_HIGHLIGHTED);
            var tr = pageActive ? scaleIcon : scaleGrayIcon;
            tr.localScale = Vector3.one * hov;
        }
    }
}

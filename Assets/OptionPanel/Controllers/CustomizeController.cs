using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using BaroqueUI;


namespace VRSketch4
{
    public class CustomizeController : MonoBehaviour
    {
        public Shader viveShaderController;
        public Texture2D viveTexControllerMask;
        /*public Material viveMatControllerOutline;
         *public GameObject viveControllerHoleMask;
         *   disabled: not needed because ambient occlusion gives some 3D shape impression too;
         *   and has got a large performance hit on the controller viewed very close up */
        public Material recordVideoButtonMaterial, playVideoButtonMaterial, replayVideoButtonMaterial;
        public Material trackpadDefaultMaterial, cancelButtonMaterial, typingButtonMaterial;
        public Material cancelButtonLeftRightMaterial, trackpadLeftRightMaterial, trackpadColorPaletteMaterial;

        public Shader oculusTouchShader;

        /*MaterialCache mcache_button;*/


        public enum ETouchpadHighlight
        {
            Plain, Cancel, Typing, TypingAB, MenuIcon, CancelLeftRight, MenuIconLeftRight,
            MenuIconColorPalette,
            _WhiteColor = 0x01ffffff   /* can also be any other color, as long as the .a is 0x01 */
        }

        class CtrlModel
        {
            internal Transform tr_model, tr_body;   /* only for Vive */
            internal Renderer rend_body;            /* only for Vive */

            internal bool ready;
            internal ETouchpadHighlight currently_tracking;
            internal ETouchpadHighlight touchpad_highlight;
        }

        CtrlModel[] controllers_model;
        bool customizing;

        public void SetTouchpadHighlight(Controller controller, ETouchpadHighlight highlighted)
        {
            /* highlighed can be:
             *
             *   0 : default touchpad, actual aspect computed from current state
             *   2 : forces the touchpad to be green
             *   3 : same as 2, except on Oculus Rift forces both A and B buttons to be green
             */
            var cm = controller.GetAdditionalData(ref controllers_model);
            cm.touchpad_highlight = highlighted;
        }

        public void ResetTouchpadHighlights()
        {
            if (controllers_model != null)
                foreach (var cm in controllers_model)
                    if (cm != null)
                        cm.touchpad_highlight = ETouchpadHighlight.Plain;
        }

        public void StartCustomizing()
        {
            if (!customizing)
            {
                Application.onBeforeRender += () =>
                {
                    foreach (var ctrl in Baroque.GetControllers())
                        UpdateMaterialsOnController(ctrl);
                };
                customizing = true;
            }
        }

        void Start()
        {
            StartCustomizing();
        }

        void UpdateMaterialsOnController(Controller ctrl)
        {
            var cm = ctrl.GetAdditionalData(ref controllers_model);
            if (!ctrl.isActiveAndEnabled)
            {
                cm.ready = false;
                return;
            }

            bool update_now = false;
            if (!cm.ready)
            {
                update_now = true;
                cm.ready = true;
            }

            ETouchpadHighlight currently_tracking;
            if (cm.touchpad_highlight == ETouchpadHighlight.Plain)
                currently_tracking = ETouchpadHighlight.Plain;// ToolBase.IsCurrentlyTracking(ctrl);
            else
                currently_tracking = cm.touchpad_highlight;
            if (cm.currently_tracking != currently_tracking)
            {
                cm.currently_tracking = currently_tracking;
                update_now = true;
            }

            if (cm.rend_body != null && cm.rend_body.sharedMaterial != viveCustomMaterial)
                update_now = true;

            if (update_now)
            {
                switch (Controller.controllerModel)
                {
                    case Controller.Model.HTCVive:
                        UpdateMaterialOnHTCViveController(ctrl, cm);
                        break;

                    case Controller.Model.OculusTouch:
                        UpdateMaterialOnOculusTouchController(ctrl, cm);
                        break;
                }
            }
        }

        Color TouchpadHighlight2Color(ETouchpadHighlight th)
        {
            return new Color32((byte)(int)th, (byte)((int)th >> 8), (byte)((int)th >> 16), 255);
        }


        Material viveCustomMaterial;

        void UpdateMaterialOnHTCViveController(Controller ctrl, CtrlModel cm)
        {
            if (cm.rend_body == null)
            {
                cm.tr_model = ctrl.transform.Find("Model");
                cm.tr_body = cm.tr_model.Find("body");
                if (cm.tr_model == null || cm.tr_body == null)
                {
                    cm.ready = false;
                    return;
                }
                cm.rend_body = cm.tr_body.GetComponent<Renderer>();
            }
            if (viveCustomMaterial == null)
            {
                /* initialize customViveCtrlMaterial by copying and tweaking the existing material */
                viveCustomMaterial = new Material(cm.rend_body.sharedMaterial);
                viveCustomMaterial.shader = viveShaderController;
                viveCustomMaterial.SetTexture("_MaskTex", viveTexControllerMask);
            }

            Material button_mat;
            button_mat = viveCustomMaterial;    /* but really, hidden because of _MaskTex */

            Material touchpad_mat;
            switch (cm.currently_tracking)
            {
                case ETouchpadHighlight.Plain: touchpad_mat = viveCustomMaterial; break;
                case ETouchpadHighlight.Cancel: touchpad_mat = cancelButtonMaterial; break;
                case ETouchpadHighlight.CancelLeftRight: touchpad_mat = cancelButtonLeftRightMaterial; break;
                case ETouchpadHighlight.Typing: touchpad_mat = typingButtonMaterial; break;
                case ETouchpadHighlight.TypingAB: touchpad_mat = typingButtonMaterial; break;
                case ETouchpadHighlight.MenuIcon: touchpad_mat = trackpadDefaultMaterial; break;
                case ETouchpadHighlight.MenuIconLeftRight: touchpad_mat = trackpadLeftRightMaterial; break;
                case ETouchpadHighlight.MenuIconColorPalette: touchpad_mat = trackpadColorPaletteMaterial; break;
                default:
                    /*if (mcache_button == null)
                        mcache_button = new MaterialCache(trackpadColorMaterial);
                    touchpad_mat = mcache_button.Get(TouchpadHighlight2Color(cm.currently_tracking));
                    break;*/
                    touchpad_mat = viveCustomMaterial;
                    break;
            }

            foreach (var rend in cm.tr_model.GetComponentsInChildren<Renderer>(includeInactive: true))
            {
                var tr = rend.transform;
                if (tr == cm.tr_body)
                {
                    /* for the main body of the controller, use the standard material */
                    /*rend.sharedMaterials = new Material[] { viveCustomMaterial, viveMatControllerOutline };*/
                    rend.sharedMaterial = viveCustomMaterial;
                }
                else if (tr.name == "button")
                {
                    /* for the menu button, it is either hidden (by applying the customMaterial)
                     * or not if we have recording/playing videos. */
                    rend.sharedMaterial = button_mat;
                }
                else if (tr.name.StartsWith("trackpad"))
                {
                    /* for the touchpad button, use one of the chosen selections */
                    rend.sharedMaterial = touchpad_mat;
                }
                else if (tr.name == "lgrip" || tr.name == "rgrip")
                {
                    /* for the grip buttons, use this, which has got a texture showing arrow icons */
                    rend.sharedMaterial = trackpadDefaultMaterial;
                }
                else
                {
                    /* for all other small parts of the model, just apply the customMaterial directly */
                    rend.sharedMaterial = viveCustomMaterial;
                }
            }

#if false
            if (ctrl.hotspot.Find("Hole mask") == null)
            {
                GameObject go = Instantiate(viveControllerHoleMask, ctrl.hotspot);
                go.name = "Hole mask";
                go.transform.localPosition = Vector3.zero;
                go.transform.localRotation = Quaternion.LookRotation(Vector3.up);
            }
#endif
        }

        void UpdateMaterialOnOculusTouchController(Controller ctrl, CtrlModel cm)
        {
            Baroque._oculus_touch_controllers_shader = oculusTouchShader;

            Color Acol = Color.black;
            Color Bcol = Color.black;
            Color Bcolborder = Color.black;
            Color joyCol = Color.black;

            switch (cm.currently_tracking)
            {
                case ETouchpadHighlight.Plain:
                case ETouchpadHighlight.MenuIcon:
                case ETouchpadHighlight.MenuIconLeftRight:
                case ETouchpadHighlight.MenuIconColorPalette:
                    break;  /* default */
                case ETouchpadHighlight.Cancel:
                case ETouchpadHighlight.CancelLeftRight:
                    Acol = new Color(0.6f, 0, 0);    /* red */
                    break;
                case ETouchpadHighlight.Typing:
                    Acol = new Color(0, 0.65f, 0);   /* green */
                    break;
                case ETouchpadHighlight.TypingAB:
                    Acol = Bcol = new Color(0, 0.65f, 0);    /* green on both A and B */
                    break;
                default:
                    joyCol = TouchpadHighlight2Color(cm.currently_tracking);
                    break;
            }
            ctrl.SetComponentColor(Controller.EColorizableComponent.TouchpadSurface, Acol);
            ctrl.SetComponentColor(Controller.EColorizableComponent.MenuSurface, Bcol);
            if (Bcol != Color.black)
                Bcolborder = Color.black;
            ctrl.SetComponentColor(Controller.EColorizableComponent.MenuBorder, Bcolborder);
            ctrl.SetComponentColor(Controller.EColorizableComponent.Joystick, joyCol);
            /* NB. if we send (Joystick, Black) then it might be replaced with dark green, in the
             * situation where the scrollwheel would be visible on Vive */
        }
    }
}

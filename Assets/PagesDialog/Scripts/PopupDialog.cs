using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using BaroqueUI;


namespace MyTest
{
    public interface IPopupDialogClose
    {
        void DoCloseDialog();
    }

    public class PopupDialog : MonoBehaviour, IPopupDialogClose
    {
        /* For any dialog box.  When used in conjunction with PopupMenu,
         * set "temporary" to true.  When used in conjunction with dialog
         * boxes which have a "close" button, set "temporary" to false.
         */
        public bool temporary;
        public bool moveToBottom;
        public bool updateCubeSizes;

        /* these two fields are null or non-null at the same time */
        static PopupDialog temporary_dialog;
        //static object saved_update;


        void MovePanelToInitialPosition(Controller follow_ctrl)
        {
            Vector3 fwd = follow_ctrl.forward;
            Vector3 head = follow_ctrl.position - Baroque.GetHeadTransform().position;

            Quaternion rot = Quaternion.LookRotation(fwd * 0.25f + head.normalized);
            Vector3 pos = PopupDialogCanvas.GetPos(follow_ctrl);

            float move_closer = Mathf.Min(head.magnitude * 0.14f, 0.0874f);
            pos += move_closer * (rot * Vector3.back);

            transform.SetPositionAndRotation(pos, rot);

            if (moveToBottom)
            {
                var backgroundCube = GetComponentInChildren<PopupDialogCanvas>().backgroundCubeTransform;
                Vector3 v = backgroundCube.TransformVector(new Vector3(0, 0.5f, 0));
                transform.position += v + transform.up * 0.02f;
            }

            GetComponentInChildren<PopupDialogCanvas>().prefer_controller = follow_ctrl;
        }

        public void ShowDialog(Controller initial_position_ctrl = null)
        {
            if (updateCubeSizes)
                GetComponentInChildren<PopupDialogCanvas>().UpdateBackgroundCubeSize();

            if (initial_position_ctrl != null)
                MovePanelToInitialPosition(initial_position_ctrl);

            if (temporary)
            {
                CloseExistingTemporaryDialog();

                foreach (var ctrl1 in Baroque.GetControllers())
                    ctrl1.SetControllerHints(/*nothing*/);

                foreach (var ctrl in Baroque.GetControllers())
                    StartCoroutine(FollowMenuOnController(ctrl, ctrl.menuPressed));

                //saved_update = ToolBase.SuspendControllersUpdate();
                temporary_dialog = this;
            }
            /* else: saved_update remains unmodified */
        }

        IEnumerator FollowMenuOnController(Controller follow_ctrl, bool menu_initially_pressed)
        {
            while (gameObject.activeSelf)
            {
                if (follow_ctrl.isActiveAndEnabled && follow_ctrl.menuPressed != menu_initially_pressed)
                {
                    /* we just released the menu button: emulate a click and then close the dialog */
                    var pdc = GetComponentInChildren<PopupDialogCanvas>();
                    if (pdc != null)
                    {
                        pdc.MouseDown(follow_ctrl);
                        yield return null;
                        pdc.MouseUp(follow_ctrl);
                    }
                    DoCloseDialog();
                    yield break;
                }
                yield return null;
            }
        }

        public static void CloseExistingTemporaryDialog()
        {
            if (temporary_dialog != null)
            {
                //ToolBase.RestoreControllersUpdate(saved_update);
                //saved_update = null;
                if (temporary_dialog.gameObject)   /* not destroyed already */
                    Destroy(temporary_dialog.gameObject);
                temporary_dialog = null;
            }
        }

        private void OnDestroy()
        {
            if (temporary_dialog == this)
            {
                //ToolBase.RestoreControllersUpdate(saved_update);
                //saved_update = null;
                temporary_dialog = null;
            }
        }

        public void DoCloseDialog()
        {
            if (gameObject)     /* if not already destroyed */
                Destroy(gameObject);
        }
    }
}

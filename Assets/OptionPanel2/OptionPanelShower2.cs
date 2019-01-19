using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using BaroqueUI;


public class OptionPanelShower2 : MonoBehaviour
{
    public OptionPanel optionPanelPrefab;


    OptionPanel panel;
    Controller follow_ctrl;
    //Vector2 shift;

    void Start()
    {
        var gt = Controller.GlobalTracker(this);
        gt.onMenuClick += Gt_onMenuClick;
        gt.onControllersUpdate += Gt_onControllersUpdate;
    }

    private void Gt_onControllersUpdate(Controller[] controllers)
    {
        if (panel == null || !follow_ctrl.menuPressed)
            return;

        /*var tr = panel.backgroundCube.transform;
        shift = tr.InverseTransformPoint(follow_ctrl.position);*/
        MovePanel();
    }

    private void Gt_onMenuClick(Controller controller)
    {
        if (panel != null)
        {
            Destroy(panel.gameObject);
            panel = null;
        }
        else
        {
            follow_ctrl = controller;
            panel = Instantiate(optionPanelPrefab);
            //shift = new Vector2(0, -1);
            MovePanel();
        }
    }

    void MovePanel()
    {
        /*if (shift.x < -0.5f) shift.x = -0.5f;
        if (shift.x > 0.5f) shift.x = 0.5f;
        if (shift.y < -0.5f) shift.y = -0.5f;
        if (shift.y > 0.5f) shift.y = 0.5f;*/

        var tr = panel.backgroundCube.transform;
        Vector3 v = tr.TransformVector(new Vector3(0, -0.5f, 0));

        Vector3 fwd = follow_ctrl.forward;
        Vector3 head = follow_ctrl.position - Baroque.GetHeadTransform().position;

        Quaternion rot = Quaternion.LookRotation(fwd + head.normalized);

        panel.transform.SetPositionAndRotation(
            follow_ctrl.position - v,
            rot);
    }
}

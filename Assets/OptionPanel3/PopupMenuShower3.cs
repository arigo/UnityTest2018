using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using BaroqueUI;


public class PopupMenuShower3 : MonoBehaviour
{
    public PopupMenu menuPrefab;


    PopupMenu menu;
    Controller follow_ctrl;

    void Start()
    {
        var gt = Controller.GlobalTracker(this);
        gt.onMenuClick += Gt_onMenuClick;
        gt.onGripDown += Gt_onGripDown;
        gt.onGripDrag += Gt_onGripDrag;
    }

    bool grip_down;
    Vector3 origin_position;
    Quaternion origin_rotation;

    private void Gt_onGripDown(Controller controller)
    {
        grip_down = (menu != null);
        if (grip_down)
        {
            origin_rotation = Quaternion.Inverse(controller.rotation) * menu.transform.rotation;
            origin_position = Quaternion.Inverse(menu.transform.rotation) * (menu.transform.position - controller.position);
        }
    }

    private void Gt_onGripDrag(Controller controller)
    {
        if (menu == null || !grip_down)
            return;
        menu.transform.rotation = controller.rotation * origin_rotation;
        menu.transform.position = controller.position + menu.transform.rotation * origin_position;
    }

    private void Gt_onMenuClick(Controller controller)
    {
        if (menu != null)
        {
            Destroy(menu.gameObject);
            menu = null;
        }
        else
        {
            follow_ctrl = controller;
            menu = Instantiate(menuPrefab);
            menu.SetItems(new PopupMenu.Item[]
            {
                new PopupMenu.Item("short arc", ()=>{}),
                new PopupMenu.Item("regular arc", PopupMenu.ECheckbox.Bullet, ()=>{}),
                new PopupMenu.Item("long arc", PopupMenu.ECheckbox.Check, ()=>{}),
                new PopupMenu.Item("flat line", PopupMenu.ECheckbox.None, null),
                PopupMenu.Item.separator,
                new PopupMenu.Item("turn left", ()=>{}),
                new PopupMenu.Item("turn right", ()=>{Debug.Log("TURN RIGHT"); }),
            });
            MovePanelToInitialPosition();
        }
    }

    void MovePanelToInitialPosition()
    {
        var tr = menu.backgroundCube.transform;
        Vector3 v = tr.TransformVector(new Vector3(0, -0.5f, 0));

        Vector3 fwd = follow_ctrl.forward;
        Vector3 head = follow_ctrl.position - Baroque.GetHeadTransform().position;

        Quaternion rot = Quaternion.LookRotation(fwd + head.normalized);

        menu.transform.SetPositionAndRotation(
            follow_ctrl.position - v + 0.015f * (rot * Vector3.up),
            rot);
    }
}

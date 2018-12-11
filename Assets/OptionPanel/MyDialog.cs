#if false
using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using UnityEngine.EventSystems;
using BaroqueUI;


public class MyDialog : MonoBehaviour
{
    public Transform pointerPrefab;

    class PointerPos
    {
        internal Transform pointer;
    }
    PointerPos[] pointers_pos;


    private void Start()
    {
        var ct = Controller.HoverTracker(this);
        ct.SetPriority(500);
        ct.onEnter += OnEnter;
        ct.onMoveOver += MouseMove;
        ct.onLeave += OnLeave;
        ct.onTriggerDown += MouseDown;
        ct.onTriggerDrag += MouseMove;
        ct.onTriggerUp += MouseUp;
    }

    PointerEventData pevent;
    GameObject current_pressed;

    void OnEnter(Controller controller)
    {
        pevent = new PointerEventData(EventSystem.current);
    }

    void MouseMove(Controller controller)
    {
        if (current_pressed == null)
        {
            // handle enter and exit events (highlight)
            GameObject new_target = null;
            if (UpdateCurrentPoint(controller.position))
                new_target = pevent.pointerCurrentRaycast.gameObject;

            UpdateHoveringTarget(new_target);
        }
        else
        {
            if (UpdateCurrentPoint(controller.position, allow_out_of_bounds: true))
            {
                if (pevent.pointerDrag != null)
                    ExecuteEvents.Execute(pevent.pointerDrag, pevent, ExecuteEvents.dragHandler);
            }
        }
        UpdateCursor(controller, true);
    }

    void UpdateCursor(Controller controller, bool visible)
    {
        var pp = controller.GetAdditionalData(ref pointers_pos);
        if ((pp.pointer != null) != visible)
        {
            if (visible)
                pp.pointer = Instantiate(pointerPrefab);
            else
            {
                Destroy(pp.pointer);
                pp.pointer = null;
            }
        }
        if (pp.pointer != null)
        {
            pp.pointer.transform.rotation = transform.rotation;
        }
    }

    static Vector2 ScreenPoint(Canvas canvas, Vector3 world_position)
    {
        RectTransform rtr = canvas.transform as RectTransform;
        Vector2 local_pos = canvas.transform.InverseTransformPoint(world_position);   /* drop the 'z' coordinate */
        local_pos.x += rtr.rect.width * rtr.pivot.x;
        local_pos.y += rtr.rect.height * rtr.pivot.y;
        /* Here, 'local_pos' is in coordinates that match the UI element coordinates.
         * To convert it to the 'screenspace' coordinates of a camera, we need to apply
         * a scaling factor of 'pixels_per_unit'. */
        float pixels_per_unit = canvas.GetComponent<CanvasScaler>().dynamicPixelsPerUnit;
        return local_pos * pixels_per_unit;
    }

    static void CustomRaycast(Canvas canvas, Vector3 world_position, List<RaycastResult> results)
    {
        Vector2 screen_point = ScreenPoint(canvas, world_position);
        var graphicsForCanvas = GraphicRegistry.GetGraphicsForCanvas(canvas);
        for (int i = 0; i < graphicsForCanvas.Count; i++)
        {
            Graphic graphic = graphicsForCanvas[i];
            if (graphic.canvasRenderer.cull)
                continue;
            if (graphic.depth == -1)
                continue;
            if (!graphic.raycastTarget)
                continue;
            if (!RectTransformUtility.RectangleContainsScreenPoint(graphic.rectTransform, screen_point, ortho_camera))
                continue;
            if (!graphic.Raycast(screen_point, ortho_camera))
                continue;

            results.Add(new RaycastResult
            {
                gameObject = graphic.gameObject,
                module = canvas.GetComponent<GraphicRaycaster>(),
                index = results.Count,
                depth = graphic.depth,
                sortingLayer = canvas.sortingLayerID,
                sortingOrder = canvas.sortingOrder,
                screenPosition = screen_point,
            });
        }
    }

    bool UpdateCurrentPoint(Vector3 controller_position, bool allow_out_of_bounds = false)
    {
        Vector2 screen_point = ScreenPoint(GetComponent<Canvas>(), controller_position);
        pevent.position = screen_point;

        var results = new List<RaycastResult>();
        foreach (var canvas in GetComponentsInChildren<Canvas>())
            CustomRaycast(canvas, controller_position, results);

        RaycastResult rr = new RaycastResult { depth = -1, screenPosition = screen_point };
        if (!BestRaycastResult(results, ref rr))
        {
            if (!allow_out_of_bounds)
                return false;
        }
        rr.worldNormal = transform.forward;
        rr.worldPosition = Vector3.ProjectOnPlane(controller_position - transform.position, transform.forward) + transform.position;
        pevent.pointerCurrentRaycast = rr;
        return rr.gameObject != null;
    }

    void UpdateHoveringTarget(GameObject new_target)
    {
        if (new_target == pevent.pointerEnter)
            return;    /* already up-to-date */

        /* pop off any hovered objects from the stack, as long as they are not parents of 'new_target' */
        while (pevent.hovered.Count > 0)
        {
            GameObject h = pevent.hovered[pevent.hovered.Count - 1];
            if (!h)
            {
                pevent.hovered.RemoveAt(pevent.hovered.Count - 1);
                continue;
            }
            if (new_target != null && new_target.transform.IsChildOf(h.transform))
                break;
            pevent.hovered.RemoveAt(pevent.hovered.Count - 1);
            ExecuteEvents.Execute(h, pevent, ExecuteEvents.pointerExitHandler);
        }

        /* enter and push any new object going to 'new_target', in order from outside to inside */
        pevent.pointerEnter = new_target;
        if (new_target != null)
            EnterAndPush(new_target.transform, pevent.hovered.Count == 0 ? transform :
                          pevent.hovered[pevent.hovered.Count - 1].transform);
    }

    void EnterAndPush(Transform new_target_transform, Transform limit)
    {
        if (new_target_transform != limit)
        {
            EnterAndPush(new_target_transform.parent, limit);
            ExecuteEvents.Execute(new_target_transform.gameObject, pevent, ExecuteEvents.pointerEnterHandler);
            pevent.hovered.Add(new_target_transform.gameObject);
        }
    }

    void OnLeave(Controller controller)
    {
        UpdateHoveringTarget(null);
        pevent = null;
        UpdateCursor(controller, false);
    }

    void MouseDown(Controller controller)
    {
        if (current_pressed != null)
            return;

        if (UpdateCurrentPoint(controller.position))
        {
            pevent.pressPosition = pevent.position;
            pevent.pointerPressRaycast = pevent.pointerCurrentRaycast;
            pevent.pointerPress = null;

            GameObject target = pevent.pointerPressRaycast.gameObject;
            current_pressed = ExecuteEvents.ExecuteHierarchy(target, pevent, ExecuteEvents.pointerDownHandler);

            if (current_pressed != null)
            {
                ExecuteEvents.Execute(current_pressed, pevent, ExecuteEvents.beginDragHandler);
                pevent.pointerDrag = current_pressed;
            }
            else
            {
                /* some objects have only a pointerClickHandler */
                current_pressed = target;
                pevent.pointerDrag = null;
            }
        }
    }

    void MouseUp(Controller controller)
    {
        bool in_bounds = UpdateCurrentPoint(controller.position);

        if (pevent.pointerDrag != null)
        {
            ExecuteEvents.Execute(pevent.pointerDrag, pevent, ExecuteEvents.endDragHandler);
            if (in_bounds)
            {
                ExecuteEvents.ExecuteHierarchy(pevent.pointerDrag, pevent, ExecuteEvents.dropHandler);
            }
            ExecuteEvents.Execute(pevent.pointerDrag, pevent, ExecuteEvents.pointerUpHandler);
            pevent.pointerDrag = null;
        }

        if (in_bounds)
            ExecuteEvents.Execute(current_pressed, pevent, ExecuteEvents.pointerClickHandler);

        current_pressed = null;
    }
}
#endif
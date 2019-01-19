using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using UnityEngine.EventSystems;
using BaroqueUI;


public class MyMenuDialog : MonoBehaviour
{
    public RectTransform highlightPrefab;


    RectTransform highlight;

    private void Start()
    {
        var ct = Controller.HoverTracker(this);
        ct.computePriority = GetPriority;
        ct.onEnter += OnEnter;
        ct.onMoveOver += MouseMove;
        ct.onLeave += OnLeave;
        ct.onTriggerDown += (ctrl) => MouseDownAt(ctrl.position);
        ct.onTriggerDrag += MouseMove;
        ct.onTriggerUp += (ctrl) => MouseUpAt(ctrl.position);
        ct.onMenuClick += MenuClick;

        foreach (var canvas in GetComponentsInChildren<Canvas>())
        {
            var rend = canvas.GetComponent<DialogRenderer>();
            if (rend != null)
                Destroy(rend);
            rend = canvas.gameObject.AddComponent<DialogRenderer>();
            float pixels_per_unit = GetComponent<CanvasScaler>().dynamicPixelsPerUnit;
            rend.PrepareForRendering(pixels_per_unit);
        }

        foreach (var ctrl in Baroque.GetControllers())
            if (ctrl.isActiveAndEnabled && ctrl.menuPressed)
                StartCoroutine(FollowMenuOnController(ctrl));
    }

    private void OnDisable()
    {
        current_pressed = null;
        Leave();
    }

    static bool IsBetterRaycastResult(RaycastResult rr1, RaycastResult rr2)
    {
        if (rr1.sortingLayer != rr2.sortingLayer)
            return SortingLayer.GetLayerValueFromID(rr1.sortingLayer) > SortingLayer.GetLayerValueFromID(rr2.sortingLayer);
        if (rr1.sortingOrder != rr2.sortingOrder)
            return rr1.sortingOrder > rr2.sortingOrder;
        if (rr1.depth != rr2.depth)
            return rr1.depth > rr2.depth;
        if (rr1.distance != rr2.distance)
            return rr1.distance < rr2.distance;
        return rr1.index < rr2.index;
    }

    static bool BestRaycastResult(List<RaycastResult> lst, ref RaycastResult best_result)
    {
        bool found_any = false;

        foreach (var result in lst)
        {
            if (result.gameObject == null)
                continue;
            if (!found_any || IsBetterRaycastResult(result, best_result))
            {
                best_result = result;
                found_any = true;
            }
        }
        return found_any;
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

        /*Selectable selectable = null;
        if (pevent.pointerEnter != null)
            selectable = pevent.pointerEnter.GetComponentInParent<Selectable>();

        if (selectable != null && selectable.IsInteractable())
        {
            var rtr = selectable.transform as RectTransform;
            var canvas = GetComponent<Canvas>();
            Vector2 actual_pt = canvas.transform.InverseTransformPoint(controller.position);
            Vector2 topleft = canvas.transform.InverseTransformPoint(rtr.TransformPoint(rtr.rect.min));
            Vector2 rectsize = canvas.transform.InverseTransformVector(rtr.TransformVector(rtr.rect.size));
            cross.SetBoundsAndHighlight(new Rect(topleft, rectsize), actual_pt);
            cross.gameObject.SetActive(true);
        }
        else
            cross.gameObject.SetActive(false);*/

        if (highlight == null)
            highlight = Instantiate(highlightPrefab, transform);

        var canvas = GetComponent<Canvas>();
        Vector2 actual_pt = canvas.transform.InverseTransformPoint(controller.position);
        highlight.localPosition = actual_pt;
    }

#if false
    void UpdateCursor(Controller controller)
    {
        bool visible = (controller != null);
        cross.gameObject.SetActive(visible);

        if (visible)
        {
            /*var tr = pointer.transform;
            Vector3 pos = controller.position;
            pos = transform.position + Vector3.ProjectOnPlane(pos - transform.position, transform.forward);
            tr.position = pos;
            tr.rotation = transform.rotation;*/

            Vector2 local_pos = transform.InverseTransformPoint(controller.position);   /* drop the 'z' coordinate */
            cross.localPosition = local_pos;   /* z = 0 */
        }
    }
#endif

    bool UpdateCurrentPoint(Vector3 controller_position, bool allow_out_of_bounds = false)
    {
        Vector2 screen_point = GetComponent<DialogRenderer>().ScreenPoint(controller_position);
        pevent.position = screen_point;

        var results = new List<RaycastResult>();
        foreach (var rend in GetComponentsInChildren<DialogRenderer>())
            rend.CustomRaycast(controller_position, results);

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

    void Leave()
    {
        if (pevent != null)
        {
            UpdateHoveringTarget(null);
            pevent = null;
        }
        //UpdateCursor(null);
        //cross.gameObject.SetActive(false);
        if (highlight != null)
        {
            Destroy(highlight.gameObject);
            highlight = null;
        }
    }

    void OnLeave(Controller controller)
    {
        Leave();
    }

    void MouseDownAt(Vector3 ctrl_position)
    {
        if (current_pressed != null)
            return;

        if (UpdateCurrentPoint(ctrl_position))
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

    void MouseUpAt(Vector3 ctrl_position)
    {
        bool in_bounds = UpdateCurrentPoint(ctrl_position);

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

        /* unselect as soon as we release the mouse press */
        if (EventSystem.current != null)
            EventSystem.current.SetSelectedGameObject(null);
    }

    float GetPriority(Controller controller)
    {
        Plane plane = new Plane(transform.forward, transform.position);
        Ray ray = new Ray(controller.position, transform.forward);
        float enter;
        if (!plane.Raycast(ray, out enter))
            enter = 0;
        return 100 + enter;
    }

    IEnumerator FollowMenuOnController(Controller follow_ctrl)
    {
        while (gameObject.activeSelf)
        {
            if (!follow_ctrl.menuPressed)
            {
                /* we just released the menu button */
                if (pevent != null)
                {
                    /* we're around the dialog, with the cursor visible */
                    Vector3 position = follow_ctrl.position;
                    if (current_pressed == null)
                    {
                        /* press */
                        MouseDownAt(position);
                        /* wait */
                        yield return new WaitForSeconds(0.07f);
                        if (pevent == null)
                            yield break;
                    }
                    /* release */
                    MouseUpAt(position);
                }
                else
                {
                    /* we're completely outside the dialog: close it */
                    SendMessageUpwards("CloseDialog");
                }
                yield break;
            }
            yield return null;
        }
    }

    void MenuClick(Controller ctrl)
    {
        MouseDownAt(ctrl.position);
        StartCoroutine(FollowMenuOnController(ctrl));
    }


    /****************************************************************************************/

    class DialogRenderer : MonoBehaviour
    {
        Camera ortho_camera;
        float pixels_per_unit;
        static readonly Vector2[] screen_deltas = new Vector2[]
        {
            new Vector2(0, 0),
            new Vector2(0, 30f),
            new Vector2(0, -30f),
            new Vector2(50f, 0),
            new Vector2(-50f, 0),
        };

        public void PrepareForRendering(float pixels_per_unit)
        {
            RectTransform rtr = transform as RectTransform;
            if (GetComponentInChildren<Collider>() == null)
            {
                Rect r = rtr.rect;
                float zscale = transform.InverseTransformVector(transform.forward * 0.3f).magnitude;

                BoxCollider coll = gameObject.AddComponent<BoxCollider>();
                coll.isTrigger = true;
                coll.size = new Vector3(r.width, r.height, zscale);
                coll.center = new Vector3(r.center.x, r.center.y, 0);
            }

            this.pixels_per_unit = pixels_per_unit;
            /* This feels like a hack, but to get UI elements from a 3D position, we need a Camera
             * to issue a Raycast().  This "camera" is set up to "look" from the controller's point 
             * of view, usually orthogonally from the plane of the UI (but it could also be along
             * the controller's direction, if we go for ray-casting selection).  This is inspired 
             * from https://github.com/VREALITY/ViveUGUIModule.
             */
            Transform tr1 = transform.Find("Ortho Camera");
            if (tr1 != null)
                ortho_camera = tr1.GetComponent<Camera>();
            else
                ortho_camera = new GameObject("Ortho Camera").AddComponent<Camera>();
            ortho_camera.enabled = false;
            ortho_camera.transform.SetParent(transform);
            ortho_camera.transform.position = rtr.TransformPoint(
                rtr.rect.width * (0.5f - rtr.pivot.x),
                rtr.rect.height * (0.5f - rtr.pivot.y),
                0);
            ortho_camera.transform.rotation = rtr.rotation;
            ortho_camera.clearFlags = CameraClearFlags.SolidColor;
            ortho_camera.orthographic = true;
            ortho_camera.orthographicSize = rtr.TransformVector(0, rtr.rect.height * 0.5f, 0).magnitude;
            ortho_camera.nearClipPlane = -10;
            ortho_camera.farClipPlane = 10;
            /* XXX Not managing to get a correct positionning with a camera with no targetTexture.
             * XXX At least we can stick it a correctly-sized RenderTexture, which should never be
             * XXX realized on the GPU.  Nonsense.
             */
            var render_texture = new RenderTexture((int)(rtr.rect.width * pixels_per_unit + 0.5),
                                                   (int)(rtr.rect.height * pixels_per_unit + 0.5), 32);
            ortho_camera.targetTexture = render_texture;

            GetComponent<Canvas>().worldCamera = ortho_camera;
        }

        public void CustomRaycast(Vector3 world_position, List<RaycastResult> results)
        {
            Vector2 screen_point = ScreenPoint(world_position);
            var canvas = GetComponent<Canvas>();
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

                int inside = -1;
                for (int j = 0; j < screen_deltas.Length; j++)
                {
                    Vector2 sp1 = screen_point + screen_deltas[j];
                    if (!RectTransformUtility.RectangleContainsScreenPoint(graphic.rectTransform, sp1, ortho_camera))
                        continue;
                    if (!graphic.Raycast(sp1, ortho_camera))
                        continue;
                    inside = j;
                    break;
                }
                if (inside < 0)
                    continue;

                results.Add(new RaycastResult {
                    gameObject = graphic.gameObject,
                    module = GetComponent<GraphicRaycaster>(),
                    index = results.Count,
                    depth = graphic.depth,
                    sortingLayer = canvas.sortingLayerID,
                    sortingOrder = canvas.sortingOrder + (inside == 0 ? 1000000 : 0),
                    screenPosition = screen_point,
                });
            }
        }

        public Vector2 ScreenPoint(Vector3 world_position)
        {
            RectTransform rtr = transform as RectTransform;
            Vector2 local_pos = transform.InverseTransformPoint(world_position);   /* drop the 'z' coordinate */
            local_pos.x += rtr.rect.width * rtr.pivot.x;
            local_pos.y += rtr.rect.height * rtr.pivot.y;
            /* Here, 'local_pos' is in coordinates that match the UI element coordinates.
             * To convert it to the 'screenspace' coordinates of a camera, we need to apply
             * a scaling factor of 'pixels_per_unit'. */
            return local_pos * pixels_per_unit;
        }
    }
}

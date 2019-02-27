using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using UnityEngine.EventSystems;
using BaroqueUI;


namespace MyTest
{
    public class PopupDialogCanvas : MonoBehaviour
    {
        /* This component is put on the top-level Canvas level of dialog boxes and pop-up menus.
         * It handles:
         *
         *  - setting the size of the canvas
         *  - a cursor that follows the controller position when it is close to the dialog box
         *  - the interactions of this cursor with the Unity UI components
         *  - buttons called "K-X" or "K-back" automatically emulate the keys X or backspace
         *  - optionally, the dialog title (click on the title to move; click on the "x" to close)
         *  - controller hints (usually removed, except if there is a DialogUIHint component)
         *  - the controller grab buttons can also be used to move the dialog
         */

        public Selectable initialSelection;
        public RectTransform cursorPrefab;
        public Transform backgroundCubeTransform, cubeTitleTransform;
        public PopupDialogTitle dialogTitle;

        internal Controller prefer_controller;

        public void UpdateBackgroundCubeSize()
        {
            var rtr = transform as RectTransform;
            Rect rect = rtr.rect;
            Vector3 scale = backgroundCubeTransform.localScale;
            scale.x = rect.width;
            scale.y = rect.height;
            backgroundCubeTransform.localScale = scale;

            if (cubeTitleTransform != null)
            {
                scale = cubeTitleTransform.localScale;
                scale.x = rect.width;
                cubeTitleTransform.localScale = scale;
            }
        }


        RectTransform cursor;

        private IEnumerator Start()
        {
            var ct = Controller.HoverTracker(this);
            ct.computePriority = GetPriority;
            ct.onEnter += OnEnter;
            ct.onMoveOver += MouseMove;
            ct.onLeave += OnLeave;
            ct.onTriggerDown += MouseDown;
            ct.onTriggerDrag += MouseMove;
            ct.onTriggerUp += MouseUp;
            ct.onGripDown += GripDown;
            ct.onGripDrag += GripDrag;

            foreach (var canvas in GetComponentsInChildren<Canvas>())
            {
                var rend = canvas.GetComponent<DialogRenderer>();
                if (rend != null)
                    Destroy(rend);
                rend = canvas.gameObject.AddComponent<DialogRenderer>();
                float pixels_per_unit = GetComponent<CanvasScaler>().dynamicPixelsPerUnit;
                rend.PrepareForRendering(pixels_per_unit);
            }

            /* wait until the EventSystem is initialized */
            while (EventSystem.current == null ||
                   EventSystem.current.currentInputModule == null ||
                   EventSystem.current.currentInputModule.input == null)
                yield return null;
            yield return null;   /* and one more for the road */

            if (this)
            {
                var input_field = GetComponentInChildren<InputField>();
                if (input_field != null)
                    InitializeInputFields();

                if (initialSelection != null)
                    SetInitialSelection();
            }
        }


        /*************************************************************************/

        class KeyInfo
        {
            /* these fields control the appearence of the key, not its behaviour */
            internal PopupDialogCanvas pdc;
            internal string key_insert;
            internal Graphic image;
            internal float blink_end;

            internal const float TOTAL_KEY_TIME = 0.3f;

            void UpdateColor()
            {
                float done_fraction = 1 - (blink_end - Time.time) / TOTAL_KEY_TIME;
                Color col1 = Color.red, col2 = Color.white;
                image.color = Color.Lerp(col1, col2, done_fraction);
                if (done_fraction >= 1)
                    blink_end = 0;
            }

            IEnumerator CoUpdate()
            {
                while (blink_end > 0)
                {
                    UpdateColor();
                    yield return null;
                }
            }

            internal void SetBlink()
            {
                bool was_inactive = (blink_end == 0);
                float end = Time.time + TOTAL_KEY_TIME;
                if (end > blink_end)
                {
                    blink_end = end;
                    UpdateColor();
                }
                if (was_inactive)
                    pdc.StartCoroutine(CoUpdate());
            }
        }

        Dictionary<Button, KeyInfo> key_infos;

        void InitializeInputFields()
        {
            Debug.Assert(EventSystem.current.currentInputModule is PopupVRInputModule,
                "You need to create an EventSystem object with PopupVRInputModule as a component.");

            key_infos = new Dictionary<Button, KeyInfo>();

            foreach (var btn in GetComponentsInChildren<Button>(includeInactive: true))
            {
                string name = btn.gameObject.name;
                if (!name.StartsWith("K-"))
                    continue;

                var info = new KeyInfo();
                info.pdc = this;
                info.key_insert = name.Substring(2);
                info.image = btn.targetGraphic;
                key_infos[btn] = info;
            }
        }

        void SetInitialSelection()
        {
            if (!this || !isActiveAndEnabled)
                return;
            Debug.Assert(initialSelection.interactable);
            //EventSystem.current.SetSelectedGameObject(initialSelection.gameObject, null);
            initialSelection.Select();
        }

        bool SendKeyPressFrom(GameObject target)
        {
            /* special case for buttons listed in key_infos: send the key press to the
             * currently selected InputField */

            KeyInfo info;
            Button button = target.GetComponentInParent<Button>();
            if (button == null || key_infos == null || !key_infos.TryGetValue(button, out info))
                return false;

            Debug.Log("Send key press: " + info.key_insert);

            InputField input_field = null;
            var cursel = EventSystem.current.currentSelectedGameObject;
            if (cursel != null && cursel.GetComponentInParent<PopupDialogCanvas>() == this)
                input_field = cursel.GetComponent<InputField>();

            if (input_field == null)
                input_field = initialSelection as InputField;

            SendKeyPressInto(input_field, info);
            return true;
        }

        void SendKeyPressInto(InputField input_field, KeyInfo info)
        {
            if (input_field == null)
            {
                Debug.LogWarning("Pressed key has nowhere to go");
                return;
            }

            info.SetBlink();

            string key = info.key_insert;
            int start, stop;
            KeyboardVRInput.GetBounds(input_field, out start, out stop);
            if (key == "back")
            {
                if (start == stop)
                {
                    start = stop - 1;
                    if (start < 0)
                        return;
                }
                key = "";
            }

            string text = input_field.text;
            text = text.Substring(0, start) + key + text.Substring(stop, text.Length - stop);
            input_field.text = text;
            stop = start + key.Length;
            KeyboardVRInput.SetBounds(input_field, stop, stop);

            input_field.onValueChanged.Invoke(text);
        }

        /*************************************************************************/


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


        public static Controller most_recent_ctrl { get; private set; }
        PointerEventData pevent;
        GameObject current_pressed;
        /*RectTransform in_front;
        const float ZCLOSER = -15;*/
        const float ZCLOSERCURSOR = -18;

        public static Vector3 GetPos(Controller ctrl)
        {
            /* position of the menu button on the Vive controller */
            return ctrl.transform.TransformPoint(new Vector3(0, 0.0082f, -0.01968f));
        }

        void OnEnter(Controller controller)
        {
            pevent = new PointerEventData(EventSystem.current);
            most_recent_ctrl = controller;

            controller.SetControllerHints(/*nothing*/);
        }

        void MouseMove(Controller controller)
        {
            if (pevent == null)
                return;
            most_recent_ctrl = controller;
            if (current_pressed == null)
            {
                // handle enter and exit events (highlight)
                GameObject new_target = null;
                if (UpdateCurrentPoint(controller))
                    new_target = pevent.pointerCurrentRaycast.gameObject;

                UpdateHoveringTarget(new_target);
            }
            else
            {
                if (UpdateCurrentPoint(controller, allow_out_of_bounds: true))
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
                Vector2 actual_pt = canvas.transform.InverseTransformPoint(GetPos(controller));
                Vector2 topleft = canvas.transform.InverseTransformPoint(rtr.TransformPoint(rtr.rect.min));
                Vector2 rectsize = canvas.transform.InverseTransformVector(rtr.TransformVector(rtr.rect.size));
                cross.SetBoundsAndHighlight(new Rect(topleft, rectsize), actual_pt);
                cross.gameObject.SetActive(true);
            }
            else
                cross.gameObject.SetActive(false);*/

            if (cursor == null)
                cursor = Instantiate(cursorPrefab, transform);

            var canvas = GetComponent<Canvas>();
            Vector3 actual_pt = canvas.transform.InverseTransformPoint(GetPos(controller));
            actual_pt.z = ZCLOSERCURSOR;
            cursor.localPosition = actual_pt;
        }

        bool UpdateCurrentPoint(Controller ctrl, bool allow_out_of_bounds = false)
        {
            return UpdateCurrentPoint(GetPos(ctrl), allow_out_of_bounds);
        }

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

        /*Dictionary<RectTransform, float> z_origins;*/

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

#if false
            var btn = new_target == null ? null : new_target.GetComponentInParent<Button>();
            RectTransform new_in_front = btn == null ? null : (btn.transform as RectTransform);
            if (new_in_front != in_front)
            {
                if (z_origins == null)
                    z_origins = new Dictionary<RectTransform, float>();

                if (in_front != null)
                {
                    Vector3 pos = in_front.localPosition;
                    if (!z_origins.TryGetValue(in_front, out pos.z))
                        pos.z = 0f;
                    in_front.localPosition = pos;
                }
                if (new_in_front != null)
                {
                    Vector3 pos = new_in_front.localPosition;
                    if (pos.z != 0f && !z_origins.ContainsKey(new_in_front))
                        z_origins.Add(new_in_front, pos.z);
                    pos.z = ZCLOSER;
                    new_in_front.localPosition = pos;
                }
                in_front = new_in_front;
            }
#endif
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
            if (cursor != null)
            {
                Destroy(cursor.gameObject);
                cursor = null;
            }
        }

        void OnLeave(Controller controller)
        {
            most_recent_ctrl = controller;
            Leave();
        }

        public void MouseDown(Controller ctrl)
        {
            if (current_pressed != null)
                return;
            if (pevent == null)
                pevent = new PointerEventData(EventSystem.current);
            most_recent_ctrl = ctrl;

            if (UpdateCurrentPoint(ctrl))
            {
                if (SendKeyPressFrom(pevent.pointerCurrentRaycast.gameObject))
                    return;   /* don't do normal processing; the key button is not selected */

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

        public void MouseUp(Controller ctrl)
        {
            if (pevent != null)
            {
                most_recent_ctrl = ctrl;
                bool in_bounds = UpdateCurrentPoint(ctrl);

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
            }

            current_pressed = null;

            /* unselect as soon as we release the mouse press, and select again the most recent InputField */
            if (EventSystem.current != null)
            {
                var go = EventSystem.current.currentSelectedGameObject;
                var input_field = go ? go.GetComponent<InputField>() : null;
                if (input_field != null)
                    initialSelection = input_field;
                else if (initialSelection != null)
                    initialSelection.Select();
            }
        }

        float GetPriority(Controller controller)
        {
            float dist = Mathf.Abs(transform.InverseTransformPoint(GetPos(controller)).z);
            return 100 + 1 / (dist + 1) + (controller == prefer_controller ? 2 : 0);
        }

        public void CloseDialog()
        {
            /* used as an event handler for the close button. */
            foreach (var popup_dialog in GetComponentsInParent<IPopupDialogClose>())
                popup_dialog.DoCloseDialog();
        }


        /****************************************************************************************/

        Vector3 grip_origin_position;
        Quaternion origin_rotation;

        public void GripDown(Controller controller)
        {
            origin_rotation = Quaternion.Inverse(controller.rotation) * transform.rotation;
            grip_origin_position = Quaternion.Inverse(transform.rotation) * (transform.position - controller.position);
        }

        public void GripDrag(Controller controller)
        {
            transform.rotation = controller.rotation * origin_rotation;
            transform.position = controller.position + transform.rotation * grip_origin_position;
        }


        /****************************************************************************************/

        class DialogRenderer : MonoBehaviour
        {
            Camera ortho_camera;
            float pixels_per_unit;
            static readonly Vector2[] screen_deltas = new Vector2[]
            {
                new Vector2(0, 0),
                new Vector2(0, 15f),
                new Vector2(0, -15f),
                new Vector2(25f, 0),
                new Vector2(-25f, 0),
            };

            public void PrepareForRendering(float pixels_per_unit)
            {
                RectTransform rtr = transform as RectTransform;
                if (GetComponentInChildren<Collider>() == null)
                {
                    Rect r = rtr.rect;
                    float margin = transform.InverseTransformVector(transform.up * 0.10f).magnitude;
                    float zscale = transform.InverseTransformVector(transform.forward * 0.30f).magnitude;

                    BoxCollider coll = gameObject.AddComponent<BoxCollider>();
                    coll.isTrigger = true;
                    coll.size = new Vector3(r.width + margin, r.height + margin, zscale + margin);
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

                    results.Add(new RaycastResult
                    {
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
}

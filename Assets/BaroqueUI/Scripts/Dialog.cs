﻿using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using UnityEngine.Events;
using UnityEngine.EventSystems;


namespace BaroqueUI
{
    public class Dialog : MonoBehaviour
    {
        [Tooltip("If checked, the dialog box is already placed in world space.  " +
                 "If left unchecked, the dialog box is hidden in a different layer " +
                 "(it can be either in the scene or a prefab) and is used with MakePopup().")]
        public bool alreadyPositioned = false;

        [Tooltip("If checked, the dialog box automatically shows and hides a keyboard (if it has got any InputField).  " +
                 "Otherwise, we assume a keyboard is already connected.")]
        public bool automaticKeyboard = true;

        [Tooltip("Can the user scroll the whole dialog with the scroll wheel?")]
        public bool scrollWholeDialog = false;

        [Tooltip("Can the user activate dialog elements just by touching the touchpad?")]
        public bool touchpadTouchAct = true;

        [Tooltip("For pop-ups, the scale of the dialog box is corrected to this number of units per world space 'meter'.")]
        public float unitsPerMeter = 400;

        [Tooltip("Tweak the materials: set to a length 2 array with the front and back material, or a length 1 array if you want no back.  The front material must support a texture.")]
        public Material[] materials;

        [Tooltip("Background color.  You can use a partially or totally transparent color if you change the front material, e.g. to shader 'Standard' mode 'Transparent'.")]
        public Color backgroundColor = Color.white;

        [Tooltip("A custom RenderTexture.  If specified, this Dialog doesn't show anything but only renders here.  You can then use it as a regular texture somewhere else.  Should have the same width/height as set in the Rect Transform, multiplied by 'Dynamic Pixels Per Unit' if it is not 1.")]
        public RenderTexture renderTexture;

        public void Reset()
        {
            alreadyPositioned = false;
            automaticKeyboard = true;
            scrollWholeDialog = false;
            touchpadTouchAct = true;
            unitsPerMeter = 400;
            materials = DefaultMaterials();
            backgroundColor = Color.white;
            renderTexture = null;
        }


        public void Set<T>(string widget_name, T value, UnityAction<T> onChange = null)
        {
            _Set(widget_name, value, onChange);
        }

        public T Get<T>(string widget_name)
        {
            return (T)_Get(widget_name);
        }

        public void SetClick(string clickable_widget_name, UnityAction onClick)
        {
            _Set(clickable_widget_name, null, onClick);
        }

        public void SetChoices(string choice_widget_name, List<string> choices)
        {
            var descr = FindWidget(choice_widget_name);
            descr.setter_extra(choices);
        }

        public Dialog MakePopup(Controller controller, GameObject requester = null)
        {
            return MakePopup(this, () => Instantiate<Dialog>(this), controller, requester);
        }

        public static Dialog MakePopup(string name_in_scene, Controller controller, GameObject requester = null)
        {
            GameObject gobj = Baroque.FindPossiblyInactive(name_in_scene);
            Dialog dlg = gobj.GetComponent<Dialog>();
            return dlg.MakePopup(controller, requester);
        }

        public delegate Dialog DialogBuilderDelegate();
        static public Dialog MakePopup(object model, DialogBuilderDelegate builder, Controller controller, GameObject requester)
        {
            return ShouldShowPopup(model, requester) ? builder().DoShowPopup(controller) : null;
        }


        /*****************************************************************************************/

        static Dialog popupShown;   /* globally, a single one for now */
        static object popupRequester;
        static int popupCloseTriggerOutside;   /* bitmask, see Update() */

        static bool ShouldShowPopup(object model, GameObject requester)
        {
            /* If 'requester' is not null, use that to identify the owner of the dialog box
             * and hide if called a second time.  If it is null, then use instead 'model'
             * (the dialog template or the Menu instance). */
            object new_requester = (requester != null ? requester : model);
            bool should_hide = false;
            if (popupShown != null && popupShown)
            {
                should_hide = (popupRequester == new_requester);
                if (should_hide)
                    new_requester = null;
                Destroy(popupShown.gameObject);
            }
            popupShown = null;
            popupRequester = new_requester;
            popupCloseTriggerOutside = 0;
            return !should_hide;
        }

        Dialog DoShowPopup(Controller controller)
        {
            Vector3 head_forward = controller.position - Baroque.GetHeadTransform().position;
            Vector3 fw = controller.forward + head_forward.normalized;
            fw.y = 0;
            transform.forward = fw;
            transform.position = controller.position + 0.15f * transform.forward;

            DisplayDialog();
            popupShown = this;
            popupCloseTriggerOutside = 1 << 16;
            return this;
        }


        /*****************************************************************************************/


        /* XXX internals are very Javascript-ish, full of multi-level callbacks.
         * It could also be done using a class hierarchy but it feels like it would be pages longer. */
        delegate object getter_fn();
        delegate void setter_fn(object value);
        delegate void deleter_fn();

        struct WidgetDescr {
            internal getter_fn getter;
            internal setter_fn setter;
            internal deleter_fn detach;
            internal setter_fn attach;
            internal setter_fn setter_extra;
        }

        WidgetDescr FindWidget(string widget_name)
        {
            Transform tr = transform.Find(widget_name);
            if (tr == null)
                throw new MissingComponentException(widget_name);

            Slider slider = tr.GetComponent<Slider>();
            if (slider != null)
                return new WidgetDescr() {
                    getter = () => slider.value,
                    setter = (value) => slider.value = (float)value,
                    detach = () => slider.onValueChanged.RemoveAllListeners(),
                    attach = (callback) => slider.onValueChanged.AddListener((UnityAction<float>)callback),
                };

            Text text = tr.GetComponent<Text>();
            if (text != null)
                return new WidgetDescr() {
                    getter = () => text.text,
                    setter = (value) => text.text = (string)value,
                };

            InputField inputField = tr.GetComponent<InputField>();
            if (inputField != null)
                return new WidgetDescr() {
                    getter = () => inputField.text,
                    setter = (value) => inputField.text = (string)value,
                    detach = () => inputField.onEndEdit.RemoveAllListeners(),
                    attach = (callback) => inputField.onEndEdit.AddListener((UnityAction<string>)callback),
                };

            Button button = tr.GetComponent<Button>();
            if (button != null)
                return new WidgetDescr() {
                    getter = () => null,
                    setter = (ignored) => { },
                    detach = () => button.onClick.RemoveAllListeners(),
                    attach = (callback) => button.onClick.AddListener((UnityAction)callback),
                };

            Toggle toggle = tr.GetComponent<Toggle>();
            if (toggle != null)
                return new WidgetDescr()
                {
                    getter = () => toggle.isOn,
                    setter = (value) => toggle.isOn = (bool)value,
                    detach = () => toggle.onValueChanged.RemoveAllListeners(),
                    attach = (callback) => toggle.onValueChanged.AddListener((UnityAction<bool>)callback),
                };

            Dropdown dropdown = tr.GetComponent<Dropdown>();
            if (dropdown != null)
                return new WidgetDescr()
                {
                    getter = () => dropdown.value,
                    setter = (value) => dropdown.value = (int)value,
                    setter_extra = (value) => { dropdown.ClearOptions(); dropdown.AddOptions(value as List<string>); },
                    detach = () => dropdown.onValueChanged.RemoveAllListeners(),
                    attach = (callback) => dropdown.onValueChanged.AddListener((UnityAction<int>)callback),
                };

            throw new NotImplementedException(widget_name);
        }

        void _Set(string widget_name, object value, object onChange = null)
        {
            var descr = FindWidget(widget_name);
            if (onChange != null)
                descr.detach();
            descr.setter(value);
            if (onChange != null)
                descr.attach(onChange);
        }

        object _Get(string widget_name)
        {
            var descr = FindWidget(widget_name);
            return descr.getter();
        }


        /*******************************************************************************************/

        BackgroundRenderer background_renderer;
        IGlobalControllerTracker global_tracker;

        const int UI_layer = 29;   /* and the next one */
        static bool camera_onprecull_set = false;

        static void CreateLayer()
        {
#if UNITY_EDITOR
            /* when running in the editor, check that UI_layer and UI_layer + 1 are free,
             * and then give them useful names.  This is based on
             * https://forum.unity3d.com/threads/adding-layer-by-script.41970/reply?quote=2274824
             * but with a constant value for UI_layer.  The problem is that this code cannot run
             * outside the editor.  So I can find no reasonable way to dynamically pick an unused
             * layer in builds that run outside the editor...
             */
            UnityEditor.SerializedObject tagManager = new UnityEditor.SerializedObject(
                UnityEditor.AssetDatabase.LoadAllAssetsAtPath("ProjectSettings/TagManager.asset")[0]);
            UnityEditor.SerializedProperty layers = tagManager.FindProperty("layers");
            UnityEditor.SerializedProperty layer29 = layers.GetArrayElementAtIndex(UI_layer);
            UnityEditor.SerializedProperty layer30 = layers.GetArrayElementAtIndex(UI_layer + 1);

            if (layer29.stringValue == "")
            {
                layer29.stringValue = "BaroqueUI dialog";
                tagManager.ApplyModifiedProperties();
            }
            if (layer30.stringValue == "")
            {
                layer30.stringValue = "BaroqueUI dialog rendering";
                tagManager.ApplyModifiedProperties();
            }

            Debug.Assert(layer29.stringValue == "BaroqueUI dialog");
            Debug.Assert(layer30.stringValue == "BaroqueUI dialog rendering");
#endif

            /* we want all cameras to hide these two layers */
            if (!camera_onprecull_set)
            {
                Camera.onPreCull += Camera_OnPreCull;
                camera_onprecull_set = true;
            }
        }

        static void Camera_OnPreCull(Camera cam)
        {
            /* by default, our two dialog layers are hidden in all cameras.  The exception is
             * the special cameras set up below, for which we will compute 'mask == 0'. */
            int mask = cam.cullingMask & ~(3 << UI_layer);
            if (mask != 0 && mask != cam.cullingMask)
                cam.cullingMask = mask;
        }

        public void DisplayDialog()
        {
            transform.localScale = Vector3.one / unitsPerMeter;
            alreadyPositioned = true;
            gameObject.SetActive(true);
            StartCoroutine(CoSetInitialSelection());
        }

        void OnDestroy()
        {
            StopAutomaticKeyboard();
        }

        void RecSetLayer(int layer, bool includeInactive = false)
        {
            foreach (var child in GetComponentsInChildren<Canvas>(includeInactive))
                child.gameObject.layer = layer;
            foreach (var child in GetComponentsInChildren<CanvasRenderer>(includeInactive))
                child.gameObject.layer = layer;
        }

        void UpdateRenderingOnce()
        {
            RecSetLayer(UI_layer + 1);   /* this layer is visible for the camera */

            foreach (var canvas in GetComponentsInChildren<Canvas>())
            {
                var rend = canvas.GetComponent<DialogRenderer>();
                if (rend == null)
                {
                    rend = canvas.gameObject.AddComponent<DialogRenderer>();
                    float pixels_per_unit = GetComponent<CanvasScaler>().dynamicPixelsPerUnit;
                    Material[] mat = materials;
                    if (mat == null || mat.Length == 0)
                        mat = DefaultMaterials();
                    if (mat[0] == null)
                        mat[0] = DefaultMaterials()[0];
                    rend.PrepareForRendering(pixels_per_unit, mat, renderTexture);
                }
                rend.SetBackgroundColor(backgroundColor);
                rend.Render();
            }

            RecSetLayer(UI_layer);   /* this layer is not visible any more */
        }

        Material[] DefaultMaterials()
        {
            return new Material[]
            {
                Resources.Load<Material>("BaroqueUI/Dialog Material"),
                Resources.Load<Material>("BaroqueUI/Dialog Material Back")
            };
        }

        void Start()
        {
            if (!alreadyPositioned)
            {
                gameObject.SetActive(false);
                return;
            }

            CreateLayer();
            RecSetLayer(UI_layer, includeInactive: true);

            foreach (InputField inputField in GetComponentsInChildren<InputField>(includeInactive: true))
            {
                if (inputField.GetComponent<KeyboardVRInput>() == null)
                    inputField.gameObject.AddComponent<KeyboardVRInput>();
            }

            StartAutomaticKeyboard();

            if (renderTexture == null)   /* else, no automatic controllers */
            {
                var ct = Controller.HoverTracker(this);
                ct.computePriority = GetPriority;
                ct.onEnter += OnEnter;
                ct.onMoveOver += MouseMove;
                ct.onLeave += OnLeave;
                ct.onTriggerDown += MouseDown;
                ct.onTriggerDrag += MouseMove;
                ct.onTriggerUp += MouseUp;

                ct.onTouchDown += MouseTouchDown;
                ct.onTouchDrag += MouseTouchMove;
                ct.onTouchUp += MouseTouchUp;
                ct.onTouchPressDown += MousePressDown;
                ct.onTouchPressDrag += MousePressMove;
                ct.onTouchPressUp += MousePressUp;
                ct.onTouchScroll += OnTouchScroll;
            }
            UpdateRenderingOnce();
        }

        private void OnEnable()
        {
            if (background_renderer == null)
            {
                background_renderer = new BackgroundRenderer(UpdateRenderingOnce);

                global_tracker = Controller.GlobalTracker(this);
                global_tracker.onControllersUpdate += Gt_onControllersUpdate;
            }
        }

        private void OnDisable()
        {
            if (background_renderer != null)
            {
                background_renderer.Stop();
                background_renderer = null;
            }
            if (global_tracker != null)
            {
                global_tracker.onControllersUpdate -= Gt_onControllersUpdate;
                global_tracker = null;
            }
        }

        private void Gt_onControllersUpdate(Controller[] controllers)
        {
            BackgroundRenderer.LateUpdate();
        }

        GameObject SetInitialSelection()
        {
            foreach (var selectable in GetComponentsInChildren<Selectable>())
            {
                if (selectable.interactable)
                {
                    if (EventSystem.current != null)   /* XXX should we create it? */
                        EventSystem.current.SetSelectedGameObject(selectable.gameObject, null);
                    selectable.Select();
                    return selectable.gameObject;
                }
            }
            return null;
        }

        IEnumerator CoSetInitialSelection()
        {
            yield return new WaitForEndOfFrame();   /* doesn't work if done immediately :-( */
            yield return new WaitForFixedUpdate();
            if (this && isActiveAndEnabled)
                SetInitialSelection();
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
            background_renderer.inForeground = true;
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
            UpdateCursor(controller);
        }

        void OnTouchScroll(Controller controller, Vector2 position_difference)
        {
            bool in_bounds = UpdateCurrentPoint(controller.position);
            if (in_bounds)
            {
                const float TOUCHPAD_SCROLL_FACTOR = 100f;
                const float TOUCHPAD_SCROLL_SPACE_EDGE = 0.04f;

                GameObject target = pevent.pointerCurrentRaycast.gameObject;
                pevent.scrollDelta = position_difference * TOUCHPAD_SCROLL_FACTOR;
                //Debug.Log("scroll: " + pevent.scrollDelta.x + " " + pevent.scrollDelta.y);
                GameObject go = ExecuteEvents.ExecuteHierarchy(target, pevent, ExecuteEvents.scrollHandler);
                //Debug.Log("scroll: sent to " + target.name + ", result = " + (go == null ? "NULL" : go.name));

                if (go == null && scrollWholeDialog)
                {
                    /* not sent anywhere.  Move the whole dialog box around, if we're not close to the edge */
                    float delta_x = pevent.scrollDelta.x > 0 ? -TOUCHPAD_SCROLL_SPACE_EDGE : TOUCHPAD_SCROLL_SPACE_EDGE;
                    float delta_y = pevent.scrollDelta.y > 0 ? -TOUCHPAD_SCROLL_SPACE_EDGE : TOUCHPAD_SCROLL_SPACE_EDGE;

                    if (UpdateCurrentPoint(controller.position - delta_x * transform.right) &&
                        UpdateCurrentPoint(controller.position - delta_y * transform.up))
                    {
                        transform.position -= (pevent.scrollDelta.x * transform.right + pevent.scrollDelta.y * transform.up)
                            / unitsPerMeter;
                    }
                }
            }
            controller.SetPointer("");
        }

        void UpdateCursor(Controller controller)
        {
            float distance_to_plane = transform.InverseTransformPoint(controller.position).z * -0.06f;
            if (distance_to_plane < 1)
                distance_to_plane = 1;
            Transform tr = controller.SetPointer("Cursor");
            tr.rotation = transform.rotation;
            tr.localScale = new Vector3(1, 1, distance_to_plane);
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
            background_renderer.inForeground = false;
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

        void MouseTouchDown(Controller controller)
        {
            if (touchpadTouchAct)
                MouseDown(controller);
        }

        void MouseTouchMove(Controller controller)
        {
            if (touchpadTouchAct)
                MouseMove(controller);
        }

        void MouseTouchUp(Controller controller)
        {
            if (touchpadTouchAct)
                MouseUp(controller);
        }

        void MousePressDown(Controller controller)
        {
            if (!touchpadTouchAct)
                MouseDown(controller);
        }

        void MousePressMove(Controller controller)
        {
            if (!touchpadTouchAct)
                MouseMove(controller);
        }

        void MousePressUp(Controller controller)
        {
            if (!touchpadTouchAct)
                MouseUp(controller);
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


        /****************************************************************************************/

        private void Update()
        {
            /* bits of popupCloseTriggerOutside:
                    bit 16: active
                    bits 0 or 1: trigger is up
                    bits 8 or 9: controller was outside the whole time trigger was down 
             */
            if (popupShown == null || !popupShown)
                popupCloseTriggerOutside = 0;

            if (popupCloseTriggerOutside != 0)
            {
                int i = 0;
                foreach (var ctrl in Baroque.GetControllers())
                {
                    if (ctrl.CurrentHoverTracker() == this)
                        popupCloseTriggerOutside &= ~(0x0100 << i);   /* remove bit 8/9: we are inside */

                    if (ctrl.GetButton(EControllerButton.Trigger))
                    {
                        if ((popupCloseTriggerOutside & (0x01 << i)) != 0)    /* was up, pressing just now */
                        {
                            popupCloseTriggerOutside &= ~(0x01 << i);         /* remove bit 0/1: we are down */
                            if (ctrl.CurrentHoverTracker() != this)
                                popupCloseTriggerOutside |= (0x0100 << i);    /* set bit 8/9 if we are outside */
                        }
                    }
                    else
                    {
                        if ((popupCloseTriggerOutside & (0x01 << i)) == 0)    /* was down, releasing just now */
                        {
                            popupCloseTriggerOutside |= (0x01 << i);

                            if ((popupCloseTriggerOutside & (0x0100 << i)) != 0)    /* was outside the whole time trigger was down */
                            {
                                /* we tracked a press and a release entirely outside, so close the dialog */
                                Destroy(popupShown.gameObject);
                            }
                        }
                    }
                    i += 1;
                }
            }
        }


        /****************************************************************************************/

        KeyboardClicker auto_keyboard;

        void StartAutomaticKeyboard()
        {
            if (automaticKeyboard && GetComponentInChildren<KeyboardVRInput>() != null && auto_keyboard == null)
            {
                GameObject keyboard_prefab = Resources.Load<GameObject>("BaroqueUI/Keyboard");
                RectTransform rtr = transform as RectTransform;
                Vector3 vcorr = rtr.TransformVector(new Vector3(0, rtr.rect.height * rtr.pivot.y, 0));

                Vector3 pos = transform.position - 0.08f * transform.forward - 0.09f * transform.up - vcorr;
                Quaternion rotation = Quaternion.LookRotation(transform.forward);
                rotation = Quaternion.Euler(41.6335f, rotation.eulerAngles.y, 0);
                // note: 41.6335 = atan(0.08/0.09)
                auto_keyboard = Instantiate(keyboard_prefab, pos, rotation).GetComponent<KeyboardClicker>();
                auto_keyboard.enableEnterKey = true;
                auto_keyboard.enableTabKey = false;
                auto_keyboard.enableEscKey = true;

                auto_keyboard.onKeyboardTyping.AddListener(KeyboardTyping);
            }
        }
        void StopAutomaticKeyboard()
        {
            if (auto_keyboard != null && auto_keyboard)
            {
                Destroy(auto_keyboard.gameObject);
            }
            auto_keyboard = null;
        }

        public void KeyboardTyping(KeyboardClicker.EKeyState state, string key)
        {
            GameObject gobj = EventSystem.current.currentSelectedGameObject;
            if (gobj != null && gobj && gobj.transform.IsChildOf(transform))
            {
                /* the selection is already inside this dialog */
            }
            else
            {
                gobj = SetInitialSelection();
                if (gobj == null)
                    return;
            }
            var keyboardVrInput = gobj.GetComponent<KeyboardVRInput>();
            if (keyboardVrInput != null)
                keyboardVrInput.KeyboardTyping(state, key);
        }


        class DialogRenderer : MonoBehaviour
        {
            RenderTexture render_texture;
            bool own_render_texture;
            Camera ortho_camera;
            Transform quad, back_quad;
            float pixels_per_unit;

            public void PrepareForRendering(float pixels_per_unit, Material[] materials, RenderTexture renderTexture)
            {
                RectTransform rtr = transform as RectTransform;
                if (GetComponentInChildren<Collider>() == null)
                {
                    Rect r = rtr.rect;
                    float zscale = transform.InverseTransformVector(transform.forward * 0.108f).magnitude;

                    BoxCollider coll = gameObject.AddComponent<BoxCollider>();
                    coll.isTrigger = true;
                    coll.size = new Vector3(r.width, r.height, zscale);
                    coll.center = new Vector3(r.center.x, r.center.y, zscale * -0.3125f);
                }

                this.pixels_per_unit = pixels_per_unit;
                own_render_texture = (renderTexture == null);
                if (own_render_texture)
                    render_texture = new RenderTexture((int)(rtr.rect.width * pixels_per_unit + 0.5),
                                                       (int)(rtr.rect.height * pixels_per_unit + 0.5), 32);
                else
                    render_texture = renderTexture;
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
                ortho_camera.cullingMask = 2 << Dialog.UI_layer;
                ortho_camera.orthographic = true;
                ortho_camera.orthographicSize = rtr.TransformVector(0, rtr.rect.height * 0.5f, 0).magnitude;
                ortho_camera.nearClipPlane = -10;
                ortho_camera.farClipPlane = 10;
                ortho_camera.targetTexture = render_texture;

                if (own_render_texture)
                {
                    quad = GameObject.CreatePrimitive(PrimitiveType.Quad).transform;
                    quad.SetParent(transform);
                    quad.position = ortho_camera.transform.position;
                    quad.rotation = ortho_camera.transform.rotation;
                    quad.localScale = new Vector3(rtr.rect.width, rtr.rect.height, 1);
                    quad.gameObject.layer = 0;   /* default */
                    DestroyImmediate(quad.GetComponent<Collider>());

                    quad.GetComponent<MeshRenderer>().material = materials[0];
                    quad.GetComponent<MeshRenderer>().material.SetTexture("_MainTex", render_texture);
                }
                if (own_render_texture && materials.Length >= 2 && materials[1] != null)
                {
                    back_quad = GameObject.CreatePrimitive(PrimitiveType.Quad).transform;
                    back_quad.SetParent(transform);
                    back_quad.position = ortho_camera.transform.position;
                    back_quad.rotation = ortho_camera.transform.rotation * Quaternion.LookRotation(new Vector3(0, 0, -1));
                    back_quad.localScale = new Vector3(rtr.rect.width, rtr.rect.height, 1);
                    back_quad.gameObject.layer = 0;   /* default */
                    DestroyImmediate(back_quad.GetComponent<Collider>());

                    back_quad.GetComponent<MeshRenderer>().material = materials[1];
                }

                GetComponent<Canvas>().worldCamera = ortho_camera;
            }

            internal void SetBackgroundColor(Color backgroundColor)
            {
                ortho_camera.backgroundColor = backgroundColor;
            }

            private void OnDestroy()
            {
                if (own_render_texture)
                    render_texture.Release();
            }

            public void Render()
            {
                render_texture.DiscardContents();
                ortho_camera.Render();
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
                    if (!RectTransformUtility.RectangleContainsScreenPoint(graphic.rectTransform, screen_point, ortho_camera))
                        continue;
                    if (!graphic.Raycast(screen_point, ortho_camera))
                        continue;

                    results.Add(new RaycastResult {
                        gameObject = graphic.gameObject,
                        module = GetComponent<GraphicRaycaster>(),
                        index = results.Count,
                        depth = graphic.depth,
                        sortingLayer = canvas.sortingLayerID,
                        sortingOrder = canvas.sortingOrder,
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

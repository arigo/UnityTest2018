using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.SceneManagement;


namespace BaroqueUI
{
    abstract class BaroqueVRManager
    {
        internal class ControllerState
        {
            internal float trigger_variable_pressure;    /* 0.0 - 1.0 */
            internal float grip_variable_pressure;       /* 0.0 - 1.0; on Vive, either 0 or 1 */
            internal Vector2 touchpad_position;          /* or joystick on Oculus Touch */

            internal bool trigger_pressed;
            internal bool grip_pressed;
            internal bool touchpad_touched;              /* Vive only */
            internal bool touchpad_pressed;              /* on Oculus, this is the A button */
            internal bool menu_pressed;                  /* on Oculus, this is the B button */
        }
        protected ControllerState[] ctrl_states;

        internal Transform head_transform, camerarig_transform;
        internal Controller.Model controller_model;

        internal abstract void InitializeControllers();
        internal abstract bool GetControllerTransforms(out Transform left_tr, out Transform right_tr);
        internal abstract bool UpdateControllerState(Controller ctrl, ControllerState ctrl_state);
        internal abstract void HapticPulse(Controller ctrl, int durationMicroSec);
        internal abstract void FadeToColor(Color screen_color, float time);
        internal abstract Vector2 GetPlayAreaSize();
        internal abstract string GetStartupErrorMessage();

        internal ControllerState NewControllerState(Controller ctrl)
        {
            return ctrl.GetAdditionalData(ref ctrl_states);
        }

        protected GameObject LoadCameraRig(string name)
        {
            /* XXX! This resource is not part of BaroqueUI because it contains project-specific camera
             * configuration.  Refactor!
             */
            GameObject rig = UnityEngine.Object.FindObjectOfType<SteamVR_ControllerManager>().gameObject;
            if (rig == null)
            {
                rig = Resources.Load<GameObject>("CameraRigs/" + name);
                Debug.Assert(rig != null);
                rig = UnityEngine.Object.Instantiate(rig);
                //UnityEngine.Object.DontDestroyOnLoad(rig);   XXX
            }
            return rig;
        }
    }


    class Baroque_SteamVRManager : BaroqueVRManager
    {
        Valve.VR.VRControllerState_t controllerState;
        SteamVR_ControllerManager steamvr_manager;

        internal Baroque_SteamVRManager()
        {
            GameObject rig = LoadCameraRig("[CameraRig]");

            steamvr_manager = rig.GetComponent<SteamVR_ControllerManager>();
            Debug.Assert(steamvr_manager != null);

            SteamVR_Camera camera = steamvr_manager.GetComponentInChildren<SteamVR_Camera>();
            Debug.Assert(camera != null);
            head_transform = camera.transform;
            camerarig_transform = steamvr_manager.transform;

#if UNITY_2017_2_OR_NEWER
            var device = UnityEngine.XR.XRDevice.model;
#else
            var device = UnityEngine.VR.VRDevice.model;
#endif
            if (device.ToLower().StartsWith("vive"))
                controller_model = Controller.Model.HTCVive;
            else
                controller_model = Controller.Model.Other;
        }

        internal override void InitializeControllers()
        {
            SteamVR_Events.NewPosesApplied.AddListener(Baroque._OnNewPosesApplied);
        }

        internal override bool GetControllerTransforms(out Transform left_tr, out Transform right_tr)
        {
            left_tr = steamvr_manager.left.transform;
            right_tr = steamvr_manager.right.transform;
            return true;
        }

        static uint TrackedObjectIndex(Controller ctrl)
        {
            var trackedObject = ctrl.GetComponent<SteamVR_TrackedObject>();
            if (trackedObject == null)
                throw new MissingComponentException("'[CameraRig]/" + ctrl.name + "' gameobject is missing a SteamVR_TrackedObject component");
            return (uint)trackedObject.index;
        }

        internal override bool UpdateControllerState(Controller ctrl, ControllerState ctrl_state)
        {
            var system = Valve.VR.OpenVR.System;
            if (system == null || !ctrl.isActiveAndEnabled)
                return false;
            if (!system.GetControllerState(TrackedObjectIndex(ctrl), ref controllerState,
                                           (uint)System.Runtime.InteropServices.Marshal.SizeOf(typeof(Valve.VR.VRControllerState_t))))
                return false;

            ctrl_state.trigger_pressed = (controllerState.ulButtonPressed & (1UL << ((int)Valve.VR.EVRButtonId.k_EButton_SteamVR_Trigger))) != 0;
            ctrl_state.grip_pressed = (controllerState.ulButtonPressed & (1UL << ((int)Valve.VR.EVRButtonId.k_EButton_Grip))) != 0;
            ctrl_state.touchpad_touched = (controllerState.ulButtonTouched & (1UL << ((int)Valve.VR.EVRButtonId.k_EButton_SteamVR_Touchpad))) != 0;
            ctrl_state.touchpad_pressed = (controllerState.ulButtonPressed & (1UL << ((int)Valve.VR.EVRButtonId.k_EButton_SteamVR_Touchpad))) != 0;
            ctrl_state.menu_pressed = (controllerState.ulButtonPressed & (1UL << ((int)Valve.VR.EVRButtonId.k_EButton_ApplicationMenu))) != 0;

            ctrl_state.trigger_variable_pressure = controllerState.rAxis1.x;
            ctrl_state.grip_variable_pressure = ctrl_state.grip_pressed ? 1 : 0;
            ctrl_state.touchpad_position = new Vector2(controllerState.rAxis0.x, controllerState.rAxis0.y);

            return true;
        }

        internal override void HapticPulse(Controller ctrl, int durationMicroSec)
        {
            SteamVR_Controller.Input((int)TrackedObjectIndex(ctrl)).TriggerHapticPulse((ushort)durationMicroSec);
        }

        internal override void FadeToColor(Color screen_color, float time)
        {
            var compositor = Valve.VR.OpenVR.Compositor;
            if (compositor != null)
                compositor.FadeToColor(time, screen_color.r, screen_color.g, screen_color.b, screen_color.a, false);
        }

        internal override Vector2 GetPlayAreaSize()
        {
            var rect = new Valve.VR.HmdQuad_t();
            if (!SteamVR_PlayArea.GetBounds(SteamVR_PlayArea.Size.Calibrated, ref rect))
                return Vector2.zero;

            return new Vector2(Mathf.Abs(rect.vCorners0.v0 - rect.vCorners2.v0),
                               Mathf.Abs(rect.vCorners0.v2 - rect.vCorners2.v2));
        }

        internal override string GetStartupErrorMessage()
        {
            return ("Move the VR headset to activate it\n\n" +
                    "Make sure the lighthouses are turned on,\n" +
                    "and SteamVR is installed");
        }
    }


#if USE_OCULUS
    class Baroque_OculusVRManager : BaroqueVRManager
    {
        OVRCameraRig camera_rig;
        OvrAvatar avatar;
        OVRHapticsClip haptics_clip;
        GameObject screen_fade;
        Material screen_fade_material;
        Coroutine screen_fade_coro;
        Texture controller_albedo_with_mask;

        class ComponentColors
        {
            internal Texture2D tex;
            internal Color32[] cols;
            internal bool modified;
            internal Texture2D Apply()
            {
                if (modified) { tex.SetPixels32(cols); tex.Apply(); modified = false; }
                return tex;
            }
        }
        ComponentColors[] component_colors;


        internal Baroque_OculusVRManager()
        {
            GameObject rig = LoadCameraRig("OVRCameraRig");

            camera_rig = rig.GetComponent<OVRCameraRig>();
            Debug.Assert(camera_rig != null);

            avatar = rig.GetComponentInChildren<OvrAvatar>();
            Debug.Assert(avatar != null);

            controller_model = Controller.Model.OculusTouch;
            camera_rig.EnsureGameObjectIntegrity();
            head_transform = camera_rig.centerEyeAnchor;
            camerarig_transform = camera_rig.transform;

            controller_albedo_with_mask = Resources.Load<Texture>("BaroqueUI/ControllerOculusTouch/AlbedoWithMask");
            screen_fade_material = new Material(Resources.Load<Material>("BaroqueUI/Manual Fade Material"));
        }

        internal override void InitializeControllers()
        {
            /* Register at the same time as SteamVR would.  It's the latest moment; the "onPreCull"
             * event is sent later but the dialog boxes are already placed in world space, and
             * thus would appear at the previous frame's location. */
            Application.onBeforeRender += OnBeforeRender;
        }

        internal override bool GetControllerTransforms(out Transform left_tr, out Transform right_tr)
        {
            left_tr = camera_rig.leftHandAnchor;
            right_tr = camera_rig.rightHandAnchor;
            return true;
        }

        internal override bool UpdateControllerState(Controller ctrl, ControllerState ctrl_state)
        {
            var pose = avatar.Driver.GetCurrentPose();
            OvrAvatarDriver.ControllerPose ctrl_pose;
            switch (ctrl.index)
            {
                case 0: ctrl_pose = pose.controllerLeftPose; break;
                case 1: ctrl_pose = pose.controllerRightPose; break;
                default: return false;
            }
            if (!ctrl_pose.isActive)
                return false;

            ctrl_state.trigger_variable_pressure = ctrl_pose.indexTrigger;
            ctrl_state.grip_variable_pressure = ctrl_pose.handTrigger;
            ctrl_state.touchpad_position = ctrl_pose.joystickPosition;

            ctrl_state.trigger_pressed = ctrl_state.trigger_variable_pressure > 0.6f;
            ctrl_state.grip_pressed = ctrl_state.grip_variable_pressure > 0.8f;
            ctrl_state.touchpad_pressed = (ctrl_pose.buttons & ovrAvatarButton.One) != 0;
            ctrl_state.menu_pressed = (ctrl_pose.buttons & ovrAvatarButton.Two) != 0;
            return true;
        }

        private void OnBeforeRender()
        {
            if (camera_rig)
            {
                /* hide the ghost hands, only show the controllers */
                Action<OvrAvatarHand> disable_mesh_renderer = (hand) =>
                {
                    var skinned_mesh_render = hand.GetComponentInChildren<SkinnedMeshRenderer>();
                    if (skinned_mesh_render != null)
                        skinned_mesh_render.enabled = false;
                };
                disable_mesh_renderer(avatar.HandLeft);
                disable_mesh_renderer(avatar.HandRight);

                /* invoke the main processing loop for BaroqueUI.Controller */
                Baroque._OnNewPosesApplied();

                /* We don't need transparent controllers, but we want them to combine
                 * correctly with transparent pointers, for example, so make them opaque.
                 * Also tweak the texture to be AlbedoWithMask instead of the standard one:
                 * it has only the Green component relevant for the albedo; the Red and Blue
                 * components are used to identify various parts of the controller, and we
                 * use them as (u, v) into the _Components 4x4 texture.  For example, the
                 * border of the B button is drawn with Red = 0 and Blue = 1/3.  When
                 * mapping the coordinates (0, 1/3) in the 4x4 texture, we arrive at the
                 * 4th pixel, in the list of all 16 pixels.  That's why
                 * EColorizableComponent.MenuBorder = 4.
                 */
                foreach (var ctrl in Baroque.GetControllers())
                {
                    MonoBehaviour avatar_ctrl;
                    switch (ctrl.index)
                    {
                        case 0: avatar_ctrl = avatar.ControllerLeft; break;
                        case 1: avatar_ctrl = avatar.ControllerRight; break;
                        default: continue;
                    }
                    var skinned_mesh_render = avatar_ctrl.GetComponentInChildren<SkinnedMeshRenderer>();
                    if (skinned_mesh_render == null)
                        continue;
                    var mat = skinned_mesh_render.material;
                    mat.renderQueue = 2400;
                    if (Baroque._oculus_touch_controllers_shader != null && mat.shader != Baroque._oculus_touch_controllers_shader)
                        mat.shader = Baroque._oculus_touch_controllers_shader;

                    mat.SetTexture("_Albedo", controller_albedo_with_mask);
                    mat.SetTexture("_Components", GetComponentColors(ctrl).Apply());
                };
            }
        }

        ComponentColors GetComponentColors(Controller ctrl)
        {
            ComponentColors cc = ctrl.GetAdditionalData(ref component_colors);
            if (cc.tex == null)
            {
                cc.tex = new Texture2D(4, 4, TextureFormat.RGB24, mipmap: false, linear: false);
                cc.tex.wrapMode = TextureWrapMode.Clamp;
                cc.tex.filterMode = FilterMode.Point;
                cc.cols = new Color32[16];
                cc.modified = true;
            }
            return cc;
        }

        internal void SetComponentColor(Controller ctrl, int component, Color color)
        {
            Color32 col32 = (Color32)color;
            var cc = GetComponentColors(ctrl);
            Color32 src = cc.cols[component];
            if (src.r != col32.r || src.g != col32.g || src.b != col32.b)
            {
                cc.cols[component] = col32;
                cc.modified = true;
            }
        }

        internal override void HapticPulse(Controller ctrl, int durationMicroSec)
        {
            /* mapping from durationMicroSec, which is a value up to ~500, down to bytes */
            if (haptics_clip == null)
                haptics_clip = new OVRHapticsClip(1);

            int value = durationMicroSec / 2;
            if (value > 255) value = 255;

            haptics_clip.Reset();
            haptics_clip.WriteSample((byte)value);

            OVRHaptics.OVRHapticsChannel channel = OVRHaptics.Channels[ctrl.index];
            channel.Preempt(haptics_clip);
        }

        internal override void FadeToColor(Color screen_color, float time)
        {
            if (screen_fade == null || !screen_fade)
            {
                screen_fade = GameObject.CreatePrimitive(PrimitiveType.Quad);
                screen_fade.name = "screen fade quad";
                screen_fade.transform.SetParent(camera_rig.centerEyeAnchor);
                screen_fade.transform.localPosition = new Vector3(0, 0, 1);
                screen_fade.transform.localRotation = Quaternion.identity;
                screen_fade.transform.localScale = 10 * Vector3.one;

                screen_fade_material.color = new Color(0, 0, 0, 0);
                screen_fade.GetComponent<Renderer>().sharedMaterial = screen_fade_material;
            }
            if (screen_fade_coro != null)
                camera_rig.StopCoroutine(screen_fade_coro);
            screen_fade_coro = camera_rig.StartCoroutine(_Fading(screen_color, Time.time, time));
        }

        IEnumerator _Fading(Color target_color, float last_time, float delay)
        {
            screen_fade.SetActive(true);
            Color current_color = screen_fade_material.color;
            while (delay > 0f)
            {
                float elapsed_time = Time.time - last_time;
                current_color = Color.Lerp(current_color, target_color, elapsed_time / delay);
                screen_fade_material.color = current_color;
                delay -= elapsed_time;
                yield return new WaitForEndOfFrame();
            }
            screen_fade_material.color = target_color;
            if (target_color.a == 0)
                screen_fade.SetActive(false);
        }

        internal override Vector2 GetPlayAreaSize()
        {
            var boundary = OVRManager.boundary;
            if (boundary == null)
                return Vector2.zero;

            Vector3 dim = boundary.GetDimensions(OVRBoundary.BoundaryType.PlayArea);
            return new Vector2(dim.x, dim.z);
        }

        internal override string GetStartupErrorMessage()
        {
            return ("Move the Oculus headset to activate it\n\n" +
                    "Make sure the sensors are turned on");
        }
    }
#endif


    class Baroque_MissingManager : BaroqueVRManager
    {
        internal Baroque_MissingManager()
        {
            var go = new GameObject("MissingVR");
            head_transform = go.transform;
            camerarig_transform = go.transform;
        }

        internal override void FadeToColor(Color screen_color, float time) { }
        internal override Vector2 GetPlayAreaSize() { return Vector2.zero; }
        internal override void HapticPulse(Controller ctrl, int durationMicroSec) { }
        internal override void InitializeControllers() { }

        internal override bool GetControllerTransforms(out Transform left_tr, out Transform right_tr)
        {
            left_tr = null;
            right_tr = null;
            return false;
        }

        internal override string GetStartupErrorMessage()
        {
            return "No compatible VR headset detected.\n\nConnect it now and restart this program to try again.";
        }

        internal override bool UpdateControllerState(Controller ctrl, ControllerState ctrl_state)
        {
            throw new NotImplementedException();
        }
    }


    public static class Baroque
    {
        static BaroqueVRManager baroque_vr_manager;
        public static Shader _oculus_touch_controllers_shader;   // XXXX

        static BaroqueVRManager GetBaroqueVRManager()
        {
            if (baroque_vr_manager == null && !_TryLoadBaroqueVRManager())
                baroque_vr_manager = new Baroque_MissingManager();
            return baroque_vr_manager;
        }

        static bool _TryLoadBaroqueVRManager()
        {
#if UNITY_2017_2_OR_NEWER
            var loaded_device = UnityEngine.XR.XRSettings.loadedDeviceName;
#else
            var loaded_device = UnityEngine.VR.VRSettings.loadedDeviceName;
#endif
            switch (loaded_device)
            {
                case "OpenVR":
                    baroque_vr_manager = new Baroque_SteamVRManager();
                    return true;

#if USE_OCULUS
                case "Oculus":
                    baroque_vr_manager = new Baroque_OculusVRManager();
                    return true;
#endif

                default:
                    if (!string.IsNullOrEmpty(loaded_device))
                        Debug.LogError("Headset device name unsupported: " + loaded_device);
                    return false;
            }
        }

        static public Transform GetHeadTransform()
        {
            return GetBaroqueVRManager().head_transform;
        }

        static public Transform GetCameraRigTransform()
        {
            return GetBaroqueVRManager().camerarig_transform;
        }

        static public Controller[] GetControllers()
        {
            _EnsureStarted();
            return controllers;
        }

        static public void FadeToColor(Color screen_color, float time)
        {
            GetBaroqueVRManager().FadeToColor(screen_color, time);
        }

        static public bool TryGetPlayAreaSize(out Vector2 size)
        {
            size = GetBaroqueVRManager().GetPlayAreaSize();
            return (size.x > 0 && size.y > 0);
        }

        static public string GetStartupErrorMessage()
        {
            return GetBaroqueVRManager().GetStartupErrorMessage();
        }

        static public GameObject FindPossiblyInactive(string path_in_scene)
        {
            Transform tr = null;
            foreach (var name in path_in_scene.Split('/'))
            {
                if (name == "")
                    continue;
                if (tr == null)
                {
                    foreach (var gobj in SceneManager.GetActiveScene().GetRootGameObjects())
                    {
                        if (gobj.name == name)
                        {
                            tr = gobj.transform;
                            break;
                        }
                    }
                }
                else
                {
                    tr = tr.Find(name);
                }
                if (tr == null)
                    throw new System.Exception("gameobject not found: '" + path_in_scene + "'");
            }
            return tr.gameObject;
        }


        /*********************************************************************************************/


        static GameObject drawings;

        public static void DrawLine(Vector3 from, Vector3 to)
        {
            if (Application.isPlaying)
                MakeLine(from, to);
        }

        public static void DrawLine(Vector3 from, Vector3 to, Color color)
        {
            if (Application.isPlaying)
            {
                GameObject go = MakeLine(from, to);
                go.GetComponent<Renderer>().material.color = color;
            }
        }

        static GameObject MakeLine(Vector3 from, Vector3 to)
        {
            if (drawings == null)
                drawings = new GameObject("BaroqueUI drawings");

            var go = GameObject.CreatePrimitive(PrimitiveType.Cube);
            Collider.DestroyImmediate(go.GetComponent<Collider>());
            go.transform.SetParent(drawings.transform);
            go.transform.position = (from + to) * 0.5f;
            go.transform.localScale = new Vector3(0.005f, 0.005f, Vector3.Distance(from, to));
            go.transform.rotation = Quaternion.LookRotation(to - from);
            return go;
        }

        static void RemoveDrawings()
        {
            if (drawings != null)
                GameObject.Destroy(drawings);
            drawings = null;
        }


        /*********************************************************************************************/

        static bool controllersReady, globallyReady;
        static GameObject left_controller, right_controller;
        static Controller[] controllers;

        static GameObject InitController(Transform tr, int index)
        {
            Controller ctrl = tr.GetComponent<Controller>();
            if (ctrl == null)
                ctrl = tr.gameObject.AddComponent<Controller>();
            ctrl._Initialize(GetBaroqueVRManager(), index);
            controllers[index] = ctrl;
            return tr.gameObject;
        }

        static void InitControllers()
        {
            if (!controllersReady)
            {
#if UNITY_5_6
                /* hack hack hack for SteamVR < 1.2.2 on Unity 5.6 */
                if (typeof(SteamVR_UpdatePoses).GetMethod("OnPreCull",
                    System.Reflection.BindingFlags.Instance |
                    System.Reflection.BindingFlags.Public |
                    System.Reflection.BindingFlags.NonPublic) != null)
                {
                    if (GetHeadTransform().GetComponent<SteamVR_UpdatePoses>() == null)
                        GetHeadTransform().gameObject.AddComponent<SteamVR_UpdatePoses>();
                }
#endif
                Controller._InitControllers();

                Transform left_tr, right_tr;
                if (!GetBaroqueVRManager().GetControllerTransforms(out left_tr, out right_tr))
                {
                    controllers = new Controller[0];
                    return;
                }
                controllers = new Controller[2];
                left_controller = InitController(left_tr, 0);
                right_controller = InitController(right_tr, 1);
                controllersReady = true;

                if (!globallyReady)
                {
                    GetBaroqueVRManager().InitializeControllers();

                    SceneManager.sceneUnloaded += (scene) => { controllersReady = false; };
                    globallyReady = true;
                }
            }
            else
            {
                /* this occurs during scene unloading, when the controller objects have already
                 * been destroyed.  Make an empty list in this case. */
                controllers = new Controller[0];
            }
        }

        static internal void _EnsureStarted()
        {
            if (left_controller == null || right_controller == null)   // includes 'has been destroyed'
                InitControllers();
        }

        internal static void _OnNewPosesApplied()
        {
            Controller[] controllers = GetControllers();
            RemoveDrawings();
            if (controllers.Length > 0)
                Controller._UpdateAllControllers(controllers);
        }


        /*********************************************************************************************/

        static internal void _InitTests()
        {
            controllersReady = false;
            globallyReady = true;
            InitControllers();
        }
    }
}

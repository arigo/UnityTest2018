using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using BaroqueUI;


public class OptionPanelShower : MonoBehaviour
{
    public OptionPanel panelPrefab;


    void Start()
    {
        foreach (var ctrl in Baroque.GetControllers())
            StartCoroutine(Run(ctrl));
    }

    IEnumerator Run(Controller ctrl)
    {
        OptionPanel panel = null;
        while (true)
        {
            RemovePanel(ref panel);
            yield return null;

            /* wait until we touch the touchpad */
            while (ctrl.isActiveAndEnabled && ctrl.touchpadTouched)
            {
                RemovePanel(ref panel);

                /* track moves */
                Vector2 origin = ctrl.touchpadPosition;
                Vector2 current_delta = Vector2.zero;
                while (true)
                {
                    yield return null;

                    if (!ctrl.isActiveAndEnabled || !ctrl.touchpadTouched)
                        break;

                    current_delta = ctrl.touchpadPosition - origin;
                    ShowAtCurrentDelta(ref panel, ctrl, current_delta);
                }

                if (panel == null || current_delta.sqrMagnitude < 0.4f)
                    break;

                ShowAtCurrentDelta(ref panel, ctrl, current_delta * 16f);

                /* wait until the next time we touch the touchpad */
                while (ctrl.isActiveAndEnabled && !ctrl.touchpadTouched)
                    yield return null;
            }
        }
    }

    void RemovePanel(ref OptionPanel panel)
    {
        if (panel != null)
        {
            Destroy(panel.gameObject);
            panel = null;
        }
    }

    void ShowAtCurrentDelta(ref OptionPanel panel, Controller controller, Vector2 delta)
    {
        float fraction = delta.sqrMagnitude;
        if (fraction < 0.01f)
        {
            RemovePanel(ref panel);
            return;
        }

        if (fraction >= 1f)
        {
            delta = delta.normalized;
            fraction = 1f;
        }

        if (panel == null)
            panel = Instantiate(panelPrefab);

        const float DISTANCE_MAX = 0.2f;
        Vector3 position = controller.position +
            controller.right * delta.x * DISTANCE_MAX +
            controller.up * delta.y * DISTANCE_MAX;

        Transform headset = Baroque.GetHeadTransform();
        Quaternion rot1 = controller.rotation;
        Quaternion rot2 = Quaternion.LookRotation(controller.position - headset.position);
        Quaternion rot3 = Quaternion.Lerp(rot1, rot2, fraction * 0.5f);

        Vector3 euler_angles = rot3.eulerAngles;
        euler_angles.z = 0;
        Vector3 up1 = Quaternion.Euler(euler_angles) * Vector3.up;
        if (Vector3.Dot(Baroque.GetHeadTransform().up, up1) < 0)
            euler_angles.z = 180;
        Quaternion rotation = Quaternion.Lerp(controller.rotation, Quaternion.Euler(euler_angles), fraction);

        panel.transform.SetPositionAndRotation(position, rotation);

        panel.transform.localScale = Vector3.one * fraction;

        panel.SetOpacity(fraction);
        panel.ActivateInteractions(fraction >= 1f);
    }
}

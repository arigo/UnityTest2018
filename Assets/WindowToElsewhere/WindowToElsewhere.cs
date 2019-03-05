using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.XR;
using BaroqueUI;


public class WindowToElsewhere : MonoBehaviour
{
    public Camera secondCamera;

    RenderTexture rt_left, rt_right;


    /* NOTE: clean code, but only works if Edit -> Project Settings -> Player -> XR Settings ->
     * Stereo Rendering Method is set to Multi Pass.
     */

    private void OnWillRenderObject()
    {
        if (rt_right == null)
        {
            rt_left = new RenderTexture(XRSettings.eyeTextureDesc);
            rt_right = new RenderTexture(XRSettings.eyeTextureDesc);
            GetComponent<Renderer>().material.SetTexture("_LeftTex", rt_left);
            GetComponent<Renderer>().material.SetTexture("_RightTex", rt_right);
            GetComponent<Renderer>().material.SetFloat("_XAxis", 1f);

            var cam = Baroque.GetHeadTransform().GetComponent<Camera>();
            secondCamera.targetTexture = rt_left;
            secondCamera.fieldOfView = cam.fieldOfView;
            secondCamera.farClipPlane = cam.farClipPlane;
            secondCamera.nearClipPlane = cam.nearClipPlane;
        }

        Vector3 sc_pos = secondCamera.transform.position;
        Quaternion sc_rot = secondCamera.transform.rotation;
        try
        {
            /* we want a matrix that would map 'transform.position/rotation' to
             * 'secondCamera.transform.position/rotation'
             */
            Matrix4x4 mat = secondCamera.transform.localToWorldMatrix * transform.worldToLocalMatrix;

            secondCamera.transform.position = mat.MultiplyPoint(InputTracking.GetLocalPosition(XRNode.LeftEye));
            secondCamera.transform.rotation = mat.rotation * InputTracking.GetLocalRotation(XRNode.LeftEye);
            secondCamera.projectionMatrix = Camera.main.GetStereoProjectionMatrix(Camera.StereoscopicEye.Left);
            secondCamera.targetTexture = rt_left;
            secondCamera.Render();

            secondCamera.transform.position = mat.MultiplyPoint(InputTracking.GetLocalPosition(XRNode.RightEye));
            secondCamera.transform.rotation = mat.rotation * InputTracking.GetLocalRotation(XRNode.RightEye);
            secondCamera.projectionMatrix = Camera.main.GetStereoProjectionMatrix(Camera.StereoscopicEye.Right);
            secondCamera.targetTexture = rt_right;
            secondCamera.Render();
        }
        finally
        {
            secondCamera.transform.SetPositionAndRotation(sc_pos, sc_rot);
        }
    }
}

using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.XR;
using BaroqueUI;


public class Mirror : MonoBehaviour
{
    public Camera secondCamera;

    RenderTexture rt_left, rt_right;


    /* NOTE: clean code, but only works if Edit -> Project Settings -> Player -> XR Settings ->
     * Stereo Rendering Method is set to Multi Pass.
     * 
     * BUG: the mirror is not perfectly positioned.  It reflects images shifted a very little bit
     * to the left.  I don't understand why...
     */

    private void OnWillRenderObject()
    {
        if (rt_right == null)
        {
            rt_left = new RenderTexture(XRSettings.eyeTextureDesc);
            rt_right = new RenderTexture(XRSettings.eyeTextureDesc);
            GetComponent<Renderer>().material.SetTexture("_LeftTex", rt_left);
            GetComponent<Renderer>().material.SetTexture("_RightTex", rt_right);
            GetComponent<Renderer>().material.SetFloat("_XAxis", -1f);

            var cam = Baroque.GetHeadTransform().GetComponent<Camera>();
            secondCamera.targetTexture = rt_left;
            secondCamera.fieldOfView = cam.fieldOfView;
            secondCamera.farClipPlane = cam.farClipPlane;
            secondCamera.nearClipPlane = cam.nearClipPlane;
        }

        Plane plane = new Plane(transform.forward, transform.position);

        secondCamera.transform.position = Reflect(plane, InputTracking.GetLocalPosition(XRNode.LeftEye));
        secondCamera.transform.rotation = Reflect(plane, InputTracking.GetLocalRotation(XRNode.LeftEye));
        secondCamera.projectionMatrix = Mirrored(Camera.main.GetStereoProjectionMatrix(Camera.StereoscopicEye.Left));
        secondCamera.targetTexture = rt_left;
        secondCamera.Render();

        secondCamera.transform.position = Reflect(plane, InputTracking.GetLocalPosition(XRNode.RightEye));
        secondCamera.transform.rotation = Reflect(plane, InputTracking.GetLocalRotation(XRNode.RightEye));
        secondCamera.projectionMatrix = Mirrored(Camera.main.GetStereoProjectionMatrix(Camera.StereoscopicEye.Right));
        secondCamera.targetTexture = rt_right;
        secondCamera.Render();
    }

    Vector3 Reflect(Plane plane, Vector3 pt)
    {
        var mirror_pt = plane.ClosestPointOnPlane(pt);
        return mirror_pt + (mirror_pt - pt);
    }

    Vector3 ReflectVector(Plane plane, Vector3 v)
    {
        plane.distance = 0;
        return Reflect(plane, v);
    }

    Quaternion Reflect(Plane plane, Quaternion q)
    {
        Vector3 forward = q * Vector3.forward;
        Vector3 up = q * Vector3.up;
        Vector3 reflected_forward = ReflectVector(plane, forward);
        Vector3 reflected_up = ReflectVector(plane, up);
        return Quaternion.LookRotation(reflected_forward, reflected_up);
    }

    Matrix4x4 Mirrored(Matrix4x4 projmat)
    {
        /* XXX I get good results with this.  Must understand why */
        projmat.m02 = -projmat.m02;
        return projmat;
    }
}

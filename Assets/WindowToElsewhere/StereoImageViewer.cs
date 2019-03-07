using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using BaroqueUI;
using UnityEngine.XR;


public class StereoImageViewer : MonoBehaviour
{
    public RenderTexture leftEyeImage, rightEyeImage;
    public WindowToElsewhere src;
    public Transform quad;
    public Shader shader;

    Material material;


    IEnumerator Start()
    {
        material = new Material(shader);
        material.SetTexture("_LeftTex", leftEyeImage);
        material.SetTexture("_RightTex", rightEyeImage);
        quad.GetComponent<Renderer>().sharedMaterial = material;

        var ht = Controller.HoverTracker(this);
        ht.onTriggerDown += (ctrl) => Rerender();

        Controller.GlobalTracker(this).onControllersUpdate += (ctrls) => PrepareForRendering();

        yield return new WaitForSeconds(2f);
        Rerender();
    }

    void Rerender()
    {
        var secondCamera = src.secondCamera;
        var transform = src.transform;
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
            secondCamera.targetTexture = leftEyeImage;
            secondCamera.Render();

            secondCamera.transform.position = mat.MultiplyPoint(InputTracking.GetLocalPosition(XRNode.RightEye));
            secondCamera.transform.rotation = mat.rotation * InputTracking.GetLocalRotation(XRNode.RightEye);
            secondCamera.projectionMatrix = Camera.main.GetStereoProjectionMatrix(Camera.StereoscopicEye.Right);
            secondCamera.targetTexture = rightEyeImage;
            secondCamera.Render();
        }
        finally
        {
            secondCamera.transform.SetPositionAndRotation(sc_pos, sc_rot);
            secondCamera.targetTexture = null;
        }
    }

    void PrepareForRendering()
    {
        var headtr = Baroque.GetHeadTransform();
        Vector3 v1 = transform.position - headtr.position;
        transform.rotation = Quaternion.LookRotation(v1);

        var cam = Camera.main;
        Vector2 pt1 = cam.WorldToViewportPoint(transform.position);
        Matrix4x4 mat = Matrix4x4.identity;
        mat.m00 = 0.5f;
        mat.m11 = -0.5f;
        mat.m02 = 1f - pt1.x;
        mat.m12 = 1f - pt1.y;
        material.SetMatrix("WTETransform2D", mat);
    }
}

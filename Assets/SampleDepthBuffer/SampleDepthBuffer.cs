using System.Collections;
using System.Collections.Generic;
using UnityEngine;


public class SampleDepthBuffer : MonoBehaviour
{
    public Camera sourceCamera;
    public Camera myCamera;
    public RenderTexture depthTex;
    public Texture2D targetTexture;
    public Shader renderShader;
    public GameObject quadGameObject;


    private void Start() 
    {
        myCamera.fieldOfView = sourceCamera.fieldOfView;
        myCamera.farClipPlane = sourceCamera.farClipPlane;
        myCamera.nearClipPlane = sourceCamera.nearClipPlane;

        targetTexture = new Texture2D(depthTex.width, depthTex.height, TextureFormat.ARGB32, false);
    }

    private void Update()
    {
        myCamera.transform.SetPositionAndRotation(sourceCamera.transform.position, sourceCamera.transform.rotation);
        quadGameObject.SetActive(true);
        myCamera.RenderWithShader(renderShader, "SampleDepth");
        quadGameObject.SetActive(false);

        var saved = RenderTexture.active;
        RenderTexture.active = depthTex;
        targetTexture.ReadPixels(new Rect(0, 0, depthTex.width, depthTex.height), 0, 0);
        RenderTexture.active = saved;

        var pixels = targetTexture.GetPixels32();
        int count_black = 0, count_red = 0, count_white = 0;
        foreach (var pix in pixels)
        {
            if (pix.g > 0)
                count_white++;
            else if (pix.r > 0)
                count_red++;
            else
                count_black++;
        }
        Debug.Log("black: " + count_black + "    red: " + count_red + "    white: " + count_white);
    }
}

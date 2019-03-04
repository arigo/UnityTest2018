using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Rendering;


public class MRTRendering : MonoBehaviour
{
    public Mesh mesh;
    public Material material;


    void Start()
    {
        var cam = GetComponent<Camera>();

        cam.depthTextureMode = DepthTextureMode.Depth | DepthTextureMode.DepthNormals;

        var cmdbuf = new CommandBuffer();
        /*cmdbuf.EnableShaderKeyword("SHADOWS_SCREEN");
        //cmdbuf.SetGlobalTexture("_ShadowMapTexture", BuiltinRenderTextureType.shado);*/
        cmdbuf.DrawMesh(mesh, Matrix4x4.identity, material, 0, 2);

        cam.AddCommandBuffer(CameraEvent.BeforeDepthTexture, cmdbuf);
    }

    /*private void OnPreCull()
    {
        var cam = GetComponent<Camera>();
        Graphics.DrawMesh(mesh, Matrix4x4.identity, material, 0, cam);
    }*/
}

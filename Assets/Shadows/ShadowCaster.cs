using System.Collections;
using System.Collections.Generic;
using UnityEngine;


public class ShadowCaster : MonoBehaviour
{
    public Camera mainCamera;
    public Shader shadowCasterShader;
    public Texture shadowCustomTex;
    public float deltaZ;

    private IEnumerator Start() 
    {
        mainCamera.depthTextureMode |= DepthTextureMode.Depth;
        while (true)
        {
            var camera = GetComponent<Camera>();
            camera.RenderWithShader(shadowCasterShader, "CustomShadows");

            var world2shadow =
                Matrix4x4.TRS(new Vector3(0.5f, 0.5f, 0.5f + deltaZ), Quaternion.identity, new Vector3(0.5f, 0.5f, -0.5f))
                * camera.projectionMatrix * camera.worldToCameraMatrix;
            Shader.SetGlobalTexture("ShadowCustomTex", shadowCustomTex);
            Shader.SetGlobalMatrix("ShadowCustomMat", world2shadow);
            Shader.SetGlobalVector("ShadowCustomNormal", -camera.transform.forward);

            yield return new WaitForSeconds(1f);
        }
    }
}

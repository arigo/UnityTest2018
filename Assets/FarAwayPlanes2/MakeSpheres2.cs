using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using BaroqueUI;


public class MakeSpheres2 : MonoBehaviour
{
    public int count;
    public Material matSphere;

    public float depthMin, depthMax;
    public int depthSliceCount;
    public RenderTexture[] rendTex, depthTex;
    public Shader plainShader, customDepthShader;


    IEnumerator Start()
    {
        var localScale = transform.localScale;
        for (int i = 0; i < count; i++)
        {
            var p = Random.insideUnitSphere;
            p.x *= localScale.x;
            p.y *= localScale.y;
            p.z *= localScale.z;

            var go = GameObject.CreatePrimitive(PrimitiveType.Sphere);
            go.layer = 20;
            go.transform.position = p;
            go.transform.SetParent(transform, worldPositionStays: true);
            go.GetComponent<Renderer>().sharedMaterial = matSphere;
        }

        yield return null;

        var origin = Vector3.zero; //Baroque.GetHeadTransform().position;

        var rend_desc = new RenderTextureDescriptor(2048, 2048, RenderTextureFormat.ARGB32, 0);
        var depth_desc = new RenderTextureDescriptor(2048, 2048, RenderTextureFormat.RHalf, 16);
        rendTex = new RenderTexture[depthSliceCount];
        depthTex = new RenderTexture[depthSliceCount];

        for (int i = 0; i < depthSliceCount; i++)
        {
            float d_min = Mathf.Exp(Mathf.Lerp(Mathf.Log(depthMin), Mathf.Log(depthMax),
                                               i / (float)depthSliceCount));
            float d_max = Mathf.Exp(Mathf.Lerp(Mathf.Log(depthMin), Mathf.Log(depthMax),
                                               (i + 1) / (float)depthSliceCount));

            rendTex[i] = new RenderTexture(rend_desc);
            rendTex[i].wrapMode = TextureWrapMode.Clamp;

            depthTex[i] = new RenderTexture(depth_desc);
            depthTex[i].wrapMode = TextureWrapMode.Clamp;

            var cam = GetComponent<Camera>();
            cam.transform.position = origin;
            cam.nearClipPlane = d_min;
            cam.farClipPlane = d_max;
            cam.targetTexture = depthTex[i];
            cam.RenderWithShader(customDepthShader, "");

            cam.targetTexture = rendTex[i];
            cam.Render();


            var mat = new Material(plainShader);
            mat.SetTexture("_MainTex", rendTex[i]);
            mat.SetTexture("_ParallaxTex", depthTex[i]);
            mat.SetFloat("_MinDist", d_min);

            var go = GameObject.CreatePrimitive(PrimitiveType.Quad);
            float d_med = d_max; //Mathf.Sqrt(d_min * d_max);
            go.transform.position = origin + new Vector3(0, 0, d_med);
            //go.transform.rotation = Quaternion.LookRotation(Vector3.back);
            float scale = d_med * Mathf.Tan((Mathf.PI / 180f) * cam.fieldOfView / 2f) * 2;
            go.transform.localScale = scale * Vector3.one;
            go.GetComponent<Renderer>().material = mat;
        }

        yield return null;

        gameObject.SetActive(false);
    }
}

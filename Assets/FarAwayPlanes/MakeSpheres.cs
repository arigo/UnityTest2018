using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using BaroqueUI;


public class MakeSpheres : MonoBehaviour
{
    public int count;
    public Material matSphere;

    public float depthMin, depthMax;
    public int depthSliceCount;
    public RenderTexture[] rendTex;
    public Shader plainShader;


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

        var origin = Baroque.GetHeadTransform().position;

        var desc = new RenderTextureDescriptor(2048, 2048, RenderTextureFormat.ARGB32, 24);
        rendTex = new RenderTexture[depthSliceCount];

        for (int i = 0; i < depthSliceCount; i++)
        {
            float d_min = Mathf.Exp(Mathf.Lerp(Mathf.Log(depthMin), Mathf.Log(depthMax),
                                               i / (float)depthSliceCount));
            float d_max = Mathf.Exp(Mathf.Lerp(Mathf.Log(depthMin), Mathf.Log(depthMax),
                                               (i + 1) / (float)depthSliceCount));

            rendTex[i] = new RenderTexture(desc);
            rendTex[i].wrapMode = TextureWrapMode.Clamp;

            var cam = GetComponent<Camera>();
            cam.transform.position = origin;
            cam.nearClipPlane = d_min;
            cam.farClipPlane = d_max;
            cam.targetTexture = rendTex[i];
            cam.Render();


            var mat = new Material(plainShader);
            mat.SetTexture("_MainTex", rendTex[i]);

            var go = GameObject.CreatePrimitive(PrimitiveType.Quad);
            float d_med = Mathf.Sqrt(d_min * d_max);
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

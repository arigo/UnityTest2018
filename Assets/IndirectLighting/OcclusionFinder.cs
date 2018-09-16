using System.Collections;
using System.Collections.Generic;
using UnityEngine;


public class OcclusionFinder : MonoBehaviour
{
    public int voxelResolution = 64;
    public float voxelSize = 0.1f;
    public Camera sunCamera;
    public Shader occlusionVoxelizeShader, sunVoxelizeShader;
    public ComputeShader readSliceShader, clearShader, debugSetitemShader;
    public Material debugCubeMat;

    RenderTexture occlusionVolume;
    public RenderTexture directShadowTexture;

    private IEnumerator Start()
    {
        occlusionVolume = new RenderTexture(new RenderTextureDescriptor
        {
            dimension = UnityEngine.Rendering.TextureDimension.Tex3D,
            width = voxelResolution,
            height = voxelResolution,
            volumeDepth = voxelResolution,
            colorFormat = RenderTextureFormat.RFloat,
            enableRandomWrite = true,
            msaaSamples = 1,
        });
        occlusionVolume.wrapMode = TextureWrapMode.Clamp;
        occlusionVolume.filterMode = FilterMode.Point;
        occlusionVolume.Create();

        directShadowTexture = new RenderTexture(new RenderTextureDescriptor
        {
            dimension = UnityEngine.Rendering.TextureDimension.Tex2D,
            width = voxelResolution,
            height = voxelResolution,
            volumeDepth = 1,
            colorFormat = RenderTextureFormat.ARGB32,
            //depthBufferBits = 24,
            msaaSamples = 1,
        });
        directShadowTexture.Create();


        Shader.SetGlobalInt("AR_VoxelResolution", voxelResolution);

        /* clear --- not needed? */
        /*clearShader.SetTexture(0, "RG0", occlusionVolume);
        clearShader.Dispatch(0, voxelResolution / 8, voxelResolution / 8, voxelResolution / 8);*/

        /* Render the scene with the voxel proxy camera object with the voxelization
         * shader to voxelize the scene to the volume integer texture
         */
        float ortho_size = voxelResolution * voxelSize * 0.5f;
        sunCamera.orthographic = true;
        sunCamera.orthographicSize = ortho_size;
        sunCamera.aspect = 1;
        sunCamera.targetTexture = directShadowTexture;
        Graphics.SetRandomWriteTarget(1, occlusionVolume);
        Shader.EnableKeyword("AXIS_XYZ");
        sunCamera.RenderWithShader(occlusionVoxelizeShader, "");
        Shader.DisableKeyword("AXIS_XYZ");

        Vector3 sun_position = sunCamera.transform.position;
        Quaternion sun_orientation = sunCamera.transform.rotation;
        sunCamera.transform.SetPositionAndRotation(
            sun_position + ortho_size * (sunCamera.transform.forward + sunCamera.transform.right),
            sun_orientation * Quaternion.LookRotation(-Vector3.right, Vector3.forward));
        Shader.EnableKeyword("AXIS_ZXY");
        sunCamera.RenderWithShader(occlusionVoxelizeShader, "");
        Shader.DisableKeyword("AXIS_ZXY");
        sunCamera.transform.rotation = sun_orientation;

        sunCamera.transform.SetPositionAndRotation(
            sun_position + ortho_size * (sunCamera.transform.forward - sunCamera.transform.up),
            sun_orientation * Quaternion.LookRotation(Vector3.up, -Vector3.right));
        Shader.EnableKeyword("AXIS_YZX");
        sunCamera.RenderWithShader(occlusionVoxelizeShader, "");
        Shader.DisableKeyword("AXIS_YZX");

        sunCamera.transform.SetPositionAndRotation(sun_position, sun_orientation);
        Graphics.ClearRandomWriteTargets();


        Vector3 corner = sunCamera.transform.position - ortho_size * (
            sunCamera.transform.right - sunCamera.transform.up - 2 * sunCamera.transform.forward);
        Matrix4x4 occlusionMatrix = Matrix4x4.TRS(
            corner,
            sunCamera.transform.rotation,
            Vector3.one * (ortho_size * 2f)).inverse;
        occlusionMatrix.SetRow(1, -occlusionMatrix.GetRow(1));
        occlusionMatrix.SetRow(2, -occlusionMatrix.GetRow(2));

        Shader.SetGlobalTexture("AR_OcclusionVolume", occlusionVolume);
        Shader.SetGlobalMatrix("AR_OcclusionMatrix", occlusionMatrix);
        Shader.SetGlobalFloat("AR_OcclusionVoxelSize", voxelSize);

        //yield return new WaitForSeconds(2.5f);
        yield return null;
        DebugReadBackOcclusion();
    }

    void DebugReadBackOcclusion()
    {
        /* debugging... try to read back data from the RWTexture3D.  It's a mess */
        int[] data;
        using (ComputeBuffer cbuf = new ComputeBuffer(voxelResolution * voxelResolution * voxelResolution, 4))
        {
            readSliceShader.SetBuffer(0, "Result", cbuf);
            readSliceShader.SetTexture(0, "Source", occlusionVolume);
            readSliceShader.Dispatch(0, voxelResolution / 8, voxelResolution / 8, voxelResolution / 8);

            data = new int[voxelResolution * voxelResolution * voxelResolution];
            cbuf.GetData(data);
        }

        long total = 0;
        foreach (var x in data)
            total += x;
        Debug.Log("total: " + total);

        var parent_transform = new GameObject("CUBES").transform;
        parent_transform.position = sunCamera.transform.position;
        parent_transform.rotation = sunCamera.transform.rotation;
        //parent_transform.gameObject.SetActive(false);

        int isrc = 0;
        for (int z = 0; z < voxelResolution; z++)
            for (int y = 0; y < voxelResolution; y++)
                for (int x = 0; x < voxelResolution; x++)
                {
                    int datum = data[isrc++];
                    if (datum > 0)
                    {
                        Vector3 pos = voxelSize * new Vector3(
                            x - (voxelResolution - 1f) * 0.5f,
                            (voxelResolution - 1f) * 0.5f - y,
                            voxelResolution - 0.5f - z);

                        var tr = GameObject.CreatePrimitive(PrimitiveType.Cube).transform;
                        //tr.GetComponent<Renderer>().sharedMaterial = debugCubeMat;
                        tr.GetComponent<Renderer>().enabled = false;
                        tr.parent = parent_transform;
                        tr.localPosition = pos;
                        tr.localRotation = Quaternion.identity;
                        tr.localScale = Vector3.one * voxelSize;
                        var ro = tr.gameObject.AddComponent<RemoveOcclusion>();
                        ro.ofinder = this;
                        ro.cubeIndex = new Vector3(x, y, z);
                    }
                }
    }

    class RemoveOcclusion : MonoBehaviour
    {
        public OcclusionFinder ofinder;
        public Vector3 cubeIndex;

        private void OnDestroy()
        {
            var shader = ofinder.debugSetitemShader;
            shader.SetTexture(0, "Result", ofinder.occlusionVolume);
            shader.SetVector("Index", cubeIndex);
            shader.SetFloat("Value", 0f);
            shader.Dispatch(0, 1, 1, 1);
        }
    }


    private void Update()
    {
        directShadowTexture.Create();
        sunCamera.targetTexture = directShadowTexture;
        sunCamera.RenderWithShader(sunVoxelizeShader, "");
    }
}

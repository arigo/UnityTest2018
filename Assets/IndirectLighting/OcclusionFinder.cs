using System.Collections;
using System.Collections.Generic;
using UnityEngine;


public class OcclusionFinder : MonoBehaviour
{
    public int voxelResolution = 64;
    public float voxelSize = 0.1f;
    public Camera sunCamera;
    public Shader occlusionVoxelizeShader;
    public ComputeShader readSliceShader, clearShader;

    RenderTexture occlusionVolume;
    RenderTexture dummyVoxelTexture;

    private void Start()
    {
        occlusionVolume = new RenderTexture(new RenderTextureDescriptor
        {
            dimension = UnityEngine.Rendering.TextureDimension.Tex3D,
            width = voxelResolution,
            height = voxelResolution,
            volumeDepth = voxelResolution,
            colorFormat = RenderTextureFormat.RInt,
            enableRandomWrite = true,
            msaaSamples = 1,
        });
        occlusionVolume.wrapMode = TextureWrapMode.Clamp;
        occlusionVolume.Create();

        dummyVoxelTexture = new RenderTexture(new RenderTextureDescriptor
        {
            dimension = UnityEngine.Rendering.TextureDimension.Tex2D,
            width = voxelResolution,
            height = voxelResolution,
            volumeDepth = 1,
            colorFormat = RenderTextureFormat.RInt,
            msaaSamples = 1,
        });
        dummyVoxelTexture.Create();


        Shader.SetGlobalInt("VoxelResolution", voxelResolution);

        /* clear?? */
        clearShader.SetTexture(0, "RG0", occlusionVolume);
        clearShader.Dispatch(0, voxelResolution / 8, voxelResolution / 8, voxelResolution / 8);


        /* Render the scene with the voxel proxy camera object with the voxelization
         * shader to voxelize the scene to the volume integer texture
         */
        Graphics.SetRandomWriteTarget(1, occlusionVolume);
        sunCamera.orthographic = true;
        sunCamera.orthographicSize = voxelResolution * voxelSize * 0.5f;
        sunCamera.aspect = 1;
        sunCamera.targetTexture = dummyVoxelTexture;
        sunCamera.RenderWithShader(occlusionVoxelizeShader, "");
        Graphics.ClearRandomWriteTargets();

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

        int isrc = 0;
        for (int z = 0; z < voxelResolution; z++)
            for (int y = 0; y < voxelResolution; y++)
                for (int x = 0; x < voxelResolution; x++)
                {
                    int datum = data[isrc++];
                    if (datum > 0)
                    {
                        Vector3 pos = voxelSize * new Vector3(
                            x - voxelResolution * 0.5f, 
                            voxelResolution * 0.5f - y,
                            voxelResolution - z);

                        var tr = GameObject.CreatePrimitive(PrimitiveType.Cube).transform;
                        tr.parent = sunCamera.transform;
                        tr.localPosition = pos;
                        tr.localRotation = Quaternion.identity;
                        tr.localScale = Vector3.one * voxelSize;
                    }
                }
    }
}

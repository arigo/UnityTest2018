﻿using System.Collections;
using System.Collections.Generic;
using UnityEngine;


public class OcclusionFinder : MonoBehaviour
{
    public int voxelResolution = 64;
    public float voxelSize = 0.1f;
    public Camera sunCamera;
    public Shader occlusionVoxelizeShader;
    public ComputeShader readSliceShader, clearShader;
    public Material debugCubeMat;

    RenderTexture occlusionVolume;
    RenderTexture dummyVoxelTexture;

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


        Shader.SetGlobalInt("AR_VoxelResolution", voxelResolution);

        /* clear --- not needed? */
        /*clearShader.SetTexture(0, "RG0", occlusionVolume);
        clearShader.Dispatch(0, voxelResolution / 8, voxelResolution / 8, voxelResolution / 8);*/

        /* Render the scene with the voxel proxy camera object with the voxelization
         * shader to voxelize the scene to the volume integer texture
         */
        Graphics.SetRandomWriteTarget(1, occlusionVolume);
        float ortho_size = voxelResolution * voxelSize * 0.5f;
        sunCamera.orthographic = true;
        sunCamera.orthographicSize = ortho_size;
        sunCamera.aspect = 1;
        sunCamera.targetTexture = dummyVoxelTexture;
        sunCamera.RenderWithShader(occlusionVoxelizeShader, "");
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

        //yield return new WaitForSeconds(2.5f);
        //DebugReadBackOcclusion();
        yield return null;
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
                        tr.GetComponent<Renderer>().sharedMaterial = debugCubeMat;
                        tr.parent = sunCamera.transform;
                        tr.localPosition = pos;
                        tr.localRotation = Quaternion.identity;
                        tr.localScale = Vector3.one * voxelSize;
                    }
                }
    }
}

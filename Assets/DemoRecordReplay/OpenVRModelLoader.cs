using System.Collections;
using System.Collections.Generic;
using System.Runtime.InteropServices;
using UnityEngine;
using Valve.VR;


/* XXXX Oculus */
public class OpenVRModelLoader
{
    static Dictionary<string, Mesh> meshes;
    static SteamVR_RenderModel.RenderModelInterfaceHolder holder;
    static int holder_refcount;

    string model_name;
    bool got_ref;
    CVRRenderModels renderModels;

    public OpenVRModelLoader(string openvr_model_name)
    {
        if (meshes == null)
            meshes = new Dictionary<string, Mesh>();

        model_name = openvr_model_name;
    }

    public bool TryLoadMesh(out Mesh mesh)
    {
        bool result = true;
        try
        {
            result = _TryLoadMesh(out mesh);
        }
        finally
        {
            /* While we return 'false', keep resources.  When we either return 'true' or
             * throw an exception, free them. */
            if (result)
                Dispose();
        }
        return result;
    }

    void Dispose()
    {
        if (got_ref)
        {
            got_ref = false;
            if ((--holder_refcount) == 0)
            {
                holder.Dispose();
                holder = null;
            }
        }
    }

    bool _TryLoadMesh(out Mesh mesh)
    {
        if (meshes.TryGetValue(model_name, out mesh))
            return true;

        if (OpenVR.System == null)
            return false;

        if (holder == null)
            holder = new SteamVR_RenderModel.RenderModelInterfaceHolder();

        if (!got_ref)
        {
            holder_refcount++;
            got_ref = true;
        }

        if (renderModels == null)
        {
            renderModels = holder.instance;
            if (renderModels == null)
                return false;
        }

        var pRenderModel = System.IntPtr.Zero;
        var error = renderModels.LoadRenderModel_Async(model_name, ref pRenderModel);
        if (error == EVRRenderModelError.Loading)
            return false;
        if (error != EVRRenderModelError.None)
            throw new System.Exception(string.Format("Failed to load render model {0} - {1}", model_name, error.ToString()));

        var renderModel = MarshalRenderModel(pRenderModel);

        var vertices = new Vector3[renderModel.unVertexCount];
        var normals = new Vector3[renderModel.unVertexCount];
        var uv = new Vector2[renderModel.unVertexCount];

        var type = typeof(RenderModel_Vertex_t);
        for (int iVert = 0; iVert < renderModel.unVertexCount; iVert++)
        {
            var ptr = new System.IntPtr(renderModel.rVertexData.ToInt64() + iVert * Marshal.SizeOf(type));
            var vert = (RenderModel_Vertex_t)Marshal.PtrToStructure(ptr, type);

            vertices[iVert] = new Vector3(vert.vPosition.v0, vert.vPosition.v1, -vert.vPosition.v2);
            normals[iVert] = new Vector3(vert.vNormal.v0, vert.vNormal.v1, -vert.vNormal.v2);
            uv[iVert] = new Vector2(vert.rfTextureCoord0, vert.rfTextureCoord1);
        }

        int indexCount = (int)renderModel.unTriangleCount * 3;
        var indices = new short[indexCount];
        Marshal.Copy(renderModel.rIndexData, indices, 0, indices.Length);

        var triangles = new int[indexCount];
        for (int iTri = 0; iTri < renderModel.unTriangleCount; iTri++)
        {
            triangles[iTri * 3 + 0] = (int)indices[iTri * 3 + 2];
            triangles[iTri * 3 + 1] = (int)indices[iTri * 3 + 1];
            triangles[iTri * 3 + 2] = (int)indices[iTri * 3 + 0];
        }

        mesh = new Mesh();
        mesh.vertices = vertices;
        mesh.normals = normals;
        mesh.uv = uv;
        mesh.triangles = triangles;
        meshes[model_name] = mesh;

#if (UNITY_5_4 || UNITY_5_3 || UNITY_5_2 || UNITY_5_1 || UNITY_5_0)
            mesh.Optimize();
#endif
        //mesh.hideFlags = HideFlags.DontUnloadUnusedAsset;

        renderModels.FreeRenderModel(pRenderModel);
        return true;
    }

    RenderModel_t MarshalRenderModel(System.IntPtr pRenderModel)
    {
        if ((System.Environment.OSVersion.Platform == System.PlatformID.MacOSX) ||
            (System.Environment.OSVersion.Platform == System.PlatformID.Unix))
        {
            var packedModel = (RenderModel_t_Packed)Marshal.PtrToStructure(pRenderModel, typeof(RenderModel_t_Packed));
            RenderModel_t model = new RenderModel_t();
            packedModel.Unpack(ref model);
            return model;
        }
        else
        {
            return (RenderModel_t)Marshal.PtrToStructure(pRenderModel, typeof(RenderModel_t));
        }
    }
}

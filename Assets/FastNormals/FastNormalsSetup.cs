using System.Collections;
using System.Collections.Generic;
using UnityEngine;


public class FastNormalsSetup : MonoBehaviour
{
    public Material material;
    /*public RenderTexture paintAccum;*/
    public ComputeBuffer fieldbuf;


    private IEnumerator Start()
    {
        /*paintAccum = new RenderTexture(32, 32, 24, RenderTextureFormat.ARGB32, RenderTextureReadWrite.Linear);
        paintAccum.name = "_MainTexInternal";
        paintAccum.enableRandomWrite = true;
        paintAccum.Create();

        material.SetTexture("_MainTexInternal", paintAccum);
        Graphics.ClearRandomWriteTargets();
        Graphics.SetRandomWriteTarget(4, paintAccum);*/

        fieldbuf = new ComputeBuffer(100, sizeof(uint), ComputeBufferType.Default);
        material.SetBuffer("Field", fieldbuf);

        uint[] fdata = new uint[100];
        while (true)
        {
            yield return new WaitForSeconds(0.4f);
            Graphics.SetRandomWriteTarget(1, fieldbuf);
            yield return new WaitForSeconds(0.1f);
            fieldbuf.GetData(fdata);
            string s = "";
            for (int j = 0; j < 100; j++)
            {
                if ((j % 10) == 0)
                    s += " ";
                s += fdata[j].ToString();
            }
            Debug.Log(s);

            for (int i = 0; i < 100; i++)
                fdata[i] = 0;
            fieldbuf.SetData(fdata);
        }
    }
}

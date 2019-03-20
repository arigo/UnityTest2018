using System.Collections;
using System.Collections.Generic;
using System.Text;
using UnityEngine;


public class FNSetup2 : MonoBehaviour
{
    /*public RenderTexture paintAccum;*/
    public ComputeBuffer fieldbuf;


    private IEnumerator Start()
    {
        int width = Display.main.renderingWidth;
        int height = Display.main.renderingHeight;
        Debug.Log(width + " x " + height);

        fieldbuf = new ComputeBuffer(width * height, sizeof(uint), ComputeBufferType.Default);
        Shader.SetGlobalBuffer("CustomFastNormals", fieldbuf);
        Shader.SetGlobalVector("CustomFastNormals_Size", new Vector4(width, height, width * 0.5f, height * 0.5f));

        uint[] fdata = new uint[width * height];
        while (true)
        {
            yield return new WaitForSeconds(0.4f);
            Graphics.SetRandomWriteTarget(1, fieldbuf);
            yield return new WaitForSeconds(0.1f);
            fieldbuf.GetData(fdata);
            string s = "";
            for (int j = 1; j < 10; j++)
            {
                int y = height * j / 10;
                s += "-----";
                for (int i = 1; i < 10; i++)
                {
                    int x = width * i / 10;
                    uint norm = fdata[y * width + x];
                    s += string.Format("[{0} {1}]", norm >> 16, norm & 0xffff);
                }
            }
            Debug.Log(s);

            /*uint max = 0;
            for (int j = 1; j < fdata.Length; j++)
                if (fdata[j] > max)
                    max = fdata[j];
            Debug.Log("MAX: " + max);*/

            /*var file = new System.IO.FileStream("d:\\foo.txt", System.IO.FileMode.Create);
            for (int i = 0; i < fdata.Length; i++)
            {
                uint norm = fdata[i];
                string ss = string.Format("[{0} {1}]", norm >> 16, norm & 0xffff);
                byte[] bb = Encoding.ASCII.GetBytes(ss);
                file.Write(bb, 0, bb.Length);
            }
            file.Close();*/

            //for (int i = 0; i < 100; i++)
            //    fdata[i] = 0;
            //fieldbuf.SetData(fdata);
        }
    }
}

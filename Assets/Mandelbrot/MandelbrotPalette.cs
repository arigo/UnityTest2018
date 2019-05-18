using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using BaroqueUI;


public class MandelbrotPalette : MonoBehaviour
{
    Material mat;

    readonly byte[][] colors1 = new byte[][] {
        /*{8, 14, 32}, */
        new byte[] {0, 0, 0},
        new byte[] {120, 119, 238},
        new byte[] {24, 7, 25},
        new byte[] {197, 66, 28},
        new byte[] {29, 18, 11},
        new byte[] {135, 46, 71},
        new byte[] {24, 27, 13},
        new byte[] {241, 230, 128},
        new byte[] {17, 31, 24},
        new byte[] {240, 162, 139},
        new byte[] {11, 4, 30},
        new byte[] {106, 87, 189},
        new byte[] {29, 21, 14},
        new byte[] {12, 140, 118},
        new byte[] {10, 6, 29},
        new byte[] {50, 144, 77},
        new byte[] {22, 0, 24},
        new byte[] {148, 188, 243},
        new byte[] {4, 32, 7},
        new byte[] {231, 146, 14},
        new byte[] {10, 13, 20},
        new byte[] {184, 147, 68},
        new byte[] {13, 28, 3},
        new byte[] {169, 248, 152},
        new byte[] {4, 0, 34},
        new byte[] {62, 83, 48},
        new byte[] {7, 21, 22},
        new byte[] {152, 97, 184},
        new byte[] {8, 3, 12},
        new byte[] {247, 92, 235},
        new byte[] {31, 32, 16}
    };

    const int MAX_ITER = 171;
    ComputeBuffer cb;

    void MakePalette()
    {
        var RGB = new List<float>();

        for (int i = 0; i < MAX_ITER; i++)
        {
            byte[] inp = colors1[i % colors1.Length];
            RGB.Add(inp[0] / 255f);
            RGB.Add(inp[1] / 255f);
            RGB.Add(inp[2] / 255f);
        }

        cb = new ComputeBuffer(170 * 3, 4);
        cb.SetData(RGB);
    }

    private void Start()
    {
        MakePalette();
        mat = GetComponent<Renderer>().material;   /* make a copy */
    }

    private void Update()
    {
        mat.SetBuffer("Palette", cb);

        foreach (var ctrl in Controller.GetControllers())
        {
            if (ctrl.isActiveAndEnabled)
            {
                Vector3 pos = ctrl.position;
                pos = transform.InverseTransformPoint(pos);
                mat.SetVector("C", pos * 5f);
                break;
            }
        }
    }
}

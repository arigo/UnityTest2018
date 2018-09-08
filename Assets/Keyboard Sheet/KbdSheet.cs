using System.Collections;
using System.Collections.Generic;
using UnityEngine;


public class KbdSheet : MonoBehaviour
{
    const float WIDTH = 0.00135f * 376;
    const float HEIGHT = 0.00135f * 150;

    const int NW = 37;
    const int NH = 15;

    const float TOUCHPAD_CENTER_BACKWARD = 0.047f;
    const float TOUCHPAD_RADIUS = 0.022f;
    const float TOUCHPAD_ROTATION = -6.5f;   /* degrees */
    const float TOUCHPAD_BULDGE = 0.0161f;
    const float TOUCHPAD_BULDGE_EXTENT = 0.02f;

    Vector3[] vertices;
    Mesh mesh;
    Vector3 prev_position;

    private void Start()
    {
        transform.localPosition = Vector3.zero;
        transform.localRotation = Quaternion.identity;
        prev_position = transform.position;   /* global */

        mesh = new Mesh();

        vertices = new Vector3[(NW + 1) * (NH + 1)];
        for (int h = 0; h <= NH; h++)
        {
            for (int w = 0; w <= NW; w++)
            {
                vertices[w + (NW + 1) * h] = new Vector3(
                    (w / (float)NW - 0.5f) * WIDTH,
                    0,
                    (h / (float)NH - 0.5f) * HEIGHT);
            }
        }
        mesh.vertices = vertices;

        int[] tri = new int[6 * NW * NH];
        int i = 0;
        for (int h = 0; h < NH; h++)
        {
            for (int w = 0; w < NW; w++)
            {
                int index = w + (NW + 1) * h;
                tri[i++] = index;
                tri[i++] = index + NW + 1;
                tri[i++] = index + 1;
                tri[i++] = index + 1;
                tri[i++] = index + NW + 1;
                tri[i++] = index + NW + 2;
            }
        }
        mesh.triangles = tri;

        mesh.RecalculateBounds();
        mesh.RecalculateNormals();
        mesh.MarkDynamic();

        GetComponent<MeshFilter>().sharedMesh = mesh;
    }

    private void Update()
    {
        Vector3 delta = transform.position - prev_position;

        float x_delta = Vector3.Dot(delta, transform.right);
        float y_delta = Vector3.Dot(delta, transform.forward);

        Vector3 local_pos = transform.localPosition;
        local_pos.x -= x_delta;
        local_pos.z -= y_delta;
        transform.localPosition = local_pos;
        prev_position = transform.position;

        Vector2 t_center = new Vector2(-local_pos.x, -local_pos.z - TOUCHPAD_CENTER_BACKWARD);
        float tan = Mathf.Tan(-TOUCHPAD_ROTATION * Mathf.PI / 180);

        for (int i = 0; i < vertices.Length; i++)
        {
            Vector3 src_pt = vertices[i];
            //Debug.Log(src_pt.x + " " + src_pt.z);
            float distance = Vector2.Distance(t_center, new Vector2(src_pt.x, src_pt.z));
            float t = (distance - TOUCHPAD_RADIUS) / TOUCHPAD_BULDGE_EXTENT;

            float buldge_max = TOUCHPAD_BULDGE + (src_pt.z + local_pos.z) * tan;

            float buldge = Mathf.SmoothStep(buldge_max, 0, t);
            src_pt.y = buldge;
            vertices[i] = src_pt;
        }
        mesh.vertices = vertices;
        mesh.RecalculateNormals();
        mesh.UploadMeshData(false);
    }
}

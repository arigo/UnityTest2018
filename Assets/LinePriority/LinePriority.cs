using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class LinePriority : MonoBehaviour
{
    public Vector3 start, stop;

    private void Start()
    {
        var mesh = new Mesh();
        mesh.vertices = new Vector3[] { start, stop };
        mesh.SetIndices(new int[] { 0, 1 }, MeshTopology.Lines, 0);
        GetComponent<MeshFilter>().sharedMesh = mesh;
    }
}

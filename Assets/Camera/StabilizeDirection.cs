using System.Collections;
using System.Collections.Generic;
using UnityEngine;


public class StabilizeDirection : MonoBehaviour
{
    public float stabilizeDuration = 0.15f;

    struct Direction
    {
        internal Vector4 dir;
        internal float time;
    }
    Queue<Direction> directions = new Queue<Direction>();
    Vector4 running_sum = Vector4.zero;

    private void Update()
    {
        float old = Time.time - stabilizeDuration;
        while (directions.Count > 0 && directions.Peek().time <= old)
            running_sum -= directions.Dequeue().dir;

        transform.localRotation = Quaternion.identity;
        Quaternion q = transform.rotation;
        Vector4 n = new Vector4(q.x, q.y, q.z, q.w);
        if (Vector4.Dot(n, running_sum) < 0f)
            n = -n;

        directions.Enqueue(new Direction { dir = n, time = Time.time });
        running_sum += n;

        Vector4 n1 = running_sum.normalized;
        Quaternion q1 = new Quaternion(n1.x, n1.y, n1.z, n1.w);
        transform.rotation = q1;
    }
}

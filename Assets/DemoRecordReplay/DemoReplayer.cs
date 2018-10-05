using System;
using System.Collections;
using System.Collections.Generic;
using System.IO;
using UnityEngine;
using BaroqueUI;


public class DemoReplayer : MonoBehaviour
{
    public string openvrModelName = "vr_controller_vive_1_5";
    public Material ghostCtrlMaterial;
    public Transform robotPrefab;

    class GhostCtrl
    {
        internal Transform tr;
        internal Vector3 source_position, target_position, current_position, back_position;
        internal Quaternion source_rotation, target_rotation, current_rotation;
    }

    FileStream f;
    Coroutine c;
    Mesh ctrl_mesh;
    List<GhostCtrl> ghost_ctrl;
    Transform robot;

    private IEnumerator Start()
    {
        OpenVRModelLoader loader = new OpenVRModelLoader(openvrModelName);
        while (!loader.TryLoadMesh(out ctrl_mesh))
            yield return null;

        var gt = Controller.GlobalTracker(this);
        gt.onTriggerDown += Gt_onTriggerDown;
    }

    private void Gt_onTriggerDown(Controller controller)
    {
        if (f == null)
        {
            string dirpath = string.Format("{0}\\DemoRecordReplay\\Recordings", Application.dataPath);
            string[] files = Directory.GetFiles(dirpath, "*.bytes");
            Array.Sort(files);
            string path = files[files.Length - 1];
            f = File.OpenRead(path);
            c = StartCoroutine(Replaying());
        }
        else
        {
            StopCoroutine(c);
            f.Close();
            f = null;

            if (ghost_ctrl != null)
            {
                foreach (var gg in ghost_ctrl)
                    Destroy(gg.tr.gameObject);
                ghost_ctrl = null;
            }
            if (robot != null)
            {
                Destroy(robot.gameObject);
                robot = null;
            }
        }
    }

    IEnumerator Replaying()
    {
        byte[] b_array = new byte[(int)f.Length];
        f.Read(b_array, 0, b_array.Length);

        int i_pos = 0;
        Func<float> next = () =>
        {
            float result = BitConverter.ToSingle(b_array, i_pos);
            i_pos += 4;
            return result;
        };

        float internal_time = 0;
        ghost_ctrl = new List<GhostCtrl>();

        while (i_pos < b_array.Length)
        {
            float f_count = next();
            Debug.Assert(f_count == 1 || f_count == 8 || f_count == 15);

            int count = ((int)f_count) / 7;
            while (ghost_ctrl.Count > count)
            {
                Destroy(ghost_ctrl[ghost_ctrl.Count - 1].tr.gameObject);
                ghost_ctrl.RemoveAt(ghost_ctrl.Count - 1);
            }
            while (ghost_ctrl.Count < count)
            {
                var g = new GameObject("ghost ctrl");
                g.AddComponent<MeshFilter>().sharedMesh = ctrl_mesh;
                g.AddComponent<MeshRenderer>().sharedMaterial = ghostCtrlMaterial;
                ghost_ctrl.Add(new GhostCtrl { tr = g.transform });
            }
            if (ghost_ctrl.Count < 2)
            {
                if (robot != null)
                {
                    Destroy(robot.gameObject);
                    robot = null;
                }
            }
            else
            {
                if (robot == null)
                    robot = Instantiate(robotPrefab);
            }

            foreach (var gg in ghost_ctrl)
            {
                Vector3 p;
                Quaternion q;
                p.x = next();
                p.y = next();
                p.z = next();
                q.x = next();
                q.y = next();
                q.z = next();
                q.w = next();
                if (gg.target_position == Vector3.zero)
                {
                    gg.target_position = p;
                    gg.target_rotation = q;
                }
                gg.source_position = gg.target_position;
                gg.source_rotation = gg.target_rotation;
                gg.target_position = p;
                gg.target_rotation = q;
            }

            while (internal_time < 0.05f)
            {
                float fraction = internal_time / 0.05f;
                foreach (var gg in ghost_ctrl)
                {
                    gg.current_position = Vector3.Lerp(gg.source_position, gg.target_position, fraction);
                    gg.current_rotation = Quaternion.Lerp(gg.source_rotation, gg.target_rotation, fraction);
                    gg.tr.SetPositionAndRotation(gg.current_position, gg.current_rotation);

                    gg.back_position = gg.current_position - gg.current_rotation * new Vector3(0, 0, 0.25f);
                }

                if (robot != null)
                {
                    Vector3 p = Vector3.Lerp(ghost_ctrl[0].back_position, ghost_ctrl[1].back_position, 0.5f);
                    p += Vector3.up * 0.05f;
                    Quaternion q = Quaternion.Slerp(ghost_ctrl[0].current_rotation, ghost_ctrl[1].current_rotation, 0.5f);
                    Vector3 qe = q.eulerAngles;
                    qe.x *= 0.5f;
                    q = Quaternion.Euler(qe);
                    robot.SetPositionAndRotation(p, q);
                }

                yield return null;
                internal_time += Time.deltaTime;
            }

            internal_time -= 0.05f;
        }
    }
}

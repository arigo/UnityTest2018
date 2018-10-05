using System;
using System.Collections;
using System.Collections.Generic;
using System.IO;
using UnityEngine;
using BaroqueUI;


public class DemoRecorder : MonoBehaviour
{
    public Material ctrlMaterial, recordingCtrlMaterial;
    bool recording;
    BinaryWriter f;
    Coroutine c;

    private void Start() 
    {
        var gt = Controller.GlobalTracker(this);
        gt.onMenuClick += Gt_onMenuClick;
    }

    private void Update()
    {
        foreach (var ctrl in Baroque.GetControllers())
            foreach (var rend in ctrl.GetComponentsInChildren<Renderer>())
                rend.sharedMaterial = recording ? recordingCtrlMaterial : ctrlMaterial;
    }

    private void OnApplicationQuit()
    {
        if (recording)
            f.Close();
    }

    private void Gt_onMenuClick(Controller controller)
    {
        if (!recording)
        {
            string basename = System.DateTime.Now.ToString("yyyy-MM-dd HH.mm.ss");
            string path = string.Format("{0}\\DemoRecordReplay\\Recordings\\{1}.bytes", Application.dataPath, basename);
            f = new BinaryWriter(File.Create(path));
            c = StartCoroutine(Recording());
        }
        else
        {
            StopCoroutine(c);
            f.Close();
        }
        recording = !recording;
    }

    IEnumerator Recording()
    {
        while (true)
        {
            List<float> lst = new List<float> { 0 };
            foreach (var ctrl in Baroque.GetControllers())
            {
                if (!ctrl.isActiveAndEnabled)
                    continue;
                Vector3 p = ctrl.position;
                Quaternion q = ctrl.rotation;
                lst.Add(p.x);
                lst.Add(p.y);
                lst.Add(p.z);
                Debug.Assert(q.x != 0 || q.y != 0 || q.z != 0 || q.w != 0);
                lst.Add(q.x);
                lst.Add(q.y);
                lst.Add(q.z);
                lst.Add(q.w);
            }
            lst[0] = lst.Count;

            byte[] b_array = new byte[lst.Count * 4];
            Buffer.BlockCopy(lst.ToArray(), 0, b_array, 0, b_array.Length);
            f.Write(b_array);

            yield return new WaitForSeconds(0.05f);
        }
    }
}

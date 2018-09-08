using System.Collections;
using System.Collections.Generic;
using UnityEngine;


public class KbdProjectTouchpad : MonoBehaviour
{
    public Material kbdProjectionMat;

    private IEnumerator Start() 
    {
        while (true)
        {
            Vector3 corner = transform.TransformPoint(new Vector3(-0.5f, -0.5f, 0));
            Vector3 xproj = transform.right / transform.localScale.x;
            Vector3 yproj = transform.up / transform.localScale.y;
            kbdProjectionMat.SetVector("_Corner", corner);
            kbdProjectionMat.SetVector("_XProj", xproj);
            kbdProjectionMat.SetVector("_YProj", yproj);

            foreach (var ctrl in BaroqueUI.Baroque.GetControllers())
            {
                Transform tr1 = ctrl.transform.Find("Model/trackpad");
                if (tr1 == null)
                    continue;
                tr1.GetComponent<Renderer>().sharedMaterial = kbdProjectionMat;
            }
            yield return new WaitForSecondsRealtime(1f);
        }
    }
}

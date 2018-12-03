using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using BaroqueUI;


public class FollowCtrl : MonoBehaviour
{
    public Transform body;
    public Material[] colorizeMaterials;
    public Color exampleColor;

    private void Start()
    {
        string name = string.Format("{0}", UnityEngine.Random.Range(0f, 10f));
        int hashcode = name.GetHashCode();
        double h = Math.Abs(hashcode * 0.000123) % 1.0;

        float h1, s1, v1;
        Color.RGBToHSV(exampleColor, out h1, out s1, out v1);
        Color col = Color.HSVToRGB((float)h, s1, v1);

        foreach (var mat in colorizeMaterials)
            mat.color = col;
    }


    void Update()
    {
        var ctrl = Baroque.GetControllers()[1];
        if (!ctrl.isActiveAndEnabled)
            return;

        transform.position = ctrl.position;
        transform.rotation = ctrl.rotation;

        Vector3 step = ctrl.forward + ctrl.up;

        body.position = ctrl.position + new Vector3(0, -0.31f, 0) - step * 0.13f;
        //body.rotation = Quaternion.Euler(0, Time.time * 90f, 0);

        if (ctrl.triggerPressed)
            Start();
	}
}

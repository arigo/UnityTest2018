using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;


public class ToolOptionDialog : MonoBehaviour
{
    readonly string[] teleport_shapes = new string[]
    {
        "short",
        "regular",
        "long",
        "flat"
    };

    public void SetTeleportSlider(float value)
    {
        int ivalue = (int)value;
        transform.Find("Teleport text").GetComponent<Text>().text = teleport_shapes[ivalue];
        for (int i = 0; i < teleport_shapes.Length; i++)
            transform.Find("Teleport image " + teleport_shapes[i]).gameObject.SetActive(i == ivalue);
    }
}

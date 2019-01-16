using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;


public class BkgndSnapCircle : MonoBehaviour
{
    public float alphaMin, alphaMax, alphaReduce;

    bool reducing;


    float GetAlpha()
    {
        return GetComponent<RawImage>().color.a;
    }

    void SetAlpha(float alpha)
    {
        var ri = GetComponent<RawImage>();
        Color c = ri.color;
        c.a = alpha;
        ri.color = c;
    }

    public void ChangeCircleSize(float value)
    {
        var rtr = transform as RectTransform;
        var size = value;
        rtr.SetSizeWithCurrentAnchors(RectTransform.Axis.Horizontal, size);
        rtr.SetSizeWithCurrentAnchors(RectTransform.Axis.Vertical, size);

        SetAlpha(alphaMax);
        if (!reducing)
        {
            reducing = true;
            StartCoroutine(Reducing());
        }
    }

    IEnumerator Reducing()
    {
        float prev_time = Time.time;

        while (true)
        {
            yield return null;
            float alpha = GetAlpha();
            alpha *= Mathf.Exp((prev_time - Time.time) * alphaReduce);
            if (alpha <= alphaMin)
                break;
            prev_time = Time.time;
            SetAlpha(alpha);
        }
        SetAlpha(alphaMin);
        reducing = false;
    }
}

using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;


public class Clock : MonoBehaviour
{
    public Mines mines;
    public Material clockDigitMat;


	void Start()
    {
        Display(0);
    }

    public void StartTicking()
    {
        StartCoroutine(Ticking());
    }

    IEnumerator Ticking()
    {
        int seconds = 0;
        while (true)
        {
            yield return new WaitForSeconds(1);
            seconds++;
            Display(seconds);
        }
    }

    void Display(int seconds)
    {
        for (int i = 0; i < transform.childCount; i++)
            Destroy(transform.GetChild(i).gameObject);

        var digs = new List<Mines.Digit>();
        while (true)
        {
            digs.Add(mines.GetPrefabDigit(seconds % 10));
            seconds = seconds / 10;
            if (seconds == 0)
                break;
        }

        float gap = mines.GetPrefabDigit(0).bounds.size.x / 12f;
        float width = gap * (digs.Count - 1);
        foreach (var dig in digs)
            width += dig.bounds.size.x;

        float curx = -width / 2f;
        foreach (var dig in digs)
        {
            var tr = Instantiate(dig.prefab, transform, worldPositionStays: false);
            const float SCALE = 0.8f;
            Vector3 pos = new Vector3(curx + dig.bounds.size.x / 2f, 0, 0);
            tr.localPosition = pos - dig.center * SCALE;
            tr.localRotation = Quaternion.identity;
            tr.localScale = Vector3.one * SCALE;

            foreach (var rend in tr.GetComponentsInChildren<Renderer>())
                rend.material = clockDigitMat;

            curx += dig.bounds.size.x + gap;
        }
    }

    public void StopTicking()
    {
        StopAllCoroutines();
    }
}

using System;
using System.Collections;
using System.Collections.Generic;
using BaroqueUI;
using UnityEngine;

public class dots : MonoBehaviour {
    List<GameObject> lights = new List<GameObject>();
    const float SPACING = 0.03f;
    const float SCALE = 0.01f;
    const float CUTOFF = 0.025f;

    int lit = 0, current = -1;
    float lastUpdate = 0f;

    float startTime = -1, lastTime;
    int success = 0;
    int failure = 0;

    public Material green;
    public Material red;
    public Material defaultMat;
    public GameObject pointer;

	// Use this for initialization
	void Start () {
        var x = -1f;
        var y = 1.5f;
        var z = 0f;
        for (int i = 0; i < 10; i++)
        {
            var sphere = GameObject.CreatePrimitive(PrimitiveType.Sphere);
            sphere.transform.position = new Vector3(x, y, z);
            sphere.transform.localScale = new Vector3(SCALE, SCALE, SCALE);
            sphere.GetComponent<Renderer>().material = defaultMat;
            x += SPACING;
            lights.Add(sphere);
        }
        makeGreen(lights[0]);
        lit = 0;
        var ct = Controller.HoverTracker(this);
        ct.onTriggerDown += triggerDown;
    }

    private void triggerDown(Controller controller)
    {
        if (startTime == -1)
            startTime = Time.time;
        lastTime = Time.time;
        if (current == lit)
        {
            success++;
            int index;
            while (true)
            {
                index = UnityEngine.Random.Range(0, lights.Count);
                if (index != lit)
                    break;
            }
            makeDefault(lights[lit]);
            lit = index;
        }
        else
        {
            failure++;
        }
        Debug.Log("current success rate " + ((float)success / (success + failure)).ToString());
        Debug.Log("Rate of clicks " + (success / (lastTime - startTime)).ToString());
    }

    void makeGreen(GameObject o)
    {
        o.GetComponent<Renderer>().material = green;
    }

    void makeRed(GameObject o)
    {
        o.GetComponent<Renderer>().material = red;
    }

    void makeDefault(GameObject o)
    {
        o.GetComponent<Renderer>().material = defaultMat;
    }

    int CheckCollisions()
    {
        var candidate = -1;
        var canDist = 1000f;
        for (int i = 0; i < lights.Count; i++)
        {
            var distance = (lights[i].transform.position - pointer.transform.position).magnitude;
            if (distance < CUTOFF && distance < canDist)
            {
                candidate = i;
                canDist = distance;
            }
        }
        return candidate;
    }
	
	// Update is called once per frame
	void Update () {
        if (current != -1)
            makeDefault(lights[current]);
        var hit = CheckCollisions();
        makeGreen(lights[lit]);
        if (hit != -1)
        {
            current = hit;
            makeRed(lights[current]);
            pointer.SetActive(false);
        } else
        {
            current = -1;
            pointer.SetActive(true);
        }
	}
}

using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using BaroqueUI;


public class UnknownBox : MonoBehaviour
{
    public Material activeMat, defaultMat, probablyBombMat;
    public bool probablyBomb;


    void Start()
    {
        var ht = Controller.HoverTracker(this);
        ht.onEnter += Ht_onEnter;
        ht.onLeave += Ht_onLeave;
        ht.onTriggerDown += Ht_onTriggerDown;
        ht.onTouchPressDown += Ht_onTouchPressDown;
	}

    private void Ht_onTouchPressDown(Controller controller)
    {
        probablyBomb = !probablyBomb;
        GetComponent<Renderer>().sharedMaterial = probablyBomb ? probablyBombMat : activeMat;
    }

    private void Ht_onTriggerDown(Controller controller)
    {
        if (probablyBomb)
            return;
        var mines = GetComponentInParent<Mines>();
        mines.Click(transform);
    }

    private void Ht_onEnter(Controller controller)
    {
        GetComponent<Renderer>().sharedMaterial = probablyBomb ? probablyBombMat : activeMat;
    }

    private void Ht_onLeave(Controller controller)
    {
        GetComponent<Renderer>().sharedMaterial = probablyBomb ? probablyBombMat : defaultMat;
    }
}

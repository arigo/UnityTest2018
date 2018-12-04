using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using BaroqueUI;


public class SelectLevel : MonoBehaviour
{
    public Mines mines;
    public Material defaultMat, highlightMat, activeMat, highlightActiveMat;

    bool _hover;


    private void Start()
    {
        var ht = Controller.HoverTracker(this);
        ht.onEnter += Ht_onEnter;
        ht.onLeave += Ht_onLeave;
        ht.onTriggerDown += Ht_onTriggerDown;
        UpdateMaterial();
    }

    void UpdateMaterial()
    {
        Material mat;
        if (mines.playArea.currentMines == mines)
            mat = _hover ? highlightActiveMat : activeMat;
        else
            mat = _hover ? highlightMat : defaultMat;
        GetComponent<Renderer>().sharedMaterial = mat;
    }

    private void Ht_onEnter(Controller controller)
    {
        _hover = true;
        UpdateMaterial();
    }

    private void Ht_onLeave(Controller controller)
    {
        _hover = false;
        UpdateMaterial();
    }

    private void Ht_onTriggerDown(Controller controller)
    {
        mines.playArea.currentMines.Unpopulate();
        mines.playArea.currentMines = mines;
        mines.Populate();

        foreach (var sl in FindObjectsOfType<SelectLevel>())
            sl.UpdateMaterial();
    }

    private void Update()
    {
        transform.rotation = Quaternion.Euler(Time.time * 90f, 0, 0);
    }
}

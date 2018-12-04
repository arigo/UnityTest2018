using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using BaroqueUI;


public class UnknownBox : MonoBehaviour
{
    public Material activeMat, defaultMat, probablyBombMat, extraLightMat, probablyBombExtraLightMat;
    public bool probablyBomb;
    public Vector3Int position;
    public Mines mines;

    bool _hover;


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
        UpdateMaterial();
    }

    private void Ht_onTriggerDown(Controller controller)
    {
        if (probablyBomb)
            return;
        mines.Click(transform);
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

    bool ReceiveExtraLight()
    {
        foreach (var pos in mines.Neighbors(position))
        {
            var digitbox = mines.GetCellComponent<DigitBox>(pos);
            if (digitbox != null && digitbox.emit_extra_light)
                return true;
        }
        return false;
    }

    public void UpdateMaterial()
    {
        Material mat;

        if (probablyBomb)
            mat = ReceiveExtraLight() ? probablyBombExtraLightMat : probablyBombMat;
        else if (_hover)
            mat = activeMat;
        else
            mat = ReceiveExtraLight() ? extraLightMat : defaultMat;
        GetComponent<Renderer>().sharedMaterial = mat;
    }
}

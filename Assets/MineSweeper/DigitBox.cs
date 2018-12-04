using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using BaroqueUI;


public class DigitBox : MonoBehaviour
{
    public Mines mines;
    public Vector3Int position;
    public bool emit_extra_light;


    private void Start()
    {
        var ht = Controller.HoverTracker(this);
        ht.onEnter += Ht_onEnter;
        ht.onLeave += Ht_onLeave;
    }

    private void Ht_onEnter(Controller controller)
    {
        StartCoroutine(EmitExtraLightAfterDelay());
        UpdateNeighbors();
    }

    IEnumerator EmitExtraLightAfterDelay()
    {
        yield return new WaitForSeconds(1f);
        emit_extra_light = true;
        UpdateNeighbors();
    }

    private void Ht_onLeave(Controller controller)
    {
        StopAllCoroutines();
        if (emit_extra_light)
        {
            emit_extra_light = false;
            UpdateNeighbors();
        }
    }

    void UpdateNeighbors()
    {
        foreach (var pos in mines.Neighbors(position))
        {
            var unknown_box = mines.GetCellComponent<UnknownBox>(pos);
            if (unknown_box != null)
                unknown_box.UpdateMaterial();
        }
    }
}

using System.Collections;
using System.Collections.Generic;
using UnityEngine;


public class AddSheet : MonoBehaviour
{
    public GameObject sheet;

    private IEnumerator Start()
    {
        while (BaroqueUI.Baroque.GetControllers().Length == 0)
            yield return null;

        yield return new WaitForSecondsRealtime(1f);

        foreach (var ctrl in BaroqueUI.Baroque.GetControllers())
            Instantiate(sheet, ctrl.transform);
    }
}

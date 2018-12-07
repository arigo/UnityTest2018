using System.Collections;
using System.Collections.Generic;
using UnityEngine;


public class OptionPanel : MonoBehaviour
{
    public UnityEngine.UI.Image backgroundPanel;
    public MeshRenderer backgroundCube;
    public Material opaqueMaterial, transparentMaterial;

    Material transparentMaterialCopy;


    public void SetOpacity(float fraction)
    {
        Color color;

        if (fraction >= 1f)
        {
            backgroundCube.sharedMaterial = opaqueMaterial;
        }
        else
        {
            if (transparentMaterialCopy == null)
                transparentMaterialCopy = new Material(transparentMaterial);

            color = transparentMaterial.color;
            color.a = fraction;
            transparentMaterialCopy.color = color;
            backgroundCube.sharedMaterial = transparentMaterialCopy;
        }

        color = backgroundPanel.color;
        color.a = fraction * 0.8235f;
        backgroundPanel.color = color;
    }
}

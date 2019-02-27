using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;
using BaroqueUI;


namespace MyTest
{
    public class PopupDialogTitle : MonoBehaviour,
            IPointerEnterHandler, IPointerExitHandler, IPointerDownHandler, IPointerUpHandler, IDragHandler
    {
        public Material highlightedMaterial, dragMaterial;

        MeshRenderer cubeTitleRenderer;
        Material defaultMaterial;
        bool has_enter, has_drag;

        MeshRenderer GetCubeTitleRenderer()
        {
            if (cubeTitleRenderer == null)
            {
                var canvas = GetComponentInParent<PopupDialogCanvas>();
                cubeTitleRenderer = canvas.cubeTitleTransform.GetComponent<MeshRenderer>();
                defaultMaterial = cubeTitleRenderer.sharedMaterial;
            }
            return cubeTitleRenderer;
        }

        void UpdateMaterial()
        {
            var rend = GetCubeTitleRenderer();
            rend.sharedMaterial = has_drag ? dragMaterial :
                                  has_enter ? highlightedMaterial :
                                  defaultMaterial;
        }

        public void OnPointerEnter(PointerEventData eventData)
        {
            has_enter = true;
            UpdateMaterial();
        }

        public void OnPointerExit(PointerEventData eventData)
        {
            has_enter = false;
            UpdateMaterial();
        }

        public void OnPointerDown(PointerEventData eventData)
        {
            has_drag = true;
            UpdateMaterial();
            GetComponentInParent<PopupDialogCanvas>().GripDown(PopupDialogCanvas.most_recent_ctrl);
        }

        public void OnPointerUp(PointerEventData eventData)
        {
            has_drag = false;
            UpdateMaterial();
        }

        public void OnDrag(PointerEventData eventData)
        {
            GetComponentInParent<PopupDialogCanvas>().GripDrag(PopupDialogCanvas.most_recent_ctrl);
        }
    }
}

using System.Collections;
using System.Collections.Generic;
using UnityEngine;


public class CrossDialog : MonoBehaviour
{
    public RectTransform tileTL, tileTR, tileBL, tileBR;

    public void SetBoundsAndHighlight(Rect bounds, Vector2 highlight)
    {
        transform.localPosition = highlight;   /* z = 0 */

        const float MARGIN = 0.01f;
        highlight.x = Mathf.Min(bounds.xMax - MARGIN, Mathf.Max(bounds.xMin + MARGIN, highlight.x));
        highlight.y = Mathf.Min(bounds.yMax - MARGIN, Mathf.Max(bounds.yMin + MARGIN, highlight.y));

        tileTL.localScale = new Vector3(highlight.x - bounds.xMin, bounds.yMax - highlight.y, 1);
        tileTR.localScale = new Vector3(highlight.x - bounds.xMax, bounds.yMax - highlight.y, 1);
        tileBL.localScale = new Vector3(highlight.x - bounds.xMin, bounds.yMin - highlight.y, 1);
        tileBR.localScale = new Vector3(highlight.x - bounds.xMax, bounds.yMin - highlight.y, 1);
    }
}

using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;


namespace MyTest
{
    public class ScenesList : PageDlg
    {
        public RectTransform contentRtr;
        public ButtonWithTransition sceneItemPrefab;
        public float marginTop, marginBottom, sceneItemHeight, minScroll;

        public override void Activate()
        {
            for (int i = contentRtr.childCount - 1; i >= 0; --i)
                Destroy(contentRtr.GetChild(i).gameObject);

            var scenes_names = new List<string> { "abc", "def", "ghijkl",
            "x", "x2", "x2x", "1232231",
            "x", "x2", "x2x", "1232231",
            "x", "x2", "x2x", "1232231",
            "x", "x2", "x2x", "1232231",
            "x", "x2", "x2x", "1232231",
            "x", "x2", "x2x", "1232231",};

            float text_height = scenes_names.Count * sceneItemHeight;
            float box_height = (contentRtr.parent as RectTransform).rect.height;
            contentRtr.SetSizeWithCurrentAnchors(RectTransform.Axis.Vertical,
                Mathf.Max(marginBottom + text_height + marginTop,
                          box_height + minScroll));

            float ybottom = -marginTop;
            foreach (var scene_name in scenes_names)
            {
                ybottom -= sceneItemHeight;

                var sceneItem = Instantiate(sceneItemPrefab, contentRtr);
                sceneItem.transform.localPosition = new Vector3(0, ybottom, 0);
                sceneItem.GetComponentInChildren<Text>().text = scene_name;

                sceneItem.onClick.AddListener(() =>
                {
                    Debug.Log("GOING TO SCENE: " + scene_name);
                });
            }
        }
    }
}

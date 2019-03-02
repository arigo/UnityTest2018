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
        public float marginTop, marginBottom, sceneItemHeight;

        public override void Activate()
        {
            for (int i = contentRtr.childCount - 1; i >= 0; --i)
                Destroy(contentRtr.GetChild(i).gameObject);

            var scene_names = new List<string> { "abc", "def", "ghijkl",
            "x", "x2", "x2x", "1232231",
            "x", "x2", "x2x", "1232231",
            "x", "x2", "x2x", "1232231",
            "x", "x2", "x2x", "1232231",
            "x", "x2", "x2x", "1232231",
            "x", "x2", "x2x", "1232231",};

            float text_height = scene_names.Count * sceneItemHeight;
            contentRtr.SetSizeWithCurrentAnchors(RectTransform.Axis.Vertical,
                marginBottom + text_height + marginTop);

            float ybottom = -marginTop;
            foreach (var scene_name in scene_names)
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

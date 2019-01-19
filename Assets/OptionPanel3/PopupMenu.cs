using System.Collections;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using UnityEngine.UI;
using UnityEngine.Events;
using BaroqueUI;


public class PopupMenu : MonoBehaviour
{
    public Transform backgroundCube;

    public RectTransform textPrefab;
    public RectTransform separatorPrefab;

    public float textItemHeight;
    public float separatorItemHeight;
    public float leftColumn;
    public float widthWithoutText;
    public float heightWithoutText;


    public enum ECheckbox { None, Check, Bullet };

    public struct Item
    {
        static public readonly Item separator = new Item("-", null);

        public readonly string text;
        public readonly ECheckbox checkbox;
        public readonly UnityAction on_click;     /* can be null to disable the menu item */

        public Item(string text, UnityAction on_click)
        {
            this.text = text;
            this.checkbox = ECheckbox.None;
            this.on_click = on_click;
        }

        public Item(string text, ECheckbox checkbox, UnityAction on_click)
        {
            this.text = text;
            this.checkbox = checkbox;
            this.on_click = on_click;
        }
    }

    Item[] items;
    bool displayed;

    public void SetItems(IEnumerable<Item> items)
    {
        Debug.Assert(!displayed);
        this.items = items.ToArray();
    }

    void Start()
    {
        if (items == null || items.Length == 0)
        {
            Debug.LogWarning("No items, popup menu deactivated");
            gameObject.SetActive(false);
            return;
        }

        var my_dialog = GetComponentInChildren<MyMenuDialog>();
        var rtr = my_dialog.transform as RectTransform;

        var settings = textPrefab.GetComponentInChildren<Text>().GetGenerationSettings(new Vector2(2000, 1000));
        TextGenerator generator = new TextGenerator();

        float text_width = 50;
        float text_height = 0;
        foreach (var item in items)
        {
            if (item.text == Item.separator.text)
            {
                text_height += separatorItemHeight;
            }
            else
            {
                text_height += textItemHeight;
                text_width = Mathf.Max(text_width, generator.GetPreferredWidth(item.text, settings));
            }
        }

        var scale = backgroundCube.localScale;
        scale.x = widthWithoutText + text_width;
        scale.y = heightWithoutText + text_height;
        rtr.SetSizeWithCurrentAnchors(RectTransform.Axis.Horizontal, scale.x);
        rtr.SetSizeWithCurrentAnchors(RectTransform.Axis.Vertical, scale.y);
        backgroundCube.localScale = scale;

        text_height += heightWithoutText * 0.5f;
        foreach (var item in items)
        {
            RectTransform tr1;
            if (item.text == Item.separator.text)
            {
                text_height -= separatorItemHeight;
                tr1 = Instantiate(separatorPrefab, rtr);
            }
            else
            {
                text_height -= textItemHeight;
                tr1 = Instantiate(textPrefab, rtr);
                //tr1.GetComponent<RawImage>().color = Color.clear;
                tr1.GetComponentInChildren<Text>().text = item.text;

                switch (item.checkbox)
                {
                    case ECheckbox.Bullet:
                        var bullet = tr1.Find("Bullet").GetComponent<Text>();
                        bullet.gameObject.SetActive(true);
                        break;

                    case ECheckbox.Check:
                        bullet = tr1.Find("Bullet").GetComponent<Text>();
                        bullet.text = "✓";
                        bullet.gameObject.SetActive(true);
                        break;
                }

                if (item.on_click != null)
                {
                    tr1.GetComponent<Button>().onClick.AddListener(() =>
                    {
                        CloseDialog();
                        item.on_click();
                    });
                }
                else
                {
                    tr1.GetComponent<Button>().interactable = false;
                    foreach (var text in tr1.GetComponentsInChildren<Text>())
                        text.color = new Color(0.53f, 0.53f, 0.53f);
                }
            }
            tr1.offsetMin += new Vector2(0, text_height);
            tr1.offsetMax += new Vector2(text_width, text_height);
        }

        displayed = true;
    }

    public void CloseDialog()
    {
        Destroy(gameObject);
    }
}

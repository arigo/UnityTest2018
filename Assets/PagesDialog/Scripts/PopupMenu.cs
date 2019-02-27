using System.Collections;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using UnityEngine.UI;
using UnityEngine.Events;
using BaroqueUI;


namespace MyTest
{
    public class PopupMenu : MonoBehaviour
    {
        /* For pop-up menus that are temporary in nature.  Requires a PopupDialog as well. */

        public RectTransform textPrefab;
        public RectTransform separatorPrefab;

        public float textItemHeight;
        public float separatorItemHeight;
        public float leftColumn;
        public float widthWithoutText;
        public float heightWithoutText;


        public enum ECheckbox { None, Checked, NotChecked, Bullet };
        public static ECheckbox BulletIf(bool condition) { return condition ? ECheckbox.Bullet : ECheckbox.None; }
        public static ECheckbox CheckedIf(bool condition) { return condition ? ECheckbox.Checked : ECheckbox.NotChecked; }
        public static UnityAction ActionIf(bool condition, UnityAction action) { return condition ? action : (UnityAction)null; }

        public struct Item
        {
            static public readonly Item separator = new Item("-", null);

            public readonly string text;
            public readonly ECheckbox checkbox;
            public readonly string shortcut;
            public readonly UnityAction on_click;     /* can be null to disable the menu item */

            public Item(string text, UnityAction on_click)
            {
                this.text = text;
                this.checkbox = ECheckbox.None;
                this.shortcut = null;
                this.on_click = on_click;
            }

            public Item(string text, ECheckbox checkbox, UnityAction on_click)
            {
                this.text = text;
                this.checkbox = checkbox;
                this.shortcut = null;
                this.on_click = on_click;
            }

            public Item(string text, string shortcut, UnityAction on_click)
            {
                this.text = text;
                this.checkbox = ECheckbox.None;
                this.shortcut = shortcut;
                this.on_click = on_click;
            }
        }

        public static void ShowPopup(Controller ctrl, IEnumerable<Item> items)
        {
            var menu = Instantiate(FindObjectOfType<PagesDlgPrefab>().popupMenuPrefab);
            menu.SetItems(items);
            menu.GetComponent<PopupDialog>().ShowDialog(ctrl);
        }


        /**********************************************************************************/

        public void SetItems(IEnumerable<Item> new_items)
        {
            Item[] items = new_items.ToArray();
            if (items.Length == 0)
            {
                Debug.LogWarning("No items, popup menu deactivated");
                gameObject.SetActive(false);
                return;
            }

            var my_dialog = GetComponentInChildren<PopupDialogCanvas>();
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

            rtr.SetSizeWithCurrentAnchors(RectTransform.Axis.Horizontal, widthWithoutText + text_width);
            rtr.SetSizeWithCurrentAnchors(RectTransform.Axis.Vertical, heightWithoutText + text_height);

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
                    tr1.GetComponentInChildren<Text>().text = item.text;

                    switch (item.checkbox)
                    {
                        case ECheckbox.Bullet:
                            var bullet = tr1.Find("Bullet").GetComponent<Text>();
                            bullet.gameObject.SetActive(true);
                            break;

                        case ECheckbox.Checked:
                            bullet = tr1.Find("Bullet").GetComponent<Text>();
                            bullet.text = "✓";
                            bullet.gameObject.SetActive(true);
                            break;
                    }
                    if (item.checkbox == ECheckbox.Checked || item.checkbox == ECheckbox.NotChecked)
                        tr1.Find("Empty Box").gameObject.SetActive(true);

                    if (item.on_click != null)
                    {
                        tr1.GetComponent<Button>().onClick.AddListener(() =>
                        {
                            GetComponent<PopupDialog>().DoCloseDialog();
                            item.on_click();
                        });
                    }
                    else
                    {
                        tr1.GetComponent<Button>().interactable = false;
                        foreach (var text in tr1.GetComponentsInChildren<Text>())
                            text.color = new Color(0.53f, 0.53f, 0.53f);
                    }

                    if (!string.IsNullOrEmpty(item.shortcut))
                    {
                        var shortcut = tr1.Find("Empty Box").GetComponent<Text>();
                        shortcut.text = item.shortcut;
                        if (item.on_click != null)
                            shortcut.color = Color.black;
                        shortcut.gameObject.SetActive(true);
                    }
                }
                tr1.offsetMin += new Vector2(0, text_height);
                tr1.offsetMax += new Vector2(text_width, text_height);
            }
        }
    }
}

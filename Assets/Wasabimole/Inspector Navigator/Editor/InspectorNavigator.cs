using UnityEngine;
using UnityEditor;
using System.Collections.Generic;


// ---------------------------------------------------------------------------------------------------------------------------
// Inspector Navigator - © 2014, 2015 Wasabimole http://wasabimole.com
// Extremely minimal version extracted by Armin Rigo.
// Visit the asset store to find the full version by Wasabimole!
// ---------------------------------------------------------------------------------------------------------------------------


namespace InspectorNavigator.Editor
{
    public static class KeyBindings
    {
        // PC hot-keys
        public const string BackPC = "%LEFT";
        public const string ForwardPC = "%RIGHT";

        // Mac hot-keys
        public const string BackMac = "&%LEFT";
        public const string ForwardMac = "&%RIGHT";
    }


    [InitializeOnLoad]
    public class InspectorNavigator
    {
        const int MaxEnqueuedObjects = 50;

        static InspectorNavigator()
        {
            Selection.selectionChanged += SelectionChangedEvent;
        }

        static List<Object> SelectionList = new List<Object>();
        static int SelIndex = 0;

        static void SelectionChangedEvent()
        {
            var cur = Selection.activeObject;
            if (cur == null)
            {
                SelIndex = SelectionList.Count;
                return;
            }

            if (SelIndex < SelectionList.Count && SelectionList[SelIndex] == cur)
                return;

            SelIndex = SelectionList.Count;
            SelectionList.Add(cur);
            if (SelectionList.Count > MaxEnqueuedObjects)
            {
                SelectionList.RemoveAt(0);
                SelIndex--;
            }
        }

        static void CleanUp()
        {
            var lst = new List<Object>();
            for (int i = 0; i < SelectionList.Count; i++)
            {
                if (SelectionList[i] == null)
                {
                    if (SelIndex == i)
                        SelIndex = SelectionList.Count;
                }
                else
                {
                    if (SelIndex == i)
                        SelIndex = lst.Count;
                    lst.Add(SelectionList[i]);
                }
            }
            SelectionList = lst;
            if (SelIndex > lst.Count)
                SelIndex = lst.Count;
        }

#if UNITY_EDITOR_OSX
        [MenuItem("Window/Inspector Navigator/Back " + KeyBindings.BackMac)]
#else
        [MenuItem("Window/Inspector Navigator/Back " + KeyBindings.BackPC)]
#endif
        public static void DoBackCommand()
        {
            CleanUp();
            if (SelIndex > 0)
            {
                --SelIndex;
                Selection.activeObject = SelectionList[SelIndex];
            }
        }

#if UNITY_EDITOR_OSX
        [MenuItem("Window/Inspector Navigator/Forward " + KeyBindings.ForwardMac)]
#else
        [MenuItem("Window/Inspector Navigator/Forward " + KeyBindings.ForwardPC)]
#endif
        public static void DoForwardCommand()
        {
            CleanUp();
            if (SelIndex < SelectionList.Count - 1)
            {
                ++SelIndex;
                Selection.activeObject = SelectionList[SelIndex];
            }
            else
                Selection.activeObject = null;
        }
    }
}

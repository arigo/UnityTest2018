using System;
using System.IO;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;


public class FolderBrowser : MonoBehaviour
{
    public Text uiDisplayName;
    public RectTransform uiFolderCanvas, uiSubfolderPrefab, uiFilePrefab;
    public string[] rootFolders;

    public List<string> display_names, paths;


    private void Start()
    {
        uiSubfolderPrefab.gameObject.SetActive(false);
        uiFilePrefab.gameObject.SetActive(false);

        if (rootFolders != null)
        {
            string user_profile = Environment.GetEnvironmentVariable("UserProfile");
            if (!string.IsNullOrEmpty(user_profile))
            {
                foreach (var name in rootFolders)
                {
                    var path = Path.Combine(user_profile, name);
                    if (Directory.Exists(path))
                        AddEntry(name, path, folder: true);
                }
            }
            rootFolders = null;

            foreach (var driveinfo in DriveInfo.GetDrives())
            {
                string path = driveinfo.Name;
                AddEntry(path, path, folder: true);
            }
        }
    }

    public void SetDisplayName(string display_name)
    {
        uiDisplayName.text = display_name;
    }

    public void AddEntry(string display_name, string path, bool folder)
    {
        Debug.Log("AddEntry " + display_name + " / " + path);
        int index = paths.Count;
        display_names.Add(display_name);
        paths.Add(path);

        RectTransform prefab = folder ? uiSubfolderPrefab : uiFilePrefab;
        RectTransform entry = Instantiate(prefab, uiFolderCanvas, worldPositionStays: false);
        entry.localPosition = new Vector3(0, -150 - 75 * index, 0);
        entry.GetComponentInChildren<Text>().text = display_name;
        entry.GetComponentInChildren<Button>().onClick.AddListener(() => OpenEntry(entry, display_name, path));
        entry.gameObject.SetActive(true);
        entry.gameObject.AddComponent<RemoveAfterCloneMarker>();

        float min_height = 65 - entry.localPosition.y;
        var sz = uiFolderCanvas.sizeDelta;
        if (sz.y < min_height)
        {
            sz.y = min_height;
            uiFolderCanvas.sizeDelta = sz;
        }
    }

    /* also called from Header.onClick */
    public void CloseFurtherDown()
    {
        bool closing = false;
        for (int i = 0; i < transform.parent.childCount; i++)
        {
            var tr = transform.parent.GetChild(i);
            if (tr == transform)
                closing = true;
            else if (closing)
                Destroy(tr.gameObject);
        }
    }

    class RemoveAfterCloneMarker : MonoBehaviour { }

    void OpenEntry(RectTransform entry, string display_name, string path)
    {
        Debug.Log("OpenEntry " + display_name + " / " + path);
        CloseFurtherDown();
        string[] subdirs;
        try
        {
            subdirs = Directory.GetDirectories(path);
        }
        catch (Exception)
        {
            subdirs = new string[0];
            /* XXX show error message */
        }

        FolderBrowser subfolder = Instantiate(this, transform.parent, worldPositionStays: true);
        foreach (var racm in subfolder.GetComponentsInChildren<RemoveAfterCloneMarker>())
            Destroy(racm.gameObject);
        subfolder.rootFolders = null;
        subfolder.display_names = new List<string>();
        subfolder.paths = new List<string>();
        subfolder.SetDisplayName(display_name);
        subfolder.transform.Translate(subfolder.transform.forward * 0.15f);

        foreach (var subdir in subdirs)
            subfolder.AddEntry(Path.GetFileName(subdir), subdir, folder: true);
    }
}

using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using BaroqueUI;


public class Mines : MonoBehaviour
{
    public PlayArea playArea;
    public int nbombs;
    public int nx, ny, nz;


    public class Digit
    {
        public Transform prefab;
        public Vector3 center;
        public Bounds bounds;
    }


    Dictionary<Vector3Int, Transform> cells;
    Digit[] digits = new Digit[19];
    HashSet<Vector3Int> bombs;
    List<GameObject> remove_me;


    private void Start()
    {
        cells = new Dictionary<Vector3Int, Transform>();
        if (this == playArea.currentMines)
            Populate();
    }

    public void Populate()
    {
        for (int z = 0; z < nz; z++)
            for (int y = 0; y < ny; y++)
                for (int x = 0; x < nx; x++)
                    SetCell(new Vector3Int(x, y, z), -1);
    }

    public void Unpopulate()
    {
        playArea.floor.sharedMaterial = playArea.defaultFloorMat;
        playArea.clock.ResetTicking();
        bombs = null;
        if (remove_me != null)
        {
            foreach (var go in remove_me)
                Destroy(go);
            remove_me = null;
        }
        List<Vector3Int> keys = new List<Vector3Int>(cells.Keys);
        foreach (var pos in keys)
            SetCell(pos, 0);
    }

    void SetCell(Vector3Int pos, int number)
    {
        Transform tr;
        if (cells.TryGetValue(pos, out tr))
        {
            Destroy(tr.gameObject);
            cells.Remove(pos);
        }

        if (number == 0)
            return;

        Transform prefab;
        Vector3 center;
        if (number < 0)
        {
            prefab = playArea.unknownPrefab;
            center = Vector3.zero;
        }
        else
        {
            var digit = GetPrefabDigit(number);
            prefab = digit.prefab;
            center = digit.center;
        }

        tr = Instantiate(prefab, transform, worldPositionStays: false);
        const float SCALE = 0.4f;
        tr.localPosition = pos - center * SCALE;
        tr.localRotation = Quaternion.identity;
        tr.localScale = Vector3.one * SCALE;
        cells[pos] = tr;

        if (number > 0)
        {
            var coll = tr.gameObject.AddComponent<BoxCollider>();
            coll.isTrigger = true;
            coll.center = center;

            var digitbox = tr.gameObject.AddComponent<DigitBox>();
            digitbox.mines = this;
            digitbox.position = pos;
        }
        else
        {
            var ub = tr.GetComponent<UnknownBox>();
            ub.mines = this;
            ub.position = pos;
        }
    }

    public Digit GetPrefabDigit(int number)
    {
        int index = number;
        if (digits[index] == null)
        {
            var digit = playArea.digitsPrefabs.GetChild(index == 0 ? 18 : index - 1);
            Bounds bounds = digit.GetComponentInChildren<Renderer>().bounds;
            Vector3 center = digit.InverseTransformPoint(bounds.center);
            digits[index] = new Digit { prefab = digit, bounds = bounds, center = center };
        }
        return digits[index];
    }

    Vector3Int GetPosition(Transform tr)
    {
        foreach (var pair in cells)
        {
            if (pair.Value == tr)
                return pair.Key;
        }
        throw new KeyNotFoundException();
    }

    void ShowBombs()
    {
        playArea.selectLevel.SetActive(true);

        remove_me = new List<GameObject>();
        foreach (var pos in bombs)
        {
            var bomb = Instantiate(playArea.bombPrefab, transform, worldPositionStays: true);
            bomb.localPosition = pos;
            bomb.localScale = Vector3.one * 0.3f;
            remove_me.Add(bomb.gameObject);
        }

        foreach (var pair in cells) {
            if (IsUnknown(pair.Key))
            {
                var renderer = pair.Value.GetComponent<Renderer>();
                var color = renderer.material.color;
                renderer.material = playArea.translucentMat;
                renderer.material.color *= color;

                Destroy(pair.Value.GetComponent<UnknownBox>());
            }
            else
            {
                var digitbox = pair.Value.GetComponent<DigitBox>();
                if (digitbox != null)
                    Destroy(digitbox);
            }
        }
    }

    public void Click(Transform box)
    {
        Vector3Int pos = GetPosition(box);
        if (bombs == null)
        {
            MakeBombs(pos);
            playArea.clock.StartTicking();
        }
        if (bombs.Contains(pos))
        {
            SetCell(pos, 0);
            playArea.floor.sharedMaterial = playArea.badFloorMat;
            playArea.clock.StopTicking();
            ShowBombs();
            return;
        }
        Click(pos);

        foreach (var pos1 in cells.Keys)
            if (IsUnknown(pos1) && !bombs.Contains(pos1))
                return;
        playArea.floor.sharedMaterial = playArea.goodFloorMat;
        playArea.clock.StopTicking();
        ShowBombs();
    }

    void MakeBombs(Vector3Int pos)
    {
        bombs = new HashSet<Vector3Int>();
        for (int i = 0; i < nbombs; i++)
        {
            while (true)
            {
                Vector3Int p1 = new Vector3Int(
                    Random.Range(0, nx),
                    Random.Range(0, ny),
                    Random.Range(0, nz));
                if (p1 == pos || bombs.Contains(p1))
                    continue;
                bombs.Add(p1);
                break;
            }
        }
        playArea.selectLevel.SetActive(false);
    }

    public IEnumerable<Vector3Int> Neighbors(Vector3Int pos)
    {
        for (int z = pos.z - 1; z <= pos.z + 1; z++)
            for (int y = pos.y - 1; y <= pos.y + 1; y++)
                for (int x = pos.x - 1; x <= pos.x + 1; x++)
                {
                    if (x != pos.x || y != pos.y || z != pos.z)
                    {
                        int dx = (x != pos.x) ? 1 : 0;
                        int dy = (y != pos.y) ? 1 : 0;
                        int dz = (z != pos.z) ? 1 : 0;
                        if (dx + dy + dz < 3)
                            yield return new Vector3Int(x, y, z);
                    }
                }
    }

    bool IsUnknown(Vector3Int pos)
    {
        Transform tr;
        return cells.TryGetValue(pos, out tr) && tr.GetComponent<UnknownBox>() != null;
    }

    public T GetCellComponent<T>(Vector3Int pos) where T : MonoBehaviour
    {
        Transform tr;
        if (!cells.TryGetValue(pos, out tr))
            return null;
        return tr.GetComponent<T>();   /* may be null */
    }

    void Click(Vector3Int pos)
    {
        if (IsUnknown(pos))
        {
            int number = 0;
            foreach (var n in Neighbors(pos))
                if (bombs.Contains(n))
                    number++;
            SetCell(pos, number);

            if (number == 0)
            {
                foreach (var n in Neighbors(pos))
                    Click(n);
            }
        }
    }
}

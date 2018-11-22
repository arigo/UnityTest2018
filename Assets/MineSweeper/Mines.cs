using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class Mines : MonoBehaviour
{
    public int nbombs;
    public int nx, ny, nz;
    public Transform digitsPrefabs;
    public Transform unknownPrefab;
    public Transform bombPrefab;
    public Renderer floor;
    public Material badFloorMat, goodFloorMat, translucentMat;
    public Clock clock;


    public class Digit
    {
        public Transform prefab;
        public Vector3 center;
        public Bounds bounds;
    }


    Dictionary<Vector3Int, Transform> cells;
    Digit[] digits = new Digit[19];
    HashSet<Vector3Int> bombs;


    private void Start()
    {
        cells = new Dictionary<Vector3Int, Transform>();
        for (int z = 0; z < nz; z++)
            for (int y = 0; y < ny; y++)
                for (int x = 0; x < nx; x++)
                    SetCell(new Vector3Int(x, y, z), -1);
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
            prefab = unknownPrefab;
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
    }

    public Digit GetPrefabDigit(int number)
    {
        int index = number;
        if (digits[index] == null)
        {
            var digit = digitsPrefabs.GetChild(index == 0 ? 18 : index - 1);
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
        foreach (var pos in bombs)
        {
            var bomb = Instantiate(bombPrefab, transform, worldPositionStays: true);
            bomb.localPosition = pos;
            bomb.localScale = Vector3.one * 0.3f;
        }

        foreach (var pair in cells) {
            if (IsUnknown(pair.Key))
            {
                var renderer = pair.Value.GetComponent<Renderer>();
                var color = renderer.material.color;
                renderer.material = translucentMat;
                renderer.material.color *= color;

                Destroy(pair.Value.GetComponent<UnknownBox>());
            }
        }
    }

    public void Click(Transform box)
    {
        Vector3Int pos = GetPosition(box);
        if (bombs == null)
        {
            MakeBombs(pos);
            clock.StartTicking();
        }
        if (bombs.Contains(pos))
        {
            SetCell(pos, 0);
            floor.sharedMaterial = badFloorMat;
            clock.StopTicking();
            ShowBombs();
            return;
        }
        Click(pos);

        foreach (var pos1 in cells.Keys)
            if (IsUnknown(pos1) && !bombs.Contains(pos1))
                return;
        floor.sharedMaterial = goodFloorMat;
        clock.StopTicking();
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
    }

    IEnumerable<Vector3Int> Neighbors(Vector3Int pos)
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

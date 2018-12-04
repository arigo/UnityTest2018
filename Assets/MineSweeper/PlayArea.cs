using System.Collections;
using System.Collections.Generic;
using UnityEngine;


public class PlayArea : MonoBehaviour
{
    public Transform digitsPrefabs;
    public Transform unknownPrefab;
    public Transform bombPrefab;
    public Renderer floor;
    public Material defaultFloorMat, badFloorMat, goodFloorMat, translucentMat;
    public Clock clock;

    public GameObject selectLevel;
    public Mines currentMines;
}

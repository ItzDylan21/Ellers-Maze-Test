using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class GridSpawner : MonoBehaviour
{
    public int width;
    public int height;
    public int startNode;
    public int endNode;
    public int cellSize;
    public GameObject gridSpawnPos;
    [HideInInspector] public CustomGrid grid;

    private void Awake()
    {
        grid = new CustomGrid(width, height, startNode, endNode, cellSize, gridSpawnPos.transform.position);
    }
}

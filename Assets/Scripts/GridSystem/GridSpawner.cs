using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class GridSpawner : MonoBehaviour
{
    public int width;
    public int height;
    [HideInInspector] public int startNode;
    [HideInInspector] public int endNode;
    [SerializeField] public Material startNodeMaterial;
    [SerializeField] public Material endNodeMaterial;
    public int cellSize;
    public GameObject gridSpawnPos;
    [HideInInspector] public CustomGrid grid;

    private void Awake()
    {
        grid = new CustomGrid(width, height, startNode, startNodeMaterial, endNode, endNodeMaterial, cellSize, gridSpawnPos.transform.position);
    }
}

using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class CustomGrid : MonoBehaviour
{
    private int width;
    private int height;
    private int cellSize;
    private Vector3 originPos;
    private int[,] gridArray;

    public CustomGrid(int width, int height, int cellSize, Vector3 originPos)
    {
        this.width = width;
        this.height = height;
        this.cellSize = cellSize;
        this.originPos = originPos;

        gridArray = new int[width, height];

        for (int x = 0; x < gridArray.GetLength(0); x++)
        {
            for (int z = 0; z < gridArray.GetLength(1); z++)
            {
                Debug.DrawLine(GetWorldPos(x, z), GetWorldPos(x + 1, z), Color.white, 1000f);
                Debug.DrawLine(GetWorldPos(x, z), GetWorldPos(x, z + 1), Color.white, 1000f);
            }
        }
    }

    public int GetCellSize()
    {
        return cellSize;
    }

    public Vector3 GetWorldPos(int x, int z)
    {
        return new Vector3(x, 0, z) * cellSize + originPos;
    }

    public int[] GetXZ(Vector3 worldPos) {
        int x = Mathf.FloorToInt((worldPos - originPos).x / cellSize);
        int z = Mathf.FloorToInt((worldPos - originPos).z / cellSize);

        return new int[] { x, z };

    }

}

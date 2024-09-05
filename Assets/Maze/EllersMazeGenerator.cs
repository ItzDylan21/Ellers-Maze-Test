using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class EllersMazeGenerator : MonoBehaviour
{
    [SerializeField] GridSpawner gridSpawner;

    private CustomGrid gridXZ;
    private int max_set_val = 1;

    private void Start()
    {
        gridXZ = gridSpawner.grid;

        SpawnEllersMaze();
    }

    private void SpawnEllersMaze()
    {
        // First row of empty cells
        List<(int[], int)> firstRow = new List<(int[], int)> ();

        for (int x = 0; x < gridSpawner.width; x++)
        {
            int[] coords = new int[] {x, 0};
            var cellTup = (coords, max_set_val);

            firstRow.Add(cellTup);

            max_set_val += 1;
        }
        WorldTextRow(firstRow, Color.white, 0);
    }

    #region Helper_Funcs

    private void WorldTextRow(List<(int[], int)> row, Color textColor, int yVal)
    {
        GameObject rowObj = new GameObject();

        foreach (var cellTup in row)
        {
            var coords = cellTup.Item1;
            var set = cellTup.Item2;

            // Create Text GO:
            var text = new GameObject();
            var mesh = text.AddComponent<TextMesh>();
            mesh.text = set.ToString();
            mesh.color = textColor;

            Vector3 gridPos = gridXZ.GetWorldPos(coords[0], coords[1]);
            gridPos = new Vector3(gridPos.x + gridXZ.GetCellSize() / 2, yVal, gridPos.z + gridXZ.GetCellSize() / 2);
            text.transform.position = gridPos;
            text.transform.parent = rowObj.transform;
        }
    }

    #endregion
}

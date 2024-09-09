using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Net.WebSockets;
using System.Security.Cryptography;
using UnityEngine;


static class Extensions
{
    public static IEnumerable<(int, T)> Enumerate<T>(
        this IEnumerable<T> input,
        int start = 0
        )
    {
        int i = start;
        foreach (var t in input) yield return (i++, t);
    }
}

    public class EllersMazeGenerator : MonoBehaviour
    {
        [SerializeField] GridSpawner gridSpawner;

        [Range(0, 100)]
        [SerializeField] int RWallSpawnPercentage = 0;

        [Range(0, 100)]
        [SerializeField] int BWallSpawnPercentage = 0;
        [SerializeField] GameObject wall;
    [SerializeField] GameObject gridCellIndicator; 

    [SerializeField] int WallYValue;
    [SerializeField] Vector2Int startPoint = new Vector2Int(0, 0);
    [SerializeField] Vector2Int endPoint = new Vector2Int(9, 9);

    private CustomGrid gridXZ;
        private int max_set_val = 1;

        private void Start()
        {
            gridXZ = gridSpawner.grid;

            SpawnEllersMaze();
        }

    private Vector2Int RandomPositionOnOuterWall(out string wall)
    {
        int wallSide = UnityEngine.Random.Range(0, 4);

        switch (wallSide)
        {
            case 0: // Top wall
                wall = "Top";
                return new Vector2Int(UnityEngine.Random.Range(0, gridSpawner.width), gridSpawner.height - 1);
            case 1: // Right wall
                wall = "Right";
                return new Vector2Int(gridSpawner.width - 1, UnityEngine.Random.Range(0, gridSpawner.height));
            case 2: // Bottom wall
                wall = "Bottom";
                return new Vector2Int(UnityEngine.Random.Range(0, gridSpawner.width), 0);
            case 3: // Left wall
                wall = "Left";
                return new Vector2Int(0, UnityEngine.Random.Range(0, gridSpawner.height));
            default:
                wall = "Top";
                return new Vector2Int(0, 0);
        }
    }

    private void PlaceStartAndEndPoints()
    {
        // Place Start Point Indicator
        PlaceGridCellIndicator(startPoint, Color.green);

        // Place End Point Indicator
        PlaceGridCellIndicator(endPoint, Color.red);
    }

    private void PlaceGridCellIndicator(Vector2Int point, Color color)
    {
        Vector3 worldPos = gridXZ.GetWorldPos(point.x, point.y);
        worldPos.y = WallYValue; // Adjust height if needed

        GameObject indicator = GameObject.Instantiate(gridCellIndicator, worldPos, Quaternion.identity);
        Renderer renderer = indicator.GetComponent<Renderer>();
        if (renderer)
        {
            Debug.Log("color");
            renderer.material.color = color;
        }
    }


    private void SpawnEllersMaze()
    {
        // Randomize start and end points
        string startWall, endWall;
        startPoint = RandomPositionOnOuterWall(out startWall);
        do
        {
            endPoint = RandomPositionOnOuterWall(out endWall);
        } while (startWall == endWall);

        List<(int[], int)> firstRow = new List<(int[], int)>();

        for (int x = 0; x < gridSpawner.width; x++)
        {
            int[] coords = new int[] { x, 0 };
            var cellTup = (coords, max_set_val);

            firstRow.Add(cellTup);

            max_set_val += 1;
        }
        PlaceWallsSameDir(firstRow, Vector3.right);

        List<(int[], int)> row = firstRow;
        List<int[]> sameSetWalls = new List<int[]>();
        for (int z = 0; z < gridSpawner.height; z++)
        {
            if (z == gridSpawner.height - 1)
            {
                PlaceWallsSameDir(IncreaseAxisRow(1, row), Vector3.right);
                //FinalRow(row, sameSetWalls);
                //continue;
            }

            var joinedRow = JoinRowVerticalWalls(row, sameSetWalls);
            WorldTextRow(joinedRow, Color.red, 1);

            BottomWalls(joinedRow, out List<int[]> bWallLoc);

            row = IncreaseAxisRow(1, EmptyCellsNewRow(joinedRow, bWallLoc));
        }

        // Place start and end points with colored indicators
        PlaceStartAndEndPoints();
    }

    private List<(int[], int)> JoinRowVerticalWalls(List<(int[], int)> row, List<int[]> sameSetWalls)
        {
            var rowCopy = new List<(int[], int)>(row);

            PlaceWallXZ(Vector3.forward, rowCopy[0].Item1[0], rowCopy[0].Item1[1]);

            for (int i = 0; i < rowCopy.Count; i++)
            {
                if (i + 1 < rowCopy.Count)
                {
                    var curCell = rowCopy[i];
                    var nextCell = rowCopy[i + 1];
                    bool join = false;
                bool sameSet = false;

                    if (curCell.Item2 == nextCell.Item2)
                    {
                        join = false;
                    sameSet = true;
                    }
                    else
                    {
                        int randNum = UnityEngine.Random.Range(0, 101);

                        join = InRangeInclusive(randNum, 0, RWallSpawnPercentage);
                    }

                    if (!join)
                    {
                        var coord = nextCell.Item1;
                        PlaceWallXZ(Vector3.forward, coord[0], coord[1]);

                    if(sameSet)
                    {
                        sameSetWalls.Add(new int[] { coord[0], coord[1] + 1});
                    }
                    }
                    else
                    {
                        var newCellTup = (nextCell.Item1, curCell.Item2);

                        rowCopy[i + 1] = newCellTup;
                    }
                }
            }

            PlaceWallXZ(Vector3.forward, rowCopy[rowCopy.Count - 1].Item1[0] + 1, rowCopy[rowCopy.Count - 1].Item1[1]);

            return rowCopy;
        }

        private bool InRangeInclusive(int num, int minRange, int maxRange)
        {
            return minRange <= num && num <= maxRange;
        }

        private void BottomWalls(List<(int[], int)> row, out List<int[]> bottomWallLocations)
        {
        bottomWallLocations = new List<int[]>();

        var rowCopy = new List<(int[], int)>(row);

            var bWallCells = new List<(int[], int)>();
            var noBWallCells = new List<(int[], int)>();

            foreach (var setList in RowSortedBySet(rowCopy))
            {

                var shuffeledSets = setList.OrderBy(a => rng.Next()).ToList();

                foreach (var (i, cell) in shuffeledSets.Enumerate())
                {
                    if (i == 0)
                    {
                        noBWallCells.Add(cell);
                        continue;
                    }
                    int randNum = UnityEngine.Random.Range(0, 101);

                    if (InRangeInclusive(randNum, 0, BWallSpawnPercentage))
                    {
                        bWallCells.Add(cell);
                    }
                    else
                    {
                        noBWallCells.Add(cell);
                    }
                }

            List<int[]> bWalls = new List<int[]>();
                foreach (var cell in bWallCells)
                {
                bWalls.Add(cell.Item1);
                }

                bottomWallLocations = bWalls;

                foreach (var cellTup in IncreaseAxisRow(1, bWallCells))
                {
                    var coord = cellTup.Item1;
                    PlaceWallXZ(Vector3.right, coord[0], coord[1]);
                }
            }
        }

    private List<(int[], int)> EmptyCellsNewRow(List<(int[], int)> row, List<int[]> emptyCells)
    {
        var newRow = new List<(int[], int)>(row);

        foreach (var (i, cell) in row.Enumerate())
        {
            int cellX = cell.Item1[0];
            int cellZ = cell.Item1[1];

            foreach (var coord in emptyCells)
            {
                int eCellX = coord[0];
                int eCellZ = coord[1];

                if (cellX == eCellX && cellZ == eCellZ)
                {
                    var newCellTup = (cell.Item1, max_set_val);
                    newRow[i] = newCellTup;
                    max_set_val += 1;
                }
            }
        }

        return newRow;
    }

    private void FinalRow(List<(int[], int)> row, List<int[]> sameSetWalls)
    {
        PlaceWallXZ(Vector3.forward, row[0].Item1[0], row[0].Item1[1]);

        foreach (var pos in sameSetWalls)
        {
            PlaceWallXZ(Vector3.forward, pos[0], pos[1]);
        }

        PlaceWallXZ(Vector3.forward, row[row.Count - 1].Item1[0], row[row.Count - 1].Item1[1]);
    }
        #region Helper_Funcs

        private List<(int[], int)> IncreaseAxisRow(int addAmount, List<(int[], int)> row)
        {
            var newRow = new List<(int[], int)>();

            foreach (var cellTup in row)
            {
                var coords = cellTup.Item1;
                var set = cellTup.Item2;

                int x = coords[0];
                int z = coords[1] + addAmount;

                int[] newCoords = { x, z };

                var newCellTup = (newCoords, set);

                newRow.Add(newCellTup);

            }

            return newRow;
        }

        private static System.Random rng = new System.Random();

        private List<List<(int[], int)>> RowSortedBySet(List<(int[], int)> row)
        {
            return row.Select((x) => new { Value = x })
                .GroupBy(x => x.Value.Item2)
                .Select(x => x.Select(v => v.Value).ToList())
                .ToList();
        }

        private void PlaceWallsSameDir(List<(int[], int)> row, Vector3 dir)
        {
            foreach (var cellTup in row)
            {
                var coords = cellTup.Item1;

                PlaceWallXZ(dir, coords[0], coords[1]);
            }
        }

    private void PlaceWallXZ(Vector3 dir, int x, int z, Color? color = null)
    {
        Vector3 position = gridXZ.GetWorldPos(x, z);
        position.y = WallYValue;

        GameObject wallObj = GameObject.Instantiate(wall, position, Quaternion.LookRotation(dir));
        Renderer renderer = wallObj.GetComponent<Renderer>();
        if (renderer != null)
        {
            if (color != null)
            {
                renderer.material.color = color.Value;
            } 
            else
            {
                renderer.material.color = Color.cyan;
            }
        }
    }

    private void WorldTextRow(List<(int[], int)> row, Color textColor, int yVal)
    {
        GameObject rowObj = new GameObject();

        foreach (var cellTup in row)
        {
            var coords = cellTup.Item1;
            var set = cellTup.Item2;

            // Determine the position and label text
            string labelText;
            if (coords[0] == startPoint.x && coords[1] == startPoint.y)
            {
                labelText = "Start \n" + set.ToString();
            }
            else if (coords[0] == endPoint.x && coords[1] == endPoint.y)
            {
                labelText = "End \n" + set.ToString();
            }
            else
            {
                labelText = set.ToString();
            }

            // Create Text GameObject
            var text = new GameObject();
            var mesh = text.AddComponent<TextMesh>();
            mesh.text = labelText;
            mesh.color = textColor;

            Vector3 gridPos = gridXZ.GetWorldPos(coords[0], coords[1]);
            gridPos = new Vector3(gridPos.x + gridXZ.GetCellSize() / 2, yVal, gridPos.z + gridXZ.GetCellSize() / 2);
            text.transform.position = gridPos;
            text.transform.parent = rowObj.transform;
        }
    }

}

#endregion

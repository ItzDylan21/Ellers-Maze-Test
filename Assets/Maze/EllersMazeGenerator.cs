using System;
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
            BottomWalls(joinedRow, out List<int[]> bWallLoc);

            row = IncreaseAxisRow(1, EmptyCellsNewRow(joinedRow, bWallLoc));
        }

        VertexText(gridXZ.GetGrid(), Color.blue, 1);

        // Place start and end points with colored indicators
        PlaceStartAndEndPoints();

        // After generating the maze, check if it is solvable and visualize
        List<Vector2Int> path;
        HashSet<Vector2Int> visitedNodes;

        if (IsSolvable(startPoint, endPoint, out path, out visitedNodes))
        {
            Debug.Log("Maze is solvable!");

        }
        else
        {
            Debug.Log("Maze is unsolvable.");
            // Handle unsolvable maze case if needed
        }
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

        // This line needs to handle the outer boundary case correctly
        int x = rowCopy[rowCopy.Count - 1].Item1[0] + 1;
        int z = rowCopy[rowCopy.Count - 1].Item1[1];

        // Ensure walls are placed at the outer boundary (max width/height)
        if (x <= gridSpawner.width && z < gridSpawner.height)
        {
            PlaceWallXZ(Vector3.forward, x, z);
        }

        // Alternatively for right walls:
        if (x < gridSpawner.width && z <= gridSpawner.height)
        {
            PlaceWallXZ(Vector3.right, x, z);
        }

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

            //// Ensure the neighboring cell also has the opposite wall
            //if (dir == Vector3.right) // Right wall
            //{
            //    // Place left wall on the neighboring cell
            //    if (coords[0] + 1 < gridSpawner.width)
            //    {
            //        PlaceWallXZ(Vector3.right, coords[0] + 1, coords[1]);
            //    }
            //}
            //else if (dir == Vector3.forward) // Bottom wall
            //{
            //    // Place top wall on the neighboring cell
            //    if (coords[1] + 1 < gridSpawner.height)
            //    {
            //        PlaceWallXZ(Vector3.forward, coords[0], coords[1] + 1);
            //    }
            //}
        }

        }

    private void PlaceWallXZ(Vector3 dir, int x, int z, Color? color = null)
    {
        bool isOuterWall = (x == gridSpawner.width || z == gridSpawner.height || x == 0 || z == 0);

        // Debug log to verify if it's identifying outer walls correctly
        //Debug.Log($"Placing wall at ({x}, {z}) in direction {dir}. Is outer wall: {isOuterWall}");

        if (!isOuterWall && (x < 0 || x >= gridSpawner.width || z < 0 || z >= gridSpawner.height))
        {
            Debug.LogWarning($"Wall placement out of bounds at ({x}, {z}) in direction {dir}");
            return; // Skip if the coordinates are invalid and it's not an outer wall
        }

        // Update wall data for the current cell
        if (dir == Vector3.right) // Right wall
        {
            if (x >= 0 && x < gridSpawner.width && z >= 0 && z < gridSpawner.height) // Ensure valid grid cell
            {
                gridXZ.SetWall(x, z, CustomGrid.Direction.WEST, true);
                gridXZ.SetWall(x, z, CustomGrid.Direction.EAST, true);
            }
        }
        else if (dir == Vector3.forward) // Bottom wall
        {
            if (x >= 0 && x < gridSpawner.width && z >= 0 && z < gridSpawner.height) // Ensure valid grid cell
            {
                gridXZ.SetWall(x, z, CustomGrid.Direction.NORTH, true);
                gridXZ.SetWall(x, z, CustomGrid.Direction.SOUTH, true);
            }
        }
        //else if (dir == Vector3.right) // Left wall (opposite direction)
        //{
        //    if (x - 1 >= 0 && x - 1 < gridSpawner.width && z >= 0 && z < gridSpawner.height) // Ensure valid grid cell
        //    {
        //        gridXZ.SetRightWall(x - 1, z, true);
        //    }
        //}
        //else if (dir == Vector3.forward) // Top wall (opposite direction)
        //{
        //    if (x >= 0 && x < gridSpawner.width && z - 1 >= 0 && z - 1 < gridSpawner.height) // Ensure valid grid cell
        //    {
        //        gridXZ.SetBottomWall(x, z - 1, true);
        //    }
        //}

        // Place the visual wall object
        Vector3 position = gridXZ.GetWorldPos(x, z);
        position.y = WallYValue;

        GameObject wallObj = GameObject.Instantiate(wall, position, Quaternion.LookRotation(dir));
        Renderer renderer = wallObj.GetComponent<Renderer>();
        if (renderer != null)
        {
            renderer.material.color = color ?? Color.cyan;
        }
    }



    private void VertexText(int[,] grid, Color textColor, int yVal)
    {
        int counter = 1; // Initialize the counter

        // Create a new GameObject for the vertex text
        GameObject gridTextObj = new GameObject("GridText");

        // Iterate over each cell in the grid
        for (int x = 0; x < grid.GetLength(0); x++)
        {
            for (int y = 0; y < grid.GetLength(1); y++)
            {
                // Determine the position and label text
                string labelText = counter.ToString(); // Use the counter for unique numbers

                // Create Text GameObject
                var text = new GameObject();
                var mesh = text.AddComponent<TextMesh>();
                mesh.text = labelText;
                mesh.color = textColor;  // Color can be customized

                // Calculate position in world space
                Vector3 gridPos = gridXZ.GetWorldPos(x, y);
                gridPos = new Vector3(gridPos.x + gridXZ.GetCellSize() / 2, yVal, gridPos.z + gridXZ.GetCellSize() / 2);
                text.transform.position = gridPos;
                text.transform.parent = gridTextObj.transform;

                // Store reference to the TextMesh to allow updates later
                gridXZ.AddTextMesh(x, y, mesh);

                // Increment the counter for the next vertex
                counter++;
            }
        }
    }

    private bool IsSolvable(Vector2Int start, Vector2Int end, out List<Vector2Int> path, out HashSet<Vector2Int> visitedNodes)
    {
        Queue<Vector2Int> frontier = new Queue<Vector2Int>();
        visitedNodes = new HashSet<Vector2Int>(); // Track visited nodes
        Dictionary<Vector2Int, Vector2Int> cameFrom = new Dictionary<Vector2Int, Vector2Int>(); // To reconstruct the path
        frontier.Enqueue(start);
        visitedNodes.Add(start);
        cameFrom[start] = start;

        Debug.Log($"Starting BFS from {start} to {end}");

        gridXZ.UpdateTextColor(start, Color.red); // Mark the start point as red when visited

        while (frontier.Count > 0)
        {
            Vector2Int current = frontier.Dequeue();

            // If we reached the end, reconstruct the path
            if (current == end)
            {
                path = ReconstructPath(cameFrom, start, end);
                Debug.Log("Path found!");
                VisualizePath(path, Color.green); // Use the VisualizePath method to show the path
                return true;
            }

            Debug.Log($"{current.x} {current.y}");

            // Get walkable neighbors
            //foreach (Vector2Int neighbor in GetWalkableNeighbors(current))
            //{
            //    if (!visitedNodes.Contains(neighbor))
            //    {
            //        frontier.Enqueue(neighbor);
            //        visitedNodes.Add(neighbor);
            //        cameFrom[neighbor] = current;

            //        // Mark the visited node as red
            //        gridXZ.UpdateTextColor(neighbor, Color.red);
            //    }
            //}
        }

        // If no path is found
        path = null;
        Debug.Log("No path found.");
        return false;
    }



    private List<Vector2Int> ReconstructPath(Dictionary<Vector2Int, Vector2Int> cameFrom, Vector2Int start, Vector2Int end)
    {
        List<Vector2Int> path = new List<Vector2Int>();
        Vector2Int current = end;

        while (current != start)
        {
            path.Add(current);
            current = cameFrom[current];
        }

        path.Add(start); // Add start at the end
        path.Reverse(); // Reverse to get path from start to end
        return path;
    }

    //private List<Vector2Int> GetWalkableNeighbors(Vector2Int current)
    //{
    //    // Get neighbors from gridXZ as a HashSet
    //    HashSet<Vector2Int> neighborSet = gridXZ.GetNeighbors(current);

    //    // Convert HashSet to List
    //    List<Vector2Int> neighbors = new List<Vector2Int>(neighborSet);


    //    // Define the possible neighbor directions and their respective wall checks
    //    Vector2Int[] directions = {
    //    new Vector2Int(1, 0), // Right
    //    new Vector2Int(-1, 0), // Left
    //    new Vector2Int(0, 1), // Up
    //    new Vector2Int(0, -1) // Down
    //};

    //    foreach (var dir in directions)
    //    {
    //        Vector2Int neighbor = current + dir;
    //        if (IsWithinBounds(neighbor) && CanMoveTo(current, dir))
    //        {
    //            neighbors.Add(neighbor);
    //        }
    //    }

    //    return neighbors;
    //}

    private bool IsWithinBounds(Vector2Int pos)
    {
        return pos.x >= 0 && pos.x < gridSpawner.width && pos.y >= 0 && pos.y < gridSpawner.height;
    }

    private bool CanMoveTo(Vector2Int current, Vector2Int direction)
    {
        Vector2Int right = new Vector2Int(1, 0);
        Vector2Int left = new Vector2Int(-1, 0);
        Vector2Int up = new Vector2Int(0, 1);
        Vector2Int down = new Vector2Int(0, -1);

        // Check if moving right
        if (direction.Equals(right))
        {
            return current.x < gridSpawner.width - 1 && !gridXZ.GetWall(current.x, current.y, CustomGrid.Direction.EAST);
        }
        // Check if moving left
        else if (direction.Equals(left))
        {
            return current.x > 0 && !gridXZ.GetWall(current.x - 1, current.y, CustomGrid.Direction.WEST);
        }
        // Check if moving up
        else if (direction.Equals(up))
        {
            return current.y < gridSpawner.height - 1 && !gridXZ.GetWall(current.x, current.y, CustomGrid.Direction.NORTH);
        }
        // Check if moving down
        else if (direction.Equals(down))
        {
            return current.y > 0 && !gridXZ.GetWall(current.x, current.y - 1, CustomGrid.Direction.SOUTH);
        }

        return false;
    }

    private void VisualizePath(List<Vector2Int> path, Color color)
    {
        foreach (var node in path)
        {
            PlaceGridCellIndicator(node, color); // Green for path, for example
        }
    }
}



#endregion

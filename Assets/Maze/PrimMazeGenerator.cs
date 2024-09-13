using UnityEngine;
using System;
using System.Collections.Generic;
using System.Runtime.CompilerServices;
using Unity.VisualScripting;
using static UnityEngine.XR.Interaction.Toolkit.Inputs.Interactions.SectorInteraction;

public class PrimMazeGenerator : MonoBehaviour
{
    [SerializeField] GridSpawner gridSpawner;
    public GameObject wallPrefab;
    private CustomGrid gridXZ;

    [SerializeField] GameObject gridCellIndicator;

    [SerializeField] int WallYValue;
    public enum Direction
    {
        NORTH, EAST, SOUTH, WEST
    }

    private static readonly int NUM_DIRECTIONS = Enum.GetValues(typeof(Direction)).Length;
    private static readonly int[] DELTA_X = { 0, 1, 0, -1 };
    private static readonly int[] DELTA_Y = { -1, 0, 1, 0 };

    private System.Random randomizer = new System.Random();

    void Start()
    {
        gridXZ = gridSpawner.grid;
        GenerateMaze();
    }

    public void GenerateMaze()
    {
        InitializeWalls();
        GenerateRandomizedPrim();
        ConfigureEntryAndExit();
        RemoveRandomWalls(2 * gridSpawner.width);
        VertexText(gridXZ.GetAllVertices(gridSpawner.startNode), Color.blue, 1);
        BuildMazeInUnity();
    }

    private void InitializeWalls()
    {
        gridXZ.ForAllCells(cell =>
        {
            SetWalls(cell, true);
        });

        // Set outer walls
        for (int x = 0; x < gridSpawner.width; x++)
        {
            // Top row
            gridXZ.SetWall(x, gridSpawner.height, CustomGrid.Direction.NORTH, true);

            // Bottom row
            gridXZ.SetWall(x, 0, CustomGrid.Direction.SOUTH, true);
        }

        for (int y = 0; y < gridSpawner.height; y++)
        {
            // Left column
            gridXZ.SetWall(gridSpawner.width, y, CustomGrid.Direction.WEST, true);

            // Right column
            gridXZ.SetWall(0, y, CustomGrid.Direction.EAST, true);
        }
    }

    private void GenerateRandomizedPrim()
    {
        int totalCells = gridSpawner.width * gridSpawner.height; // Total number of cells
        HashSet<int> visitedCells = new HashSet<int>();          // Track visited cells
        Queue<int> queue = new Queue<int>();                     // Queue to manage cell expansion

        // Start from a random initial cell
        int startX = randomizer.Next(0, gridSpawner.width);
        int startY = randomizer.Next(0, gridSpawner.height);
        int start = startX + startY * gridSpawner.width;

        queue.Enqueue(start);
        visitedCells.Add(start);

        while (queue.Count > 0)
        {
            int current = queue.Dequeue();
            int x = current % gridSpawner.width;
            int y = current / gridSpawner.width;

            List<Direction> directions = GetShuffledDirections();

            bool hasUnvisitedNeighbor = false;

            foreach (Direction direction in directions)
            {
                int nx = x + DELTA_X[(int)direction];
                int ny = y + DELTA_Y[(int)direction];

                if (nx >= 0 && nx < gridSpawner.width && ny >= 0 && ny < gridSpawner.height)
                {
                    int neighbor = nx + ny * gridSpawner.width;
                    if (!visitedCells.Contains(neighbor))
                    {
                        RemoveWall(x, y, direction);
                        visitedCells.Add(neighbor);
                        queue.Enqueue(neighbor);
                        hasUnvisitedNeighbor = true;
                        break; // Continue with the next cell
                    }
                }
            }

            // If no unvisited neighbors, check if there are still unvisited cells
            if (!hasUnvisitedNeighbor && queue.Count == 0 && visitedCells.Count < totalCells)
            {
                // Pick a random unvisited cell to continue
                Vector2Int randomUnvisited = FindRandomUnvisitedCell(visitedCells, totalCells);
                int randomCell = randomUnvisited.x + randomUnvisited.y * gridSpawner.width;

                queue.Enqueue(randomCell);
                visitedCells.Add(randomCell);
            }
        }
    }

    private Vector2Int FindRandomUnvisitedCell(HashSet<int> visitedCells, int totalCells)
    {
        List<int> unvisitedCells = new List<int>();

        // Iterate through all possible cells to find unvisited cells
        for (int i = 0; i < totalCells; i++)
        {
            if (!visitedCells.Contains(i))
            {
                unvisitedCells.Add(i);
            }
        }

        // Pick a random unvisited cell
        if (unvisitedCells.Count > 0)
        {
            int randomIndex = randomizer.Next(0, unvisitedCells.Count);
            int randomCell = unvisitedCells[randomIndex];

            int x = randomCell % gridSpawner.width;
            int y = randomCell / gridSpawner.width;
            return new Vector2Int(x, y);
        }

        return Vector2Int.zero; // Fallback! This should'nt happen if there are unvisted cells remaining
    }

    private void ConfigureEntryAndExit()
    {
        gridSpawner.startNode = gridXZ.CellNumber(2 * gridSpawner.width / 3, 2 * gridSpawner.height / 3);
        SetWalls(gridSpawner.startNode, false);

        bool choice = randomizer.Next(0, 2) == 1;
        CustomGrid.Direction exitDirection = choice ? CustomGrid.Direction.NORTH : CustomGrid.Direction.WEST;
        int exitX = choice ? randomizer.Next(gridSpawner.width) : 0;
        int exitY = choice ? 0 : randomizer.Next(gridSpawner.height);

        gridXZ.SetWall(exitX, exitY, exitDirection, false);
        gridSpawner.endNode = gridXZ.CellNumber(exitX, exitY);
    }

    private void BuildMazeInUnity()
    {
        // Build north walls
        for (int x = 0; x < gridSpawner.width; x++)
        {
            for (int y = 0; y <= gridSpawner.height; y++)
            {
                if (gridXZ.northWalls[x, y])
                {
                    Vector3 position = gridXZ.GetWorldPos(x, y);
                    Instantiate(wallPrefab, position, Quaternion.LookRotation(Vector3.right));
                }
            }
        }

        // Build west walls
        for (int x = 0; x <= gridSpawner.width; x++)
        {
            for (int y = 0; y < gridSpawner.height; y++)
            {
                if (gridXZ.westWalls[x, y])
                {
                    Vector3 position = gridXZ.GetWorldPos(x, y);
                    Instantiate(wallPrefab, position, Quaternion.LookRotation(Vector3.forward));
                }
            }
        }
    }

    private List<Direction> GetShuffledDirections()
    {
        List<Direction> directions = new List<Direction>((Direction[])Enum.GetValues(typeof(Direction)));
        for (int i = directions.Count - 1; i > 0; i--)
        {
            int j = randomizer.Next(i + 1);
            Direction temp = directions[i];
            directions[i] = directions[j];
            directions[j] = temp;
        }
        return directions;
    }

    private void VertexText(HashSet<int> vertices, Color textColor, int yVal)
    {
        Debug.Log("Total vertices: " + vertices.Count);

        GameObject gridTextObj = new GameObject("GridText");

        foreach (var vertex in vertices)
        {
            int x = vertex % gridSpawner.width;
            int y = vertex / gridSpawner.width;

            string labelText = $"({x}, {y})";

            GameObject text = new GameObject($"VertexText ({x}, {y})");
            TextMesh mesh = text.AddComponent<TextMesh>();
            mesh.text = labelText;
            mesh.color = textColor;

            // Calculate world position
            Vector3 gridPos = gridXZ.GetWorldPos(x, y);
            gridPos = new Vector3(gridPos.x + gridXZ.GetCellSize() / 2, yVal, gridPos.z + gridXZ.GetCellSize() / 2);

            // Debug.Log($"Placing text at {gridPos} for vertex ({x}, {y})");

            text.transform.position = gridPos;
            text.transform.parent = gridTextObj.transform;

            gridXZ.AddTextMesh(x, y, mesh);
        }
    }



    private void SetWalls(int cell, bool value)
    {
        gridXZ.SetWalls(gridXZ.posX(cell), gridXZ.posY(cell), value);
    }

    public bool TryToOpenNorthOrWestWallOf(int cell)
    {
        if (randomizer.Next(0, 2) == 1)
        {
            if (!TryOpenWallWithoutCreatingEmptyCorner(cell, CustomGrid.Direction.NORTH, 1, 0))
            {
                return TryOpenWallWithoutCreatingEmptyCorner(cell, CustomGrid.Direction.WEST, 0, 1);
            }
            else
            {
                return true;
            }
        }
        else
        {
            if (!TryOpenWallWithoutCreatingEmptyCorner(cell, CustomGrid.Direction.WEST, 0, 1))
            {
                return TryOpenWallWithoutCreatingEmptyCorner(cell, CustomGrid.Direction.NORTH, 1, 0);
            }
            else
            {
                return true;
            }
        }
    }

    public void RemoveRandomWalls(int n)
    {
        int maxAttempts = n * n;
        int wallsToOpen = n;
        while (wallsToOpen > 0 && maxAttempts > 0)
        {
            if (TryToOpenNorthOrWestWallOf(randomizer.Next(gridXZ.GetNumberOfCells())))
            {
                wallsToOpen--;
            }
            maxAttempts--;
        }
        if (wallsToOpen > 0)
        {
            Debug.Log($"Exceeded maximum attempts to open a random wall: opened {n - wallsToOpen} out of {n}");
        }
    }

    private void RemoveWall(int x, int y, Direction direction)
    {
        switch (direction)
        {
            case Direction.NORTH:
                gridXZ.northWalls[x, y] = false;
                break;
            case Direction.EAST:
                gridXZ.westWalls[x + 1, y] = false;
                break;
            case Direction.SOUTH:
                gridXZ.northWalls[x, y + 1] = false;
                break;
            case Direction.WEST:
                gridXZ.westWalls[x, y] = false;
                break;
        }
    }

    public bool TryOpenWallWithoutCreatingEmptyCorner(int cell, CustomGrid.Direction direction, int deltaX, int deltaZ)
    {
        int cellX = gridXZ.posX(cell);
        int cellZ = gridXZ.posY(cell);

        if (gridXZ.GetWall(cellX, cellZ, direction))
        {
            int neighbor = gridXZ.GetDirectNeighbor(cellX, cellZ, direction);
            if (neighbor >= 0 && GetNumCornerWalls(cellX, cellZ) > 1 && GetNumCornerWalls(cellX + deltaX, cellZ + deltaZ) > 1)
            {
                gridXZ.SetWall(gridXZ.posX(cell), gridXZ.posY(cell), direction, false);
                return true;
            }
        }
        return false;
    }

    public int GetNumCornerWalls(int x, int z)
    {
        int numWalls = 0;
        if (z < gridSpawner.height)
        {
            numWalls += (x < gridSpawner.width && gridXZ.GetWall(x, z, CustomGrid.Direction.NORTH) ? 1 : 0);        // check right
            numWalls += (x > 0 && gridXZ.GetWall(x - 1, z, CustomGrid.Direction.NORTH)) ? 1 : 0;             // check left
        }
        else
        {
            numWalls += (x < gridSpawner.width && gridXZ.GetWall(x, z - 1, CustomGrid.Direction.SOUTH) ? 1 : 0);    // check right
            numWalls += (x > 0 && gridXZ.GetWall(x - 1, z - 1, CustomGrid.Direction.SOUTH)) ? 1 : 0;         // check left
        }
        if (x < gridSpawner.width)
        {
            numWalls += (z < gridSpawner.height && gridXZ.GetWall(x, z, CustomGrid.Direction.WEST) ? 1 : 0);        // check down
            numWalls += (z > 0 && gridXZ.GetWall(x, z - 1, CustomGrid.Direction.WEST)) ? 1 : 0;              // check up
        }
        else
        {
            numWalls += (z < gridSpawner.height && gridXZ.GetWall(x - 1, z, CustomGrid.Direction.EAST) ? 1 : 0);    // check down
            numWalls += (z > 0 && gridXZ.GetWall(x - 1, z - 1, CustomGrid.Direction.EAST)) ? 1 : 0;          // check up
        }
        return numWalls;
    }
}

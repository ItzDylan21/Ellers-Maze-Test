using UnityEngine;
using System;
using System.Collections.Generic;

public class DfsMazeGenerator : MonoBehaviour
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
        VertexText(gridXZ.GetAllVertices(gridSpawner.startNode), Color.blue, 1);
        BuildMazeInUnity();
    }

    private void InitializeWalls()
    {
        // Initialize all walls as true
        gridXZ.ForAllCells(cell =>
        {
            SetWalls(cell, true);
        });

        // Set outer walls - always true
        for (int x = 0; x < gridSpawner.width; x++)
        {
            // Top row (y = 0)

            gridXZ.SetWall(x, gridSpawner.height, CustomGrid.Direction.NORTH, true);

            // Bottom row (y = height)

            gridXZ.SetWall(x, 0, CustomGrid.Direction.SOUTH, true);
        }

        for (int y = 0; y < gridSpawner.height; y++)
        {
            // Left column (x = 0)

            gridXZ.SetWall(gridSpawner.width, y, CustomGrid.Direction.WEST, true);

            // Right column (x = width)

            gridXZ.SetWall(0, y, CustomGrid.Direction.EAST, true);
        }
    }

    private void GenerateRandomizedPrim()
    {
        HashSet<int> visitedCells = new HashSet<int>();
        Stack<int> frontier = new Stack<int>();
        int startX = randomizer.Next(0, gridSpawner.width);
        int startY = randomizer.Next(0, gridSpawner.height);

        visitedCells.Add(startX + startY * gridSpawner.width);
        frontier.Push(startX + startY * gridSpawner.width);

        while (frontier.Count > 0)
        {
            int current = frontier.Pop();
            int x = current % gridSpawner.width;
            int y = current / gridSpawner.width;

            List<Direction> directions = GetShuffledDirections();
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
                        frontier.Push(neighbor);
                    }
                }
            }
        }
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
        Debug.Log(vertices.Count);
        // Create a new GameObject for the vertex text container
        GameObject gridTextObj = new GameObject("GridText");

        // Iterate over each vertex in the set of vertices
        foreach (var vertex in vertices)
        {
            // Convert the integer vertex back into (x, y) coordinates
            int x = vertex % gridSpawner.width;
            int y = vertex / gridSpawner.width;

            // Use the vertex coordinates as the label text in the format "(x, y)"
            string labelText = $"({x}, {y})";

            // Create a Text GameObject for the vertex
            var text = new GameObject($"VertexText ({x}, {y})");
            var mesh = text.AddComponent<TextMesh>();
            mesh.text = labelText;
            mesh.color = textColor;  // Set the color of the text

            // Calculate the position in world space based on the grid system
            Vector3 gridPos = gridXZ.GetWorldPos(x, y);
            gridPos = new Vector3(gridPos.x + gridXZ.GetCellSize() / 2, yVal, gridPos.z + gridXZ.GetCellSize() / 2);
            text.transform.position = gridPos;
            text.transform.parent = gridTextObj.transform; // Make the text a child of the container

            // Store reference to the TextMesh to allow updates later if needed
            gridXZ.AddTextMesh(x, y, mesh);
        }
    }


    private void SetWalls(int cell, bool value)
    {
        gridXZ.SetWalls(gridXZ.posX(cell), gridXZ.posY(cell), value);
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
}

using UnityEngine;
using System;
using System.Collections.Generic;

public class PrimMazeGenerator : MonoBehaviour
{
    public GameObject wallPrefab;
    public int width;
    public int height;

    private bool[,] northWalls;
    private bool[,] westWalls;

    public enum Direction
    {
        NORTH,
        EAST,
        SOUTH,
        WEST
    }

    private static readonly int NUM_DIRECTIONS = Enum.GetValues(typeof(Direction)).Length;
    private static readonly int[] DELTA_X = { 0, 1, 0, -1 };
    private static readonly int[] DELTA_Y = { -1, 0, 1, 0 };

    private System.Random randomizer = new System.Random();

    void Start()
    {
        GenerateMaze();
    }

    public void GenerateMaze()
    {
        InitializeWalls();
        GenerateRandomizedPrim();
        ConfigureEntryAndExit();
        BuildMazeInUnity();
    }

    private void InitializeWalls()
    {
        northWalls = new bool[width, height + 1]; // One extra row for the south walls
        westWalls = new bool[width + 1, height]; // One extra column for the east walls

        // Initialize all walls as true
        for (int x = 0; x < width; x++)
        {
            for (int y = 0; y < height; y++)
            {
                SetWalls(x, y, true);
            }
        }

        // Outer walls - always true
        for (int x = 0; x < width; x++)
        {
            northWalls[x, 0] = true; // Top border
            northWalls[x, height] = true; // Bottom border
        }

        for (int y = 0; y < height; y++)
        {
            westWalls[0, y] = true; // Left border
            westWalls[width, y] = true; // Right border
        }
    }

    private void SetWalls(int x, int y, bool value)
    {
        northWalls[x, y] = value;
        if (x + 1 < width)
            westWalls[x + 1, y] = value;
        if (y + 1 < height)
            northWalls[x, y + 1] = value;
        westWalls[x, y] = value;
    }

    private void GenerateRandomizedPrim()
    {
        HashSet<int> visitedCells = new HashSet<int>();
        Stack<int> frontier = new Stack<int>();
        int startX = randomizer.Next(0, width);
        int startY = randomizer.Next(0, height);

        visitedCells.Add(startX + startY * width);
        frontier.Push(startX + startY * width);

        while (frontier.Count > 0)
        {
            int current = frontier.Pop();
            int x = current % width;
            int y = current / width;

            List<Direction> directions = GetShuffledDirections();
            foreach (Direction direction in directions)
            {
                int nx = x + DELTA_X[(int)direction];
                int ny = y + DELTA_Y[(int)direction];

                if (nx >= 0 && nx < width && ny >= 0 && ny < height)
                {
                    int neighbor = nx + ny * width;
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

    private void RemoveWall(int x, int y, Direction direction)
    {
        switch (direction)
        {
            case Direction.NORTH:
                northWalls[x, y] = false;
                break;
            case Direction.EAST:
                westWalls[x + 1, y] = false;
                break;
            case Direction.SOUTH:
                northWalls[x, y + 1] = false;
                break;
            case Direction.WEST:
                westWalls[x, y] = false;
                break;
        }
    }

    private void ConfigureEntryAndExit()
    {
        // Open top-left for entry
        northWalls[0, 0] = false;

        // Open bottom-right for exit
        northWalls[width - 1, height] = false;
    }

    private void BuildMazeInUnity()
    {
        // Build north walls
        for (int x = 0; x < width; x++)
        {
            for (int y = 0; y <= height; y++)
            {
                if (northWalls[x, y])
                {
                    Vector3 position = new Vector3(x, 0, y);
                    Instantiate(wallPrefab, position, Quaternion.identity);
                }
            }
        }

        // Build west walls
        for (int x = 0; x <= width; x++)
        {
            for (int y = 0; y < height; y++)
            {
                if (westWalls[x, y])
                {
                    Vector3 position = new Vector3(x, 0, y);
                    Instantiate(wallPrefab, position, Quaternion.identity);
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
}

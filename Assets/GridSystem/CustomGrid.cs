using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.CompilerServices;
using UnityEngine;

public class CustomGrid : MonoBehaviour
{
    private int width;
    private int height;
    private int cellSize;
    private Vector3 originPos;
    private int[,] gridArray;

    private bool[,] rightWallArray;
    private bool[,] bottomWallArray;
    private Dictionary<Vector2Int, TextMesh> textMeshes = new Dictionary<Vector2Int, TextMesh>();

    // Direction enum to represent movement directions
    public enum Direction
    {
        NORTH, EAST, SOUTH, WEST
    }

    private static readonly Dictionary<Direction, Vector2Int> DirectionVectors = new Dictionary<Direction, Vector2Int>
    {
        { Direction.EAST, new Vector2Int(1, 0) },
        { Direction.WEST, new Vector2Int(-1, 0) },
        { Direction.NORTH, new Vector2Int(0, 1) },
        { Direction.SOUTH, new Vector2Int(0, -1) },
    };

    private static readonly int NUM_DIRECTIONS = Enum.GetValues(typeof(Direction)).Length;
    private static readonly int[] DELTA_X = { 0, +1, 0, -1 };
    private static readonly int[] DELTA_Y = { -1, 0, +1, 0 };


    public CustomGrid(int width, int height, int cellSize, Vector3 originPos)
    {
        this.width = width;
        this.height = height;
        this.cellSize = cellSize;
        this.originPos = originPos;

        gridArray = new int[width, height];

        rightWallArray = new bool[width, height + 1];
        bottomWallArray = new bool[width + 1, height];
       

        for (int x = 0; x < width; x++)
        {
            for (int z = 0; z < height; z++)
            {
                rightWallArray[x, z] = false;
                bottomWallArray[x, z] = false;
            }
        }

        //for (int x = 0; x < gridArray.GetLength(0); x++)
        //{
        //    for (int z = 0; z < gridArray.GetLength(1); z++)
        //    {
        //        //Debug.DrawLine(GetWorldPos(x, z), GetWorldPos(x + 1, z), Color.white, 1000f);
        //        //Debug.DrawLine(GetWorldPos(x, z), GetWorldPos(x, z + 1), Color.white, 1000f);
        //    }
        //}
    }

    public int GetCellSize()
    {
        return cellSize;
    }

    public int cellNumber(int x, int z)
    {
        return x + z * this.width;
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

    public void AddTextMesh(int x, int z, TextMesh textMesh)
    {
        Vector2Int coords = new Vector2Int(x, z);
        textMeshes[coords] = textMesh;
    }

    public void UpdateTextColor(Vector2Int coords, Color color)
    {
        if (textMeshes.ContainsKey(coords))
        {
            textMeshes[coords].color = color;
        }
    }

    public bool GetWall(int x, int z, Direction direction)
    {
        switch(direction)
        {
            case Direction.NORTH:
                return bottomWallArray[x, z];
                case Direction.EAST:
                return rightWallArray[x, z + 1];
                case Direction.SOUTH:
                return bottomWallArray[x + 1, z];
                case Direction.WEST:
                return rightWallArray[x, z];
        }
        return false;
    }

    public void SetWall(int x, int z, Direction direction, bool value)
    {
        switch (direction)
        {
            case Direction.NORTH:
                bottomWallArray[x, z] = value;
                break;
            case Direction.EAST:
                rightWallArray[x, z + 1] = value;
                break;
            case Direction.SOUTH:
                bottomWallArray[x + 1, z] = value;
                break;
            case Direction.WEST:
                rightWallArray[x, z] = value;
                break;
        }
    }


    public int[,] GetGrid()
    {
        return gridArray;
    }


    private bool IsValidPosition(Vector2Int position)
    {
        return position.x >= 0 && position.x < width && position.y >= 0 && position.y < height;
    }

    private bool HasWallBetween(Vector2Int cellA, Vector2Int cellB, Direction direction)
    {
        switch (direction)
        {
            case Direction.EAST:
                return GetWall(cellA.x, cellA.y, Direction.EAST);
            case Direction.WEST:
                return GetWall(cellB.x, cellB.y + 1, Direction.WEST);
            case Direction.NORTH:
                return GetWall(cellA.x + 1, cellA.y, Direction.NORTH);
            case Direction.SOUTH:
                return GetWall(cellB.x, cellB.y, Direction.SOUTH);
            default:
                return false;
        }
    }

    private Direction GetPerpendicularDirection(Direction direction)
    {
        switch (direction)
        {
            case Direction.EAST: return Direction.NORTH;
            case Direction.WEST: return Direction.SOUTH;
            case Direction.NORTH: return Direction.EAST;
            case Direction.SOUTH: return Direction.WEST;
            default: return Direction.EAST; // Default value
        }
    }

    private int GetNumWalls(Vector2Int cell)
    {
        int numWalls = 0;
        foreach (Direction dir in Enum.GetValues(typeof(Direction)))
        {
            numWalls += GetWall(cell.x, cell.y, dir) ? 1 : 0;
        }

        return numWalls;
    }

    private int GetDirectNeighbor(Vector2Int cell, Direction dir)
    {
        int neighborX = cell.x + DELTA_X[(int)dir];
        int neighborY = cell.y + DELTA_Y[(int)dir];

        if (neighborX < 0 || neighborX >= this.width) return -1;
        if (neighborY < 0 || neighborY >= this.height) return -1;

        // compute neighbour node number
        return cellNumber(neighborX, neighborY);
    }


    public HashSet<Vector2Int> GetNeighbors(Vector2Int fromVertex)
    {
        var neighbors = new HashSet<Vector2Int>();

        foreach (var direction in DirectionVectors)
        {
            Vector2Int directionVec = direction.Value;
            Vector2Int nextNeighbor = fromVertex + directionVec;

            if (IsValidPosition(nextNeighbor) && !HasWallBetween(fromVertex, nextNeighbor, direction.Key))
            {
                // Follow the passage along the direction
                Vector2Int neighbor = nextNeighbor;
                int numWalls = GetNumWalls(neighbor);
                Vector2Int nextNeighborAlongDirection;

                // Continue following in the same direction if possible
                do
                {
                    neighbor = nextNeighbor;
                    numWalls = GetNumWalls(neighbor);
                    nextNeighborAlongDirection = neighbor + directionVec;
                    nextNeighbor = nextNeighborAlongDirection;
                }
                while (IsValidPosition(nextNeighbor) &&
                       !HasWallBetween(neighbor, nextNeighbor, direction.Key) &&
                       numWalls == 2); // Adjust this to match the requirement of having exactly 2 walls

                // Debug log
                Debug.Log($"Adding neighbor {neighbor} for fromVertex {fromVertex}");

                // Try to continue around a corner if the path follows the constraints
                if (numWalls == 2)
                {
                    Direction turnedDirection = GetPerpendicularDirection(direction.Key);
                    nextNeighbor = neighbor + DirectionVectors[turnedDirection];

                    while (IsValidPosition(nextNeighbor) &&
                           !HasWallBetween(neighbor, nextNeighbor, turnedDirection) &&
                           numWalls == 2)
                    {
                        do
                        {
                            neighbor = nextNeighbor;
                            numWalls = GetNumWalls(neighbor);
                            nextNeighbor = neighbor + DirectionVectors[turnedDirection];
                        }
                        while (IsValidPosition(nextNeighbor) &&
                               !HasWallBetween(neighbor, nextNeighbor, turnedDirection) &&
                               numWalls == 2);

                        // Debug log
                        Debug.Log($"Continuing around the corner, adding neighbor {neighbor}");

                        // Try again around the original direction
                        nextNeighbor = neighbor + directionVec;
                        while (IsValidPosition(nextNeighbor) &&
                               !HasWallBetween(neighbor, nextNeighbor, direction.Key) &&
                               numWalls == 2)
                        {
                            neighbor = nextNeighbor;
                            numWalls = GetNumWalls(neighbor);
                            nextNeighbor = neighbor + directionVec;
                        }

                        nextNeighbor = neighbor + DirectionVectors[turnedDirection];
                    }
                }

                neighbors.Add(neighbor);
            }
        }

        // Debug log
        Debug.Log($"Neighbors for fromVertex {fromVertex}: {string.Join(", ", neighbors.Select(n => $"({n.x}, {n.y})"))}");

        return neighbors;
    }




    //// Check neighbors for solvability (used in DFS/BFS)
    //public List<Vector2Int> GetNeighbors(int x, int z)
    //{
    //    List<Vector2Int> neighbors = new List<Vector2Int>();

    //    // Check left neighbor (x - 1, z)
    //    if (x > 0 && !HasRightWall(x - 1, z)) // Check left neighbor's right wall
    //    {
    //        Debug.Log("Left");
    //        neighbors.Add(new Vector2Int(x - 1, z));
    //    }

    //    // Check right neighbor (x + 1, z)
    //    if (x < width - 1 && !HasRightWall(x, z)) // Check current cell's right wall
    //    {
    //        Debug.Log("Right");
    //        neighbors.Add(new Vector2Int(x + 1, z));
    //    }

    //    // Check bottom neighbor (x, z - 1)
    //    if (z > 0 && !HasBottomWall(x, z)) // Check current cell's bottom wall
    //    {
    //        Debug.Log("Bottom");
    //        neighbors.Add(new Vector2Int(x, z - 1));
    //    }

    //    // Check top neighbor (x, z + 1)
    //    if (z < height - 1 && !HasBottomWall(x, z)) // Check current cell's bottom wall
    //    {
    //        Debug.Log("Top");
    //        neighbors.Add(new Vector2Int(x, z + 1));
    //    }

    //    return neighbors;
    //}


}

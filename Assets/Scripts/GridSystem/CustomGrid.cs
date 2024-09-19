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
    private int startNode;
    private int endNode;
    private Material startNodeMaterial;
    private Material endNodeMaterial;
    private int cellSize;
    private Vector3 originPos;
    private int[,] gridArray;

    public bool[,] northWalls;
    public bool[,] westWalls;
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


    public CustomGrid(int width, int height, int startNode, Material startNodeMaterial, int endNode, Material endNodeMaterial, int cellSize, Vector3 originPos)
    {
        this.width = width;
        this.height = height;
        this.cellSize = cellSize;
        this.originPos = originPos;
        this.startNode = startNode;
        this.startNodeMaterial = startNodeMaterial;
        this.endNode = endNode;
        this.endNodeMaterial = endNodeMaterial;

        gridArray = new int[width, height];

        northWalls = new bool[width, height + 1];
        westWalls = new bool[width + 1, height];

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

    public int CellNumber(int x, int z)
    {
        return x + z * this.width;
    }

    public void ForAllCells(Action<int> action)
    {
        for (int cell = 0; cell < GetNumberOfCells(); cell++)
        {
            action(cell);
        }
    }

    public int GetNumberOfCells()
    {
        return width * height;
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
                return northWalls[x, z];
                case Direction.EAST:
                return westWalls[x + 1, z];
                case Direction.SOUTH:
                return northWalls[x, z + 1];
                case Direction.WEST:
                return westWalls[x, z];
        }
        return false;
    }

    public void SetWall(int x, int z, Direction direction, bool value)
    {
        switch (direction)
        {
            case Direction.NORTH:
                northWalls[x, z] = value;
                break;
            case Direction.EAST:
                westWalls[x + 1, z] = value;
                break;
            case Direction.SOUTH:
                northWalls[x, z + 1] = value;
                break;
            case Direction.WEST:
                westWalls[x, z] = value;
                break;
        }
    }

    public void SetWalls(int x, int z, bool value)
    {
        northWalls[x, z] = value;
        westWalls[x + 1, z] = value;
        northWalls[x, z + 1] = value;
        westWalls[x, z] = value;
    }

    public int[,] GetGrid()
    {
        return gridArray;
    }

    public int GetNumWalls(int x, int z)
    {
        int numWalls = 0;
        foreach (Direction dir in Enum.GetValues(typeof(Direction)))
        {
            numWalls += GetWall(x, z, dir) ? 1 : 0;
        }

        
        return numWalls;
    }

    public int GetDirectNeighbor(int x, int z, Direction dir)
    {
        int neighborX = x + DELTA_X[(int)dir];
        int neighborY = z + DELTA_Y[(int)dir];

        if (neighborX < 0 || neighborX >= this.width) return -1;
        if (neighborY < 0 || neighborY >= this.height) return -1;

        // compute neighbour node number
        return CellNumber(neighborX, neighborY);
    }

    public HashSet<int> GetNeighbors(int fromVertex)
    {
        HashSet<int> neighbors = new HashSet<int>();
        foreach (Direction direction in Enum.GetValues(typeof(CustomGrid.Direction)))
        {
            int nextNeighbor = GetDirectNeighbor(posX(fromVertex), posY(fromVertex), direction);
            if (nextNeighbor < 0 || GetWall(posX(fromVertex), posY(fromVertex), direction))
            {
                continue;
            }

            int neighbor, numWalls;

            do
            {
                neighbor = nextNeighbor;
                numWalls = GetNumWalls(posX(neighbor), posY(neighbor));
                nextNeighbor = GetDirectNeighbor(posX(neighbor), posY(neighbor), direction);
            } while (nextNeighbor >= 0 &&    // we have a further neighbour
                    !GetWall(posX(neighbor), posY(neighbor), direction) && // the passage continues
                    numWalls == NUM_DIRECTIONS - 2    // there is no junction or dead-end
            );

            Direction turnedDirection;
            if (numWalls == NUM_DIRECTIONS - 2)
            {
                // Try a perpendicular direction by shifting the enum ordinal
                turnedDirection = (Direction)(((int)direction + 1) % NUM_DIRECTIONS);

                // Check if a wall is blocking the turned direction
                if (GetWall(posX(neighbor), posY(neighbor), turnedDirection))
                {
                    // If blocked, try the other perpendicular direction
                    turnedDirection = (Direction)(((int)direction + NUM_DIRECTIONS - 1) % NUM_DIRECTIONS);
                }

                // Get the next neighbor in the turned direction
                int nextNeighbour = GetDirectNeighbor(posX(neighbor), posY(neighbor), turnedDirection);

                // While the next neighbour is valid and the passage continues without a junction or dead-end
                while (nextNeighbor >= 0 &&
                       !GetWall(posX(neighbor), posY(neighbor), turnedDirection) &&
                       numWalls == NUM_DIRECTIONS - 2)
                {
                    do
                    {
                        // Pass through to the next neighbor along the turned direction
                        neighbor = nextNeighbour;
                        numWalls = GetNumWalls(posX(neighbor), posY(neighbor));
                        nextNeighbour = GetDirectNeighbor(posX(neighbor), posY(neighbor), turnedDirection);
                    }
                    while (nextNeighbor >= 0 &&
                           !GetWall(posX(neighbor), posY(neighbor), turnedDirection) &&
                           numWalls == NUM_DIRECTIONS - 2);


                    // Try again around a corner along the original direction
                    nextNeighbour = GetDirectNeighbor(posX(neighbor), posY(neighbor), direction);

                    while (nextNeighbor >= 0 &&
                           !GetWall(posX(neighbor), posY(neighbor), direction) &&
                           numWalls == NUM_DIRECTIONS - 2)
                    {
                        // Pass through to the next neighbor along the original direction
                        neighbor = nextNeighbour;
                        numWalls = GetNumWalls(posX(neighbor), posY(neighbor));
                        nextNeighbour = GetDirectNeighbor(posX(neighbor), posY(neighbor), direction);
                    }

                    // Go back to the perpendicular direction and try again
                    nextNeighbour = GetDirectNeighbor(posX(neighbor), posY(neighbor), turnedDirection);
                }
            }
            neighbors.Add(neighbor);
        }
        return neighbors;
    }

    // DFS algorithm to find all relevant vertices
    public HashSet<int> GetAllVertices(int startVertex)
    {
        HashSet<int> visitedVertices = new HashSet<int>();
        // Start recursive traversal
        GetAllVerticesRecursive(startVertex, visitedVertices);
        return visitedVertices;
    }

    // Recursive method to traverse the maze/grid and collect all vertices
    private void GetAllVerticesRecursive(int currentVertex, HashSet<int> visitedVertices)
    {
        // Base case: If the current vertex is already visited, return
        if (visitedVertices.Contains(currentVertex))
        {
            return;
        }

        // Add the current vertex to the visited set
        visitedVertices.Add(currentVertex);

        // Get the neighbors of the current vertex using the GetNeighbors method
        HashSet<int> neighbors = GetNeighbors(currentVertex);

        // Recursively call this method for each unvisited neighbor
        foreach (var neighbor in neighbors)
        {
            GetAllVerticesRecursive(neighbor, visitedVertices);
        }
    }

    public HashSet<Vector2Int> GetAdjacentCells(int x, int z)
    {
        HashSet<Vector2Int> adjacentCells = new HashSet<Vector2Int>();
        foreach (Direction direction in Enum.GetValues(typeof(Direction)))
        {
            int neighborX = x + DELTA_X[(int)direction];
            int neighborZ = z + DELTA_Y[(int)direction];

            if (neighborX >= 0 && neighborX < width && neighborZ >= 0 && neighborZ < height)
            {
                adjacentCells.Add(new Vector2Int(neighborX, neighborZ));
            }
        }
        return adjacentCells;
    }


    public bool IsPositionInWall(Vector3 position)
    {
        // Convert world position to grid coordinates
        int[] coords = GetXZ(position);
        int x = coords[0];
        int z = coords[1];

        // Check boundaries to ensure coordinates are within the grid
        if (x < 0 || x >= width || z < 0 || z >= height)
        {
            return false; // Outside grid boundaries; adjust as needed for your logic
        }

        // Determine if position is inside a wall
        // Check for walls surrounding the cell
        bool isInWall = false;

        // Check north wall
        if (z + 1 < height && northWalls[x, z + 1])
        {
            isInWall = true;
        }
        // Check east wall
        else if (x + 1 < width && westWalls[x + 1, z])
        {
            isInWall = true;
        }
        // Check south wall
        else if (z > 0 && northWalls[x, z])
        {
            isInWall = true;
        }
        // Check west wall
        else if (x > 0 && westWalls[x, z])
        {
            isInWall = true;
        }

        return isInWall;
    }

    public int posX(int cell)
    {
        return cell % width;
    }

    public int posY(int cell)
    {
        return cell / width;
    }
}

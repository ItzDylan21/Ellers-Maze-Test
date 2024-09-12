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


    public CustomGrid(int width, int height, int startNode, int endNode, int cellSize, Vector3 originPos)
    {
        this.width = width;
        this.height = height;
        this.cellSize = cellSize;
        this.originPos = originPos;
        this.startNode = startNode;
        this.endNode = endNode;

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

    //public HashSet<int> GetNeighbors(int fromVertex)
    //{
    //    HashSet<int> neighbors = new HashSet<int>();
    //    int fromX = posX(fromVertex);
    //    int fromY = posY(fromVertex);

    //    // Iterate over all possible directions
    //    foreach (Direction direction in Enum.GetValues(typeof(Direction)))
    //    {
    //        // Get the direct neighbor in the current direction
    //        int nextNeighbor = GetDirectNeighbor(fromX, fromY, direction);

    //        // Check if the next neighbor is valid and not blocked by a wall
    //        if (nextNeighbor < 0 && GetWall(fromX, fromY, direction))
    //        {
    //            // Add the valid neighbor
    //            neighbors.Add(nextNeighbor);

    //            // Continue traversing in the same direction
    //            int currentVertex = nextNeighbor;
    //            Direction[] perpendicularDirections = GetPerpendicularDirections(direction);
    //            bool traversedInDirection = false;

    //            while (true)
    //            {
    //                // Move to the next vertex in the current direction
    //                int nextVertex = GetDirectNeighbor(posX(currentVertex), posY(currentVertex), direction);

    //                // Break if the next vertex is invalid or blocked
    //                if (nextVertex < 0 || GetWall(posX(currentVertex), posY(currentVertex), direction))
    //                {
    //                    break;
    //                }

    //                // Add the valid vertex and continue
    //                neighbors.Add(nextVertex);
    //                currentVertex = nextVertex;

    //                // Check perpendicular directions if not yet done
    //                if (!traversedInDirection)
    //                {
    //                    foreach (Direction perpendicularDirection in perpendicularDirections)
    //                    {
    //                        int perpendicularNeighbor = GetDirectNeighbor(posX(currentVertex), posY(currentVertex), perpendicularDirection);

    //                        if (perpendicularNeighbor >= 0 && !GetWall(posX(currentVertex), posY(currentVertex), perpendicularDirection))
    //                        {
    //                            // Add perpendicular neighbors if valid
    //                            neighbors.Add(perpendicularNeighbor);
    //                        }
    //                    }
    //                    traversedInDirection = true; // Ensure perpendicular directions are checked once
    //                }
    //            }
    //        }
    //    }
    //    return neighbors;
    //}

    // Helper method to get perpendicular directions
    private Direction[] GetPerpendicularDirections(Direction direction)
    {
        // Assuming 4 directions: NORTH, EAST, SOUTH, WEST
        switch (direction)
        {
            case Direction.NORTH:
                return new[] { Direction.WEST, Direction.EAST };
            case Direction.EAST:
                return new[] { Direction.NORTH, Direction.SOUTH };
            case Direction.SOUTH:
                return new[] { Direction.EAST, Direction.WEST };
            case Direction.WEST:
                return new[] { Direction.SOUTH, Direction.NORTH };
            default:
                return new Direction[0];
        }
    }



    // Method to get all reachable vertices from a starting point using DFS
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
        Debug.Log($"{posX(currentVertex)}, {posY(currentVertex)}");
        // Base case: If the current vertex is already visited, return
        if (visitedVertices.Contains(currentVertex))
        {
            return;
        }

        // Add the current vertex to the visited set
        visitedVertices.Add(currentVertex);

        // Get the neighbors of the current vertex using the provided GetNeighbors method
        HashSet<int> neighbors = GetNeighbors(currentVertex);

        // Recursively call this method for each unvisited neighbor
        foreach (var neighbor in neighbors)
        {
            GetAllVerticesRecursive(neighbor, visitedVertices);
        }
    }

    public int posX(int cell)
    {
        return cell % width;
    }

    public int posY(int cell)
    {
        return cell / width;
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

using UnityEngine;
using System;
using System.Collections.Generic;
using Unity.XR.CoreUtils;
using UnityEngine.XR.Interaction.Toolkit.Interactables;
using UnityEngine.XR.Interaction.Toolkit;
using UnityEngine.UI;
using UnityEditor.Experimental.GraphView;
using UnityEngine.XR.Interaction.Toolkit.Interactors;
using Unity.VisualScripting;

public class PrimMazeGenerator : MonoBehaviour
{
    [SerializeField] GridSpawner gridSpawner;
    public GameObject wallPrefab;
    public GameObject cubePrefab;
    public GameObject winScreenPrefab;
    public XROrigin xrOrigin;
    private GameObject spawnedCube;
    private List<GameObject> instantiatedWalls = new List<GameObject>();
    private GameObject combinedObject;
    private CustomGrid gridXZ;

    [SerializeField] GameObject gridCellIndicator;
    private GameObject startIndicator;
    private GameObject endIndicator;
    private GameObject winScreen;

    [SerializeField] int WallYValue;

    private bool isCubeHeld = false;
    private bool hasShownWinScreen = false;
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
        SpawnInteractableCube();
    }

    private void Update()
    {
        CheckEndNodeReached();
    }

    private void SpawnInteractableCube()
    {
        // Ensure startNode and endNode are valid
        if (gridSpawner.startNode < 0 || gridSpawner.startNode >= gridXZ.GetNumberOfCells() ||
            gridSpawner.endNode < 0 || gridSpawner.endNode >= gridXZ.GetNumberOfCells())
        {
            Debug.LogError("Start or end node is out of bounds.");
            return;
        }

        // Get positions of the start node and end node
        int startNodeX = gridXZ.posX(gridSpawner.startNode);
        int startNodeZ = gridXZ.posY(gridSpawner.startNode);
        int endNodeX = gridXZ.posX(gridSpawner.endNode);
        int endNodeZ = gridXZ.posY(gridSpawner.endNode);

        // Get adjacent cells to the end node and start node
        HashSet<Vector2Int> adjacentToEndCells = new HashSet<Vector2Int>(gridXZ.GetAdjacentCells(endNodeX, endNodeZ));
        HashSet<Vector2Int> adjacentToStartCells = new HashSet<Vector2Int>(gridXZ.GetAdjacentCells(startNodeX, startNodeZ));

        // List of valid positions for the cube
        List<Vector3> validPositions = new List<Vector3>();

        // Check all cells and filter out those adjacent to the start node or end node
        for (int x = 0; x < gridSpawner.width; x++)
        {
            for (int z = 0; z < gridSpawner.height; z++)
            {
                // Skip if the cell is the start node or end node, or adjacent to either node
                if ((x == startNodeX && z == startNodeZ) ||
                    (x == endNodeX && z == endNodeZ) ||
                    adjacentToStartCells.Contains(new Vector2Int(x, z)) ||
                    adjacentToEndCells.Contains(new Vector2Int(x, z)))
                {
                    continue;
                }

                // Generate a random offset within 75% of the cell size
                float cellSize = gridXZ.GetCellSize();
                float offsetX = UnityEngine.Random.Range(-0.75f * cellSize / 2, 0.75f * cellSize / 2);
                float offsetZ = UnityEngine.Random.Range(-0.75f * cellSize / 2, 0.75f * cellSize / 2);

                Vector3 position = gridXZ.GetWorldPos(x, z) + new Vector3(cellSize / 2 + offsetX, 0, cellSize / 2 + offsetZ);

                // Ensure the position is not within a wall
                if (!gridXZ.IsPositionInWall(position))
                {
                    validPositions.Add(position);
                }
            }
        }

        // If no valid positions, include cells adjacent to the end node, but still exclude start node's adjacent cells
        if (validPositions.Count == 0)
        {
            foreach (Vector2Int adjacentCell in adjacentToEndCells)
            {
                // Skip if the adjacent cell is the start node or adjacent to the start node
                if ((adjacentCell.x == startNodeX && adjacentCell.y == startNodeZ) || adjacentToStartCells.Contains(adjacentCell))
                {
                    continue;
                }

                float cellSize = gridXZ.GetCellSize();
                Vector3 position = gridXZ.GetWorldPos(adjacentCell.x, adjacentCell.y) + new Vector3(cellSize / 2, 0, cellSize / 2);

                if (!gridXZ.IsPositionInWall(position))
                {
                    validPositions.Add(position);
                }
            }
        }

        // If still no valid positions, fallback to the end node or start node
        if (validPositions.Count == 0)
        {
            Vector3 startNodePosition = gridXZ.GetWorldPos(startNodeX, startNodeZ) + new Vector3(gridXZ.GetCellSize() / 2, 0, gridXZ.GetCellSize() / 2);
            if (!gridXZ.IsPositionInWall(startNodePosition) && !adjacentToStartCells.Contains(new Vector2Int(startNodeX, startNodeZ)))
            {
                validPositions.Add(startNodePosition);
            }

            Vector3 endNodePosition = gridXZ.GetWorldPos(endNodeX, endNodeZ) + new Vector3(gridXZ.GetCellSize() / 2, 0, gridXZ.GetCellSize() / 2);
            if (!gridXZ.IsPositionInWall(endNodePosition) && !adjacentToEndCells.Contains(new Vector2Int(endNodeX, endNodeZ)))
            {
                validPositions.Add(endNodePosition);
            }
        }

        // If we have valid positions, instantiate the cube at a random valid position
        if (validPositions.Count > 0)
        {
            Vector3 spawnPosition = validPositions[UnityEngine.Random.Range(0, validPositions.Count)];
            spawnPosition.y += 1.0f; // Raise the cube in the air

            // Instantiate the cube
            spawnedCube = Instantiate(cubePrefab, spawnPosition, Quaternion.identity);
            Rigidbody rb = spawnedCube.AddComponent<Rigidbody>();
            rb.collisionDetectionMode = CollisionDetectionMode.Continuous; // Prevent tunneling
            rb.useGravity = false; // Disable gravity initially

            // Add XRGrabInteractable component
            XRGrabInteractable grabInteractable = spawnedCube.AddComponent<XRGrabInteractable>();
            grabInteractable.selectEntered.AddListener(OnCubeGrabbed);
            grabInteractable.selectExited.AddListener(OnCubeReleased);

            Transform attachPoint = spawnedCube.transform.Find("Torus"); // Change "AttachPoint" to your child object name
            if (attachPoint != null)
            {
                grabInteractable.attachTransform = attachPoint; // Set the attach point
            }

            grabInteractable.movementType = XRBaseInteractable.MovementType.Instantaneous; // Set to snap
            grabInteractable.throwOnDetach = false;

            RotatingCube rotatingScript = spawnedCube.AddComponent<RotatingCube>();

            ParticleSystem particleSystem = spawnedCube.AddComponent<ParticleSystem>();
            
            particleSystem.gameObject.layer = LayerMask.NameToLayer("Default");
            ParticleSystem.MainModule mainModule = particleSystem.main;
            mainModule.startSize = 0.04f;
            mainModule.startSpeed = 1f;
            mainModule.maxParticles = 60;

            ParticleSystem.EmissionModule emissionModule = particleSystem.emission;
            emissionModule.rateOverTime = 30;

            ParticleSystem.ShapeModule shapeModule = particleSystem.shape;
            shapeModule.shapeType = ParticleSystemShapeType.Sphere;
            shapeModule.radius = 1.5f;

            Renderer particleRenderer = particleSystem.GetComponent<Renderer>();
            particleRenderer.material = new Material(Shader.Find("Particles/Standard Unlit"));

            mainModule.startLifetime = 4f; 
            mainModule.startSpeed = 0.5f;  

            // Add gradual fade-out to particles
            ParticleSystem.ColorOverLifetimeModule colorModule = particleSystem.colorOverLifetime;
            colorModule.enabled = true;
            Gradient gradient = new Gradient();
            gradient.SetKeys(
                new GradientColorKey[] { new GradientColorKey(Color.yellow, 0.0f), new GradientColorKey(Color.clear, 1.0f) },
                new GradientAlphaKey[] { new GradientAlphaKey(1.0f, 0.0f), new GradientAlphaKey(0.0f, 1.0f) }
            );
            colorModule.color = new ParticleSystem.MinMaxGradient(gradient);

            // Store references for control during grab/release
            grabInteractable.selectEntered.AddListener((SelectEnterEventArgs args) =>
            {
                rotatingScript.enabled = false; // Stop rotation

                // Detach the particle system from the cube to keep it in place
                particleSystem.transform.parent = null; // Unparent from cube
                particleSystem.transform.position = spawnedCube.transform.position; // Keep in the same world position
            
                var emissionModule = particleSystem.emission;
                emissionModule.enabled = false; // Stop emitting new particles
            });

            grabInteractable.selectExited.AddListener((SelectExitEventArgs args) =>
            {
                //rotatingScript.enabled = true;  // Resume rotation (if needed)

                var emissionModule = particleSystem.emission;
                emissionModule.enabled = true;

                rb.useGravity = true;
            });
        }
        else
        {
            Debug.LogWarning("No valid positions available for cube spawning.");
        }
    }





    private void OnCubeGrabbed(SelectEnterEventArgs args)
    {
        isCubeHeld = true; // The cube has been grabbed
        Debug.Log("Cube grabbed!");
    }

    private void OnCubeReleased(SelectExitEventArgs args)
    {
        isCubeHeld = false; // The cube has been released
        Debug.Log("Cube released!");
    }

    private void DespawnCurrentObjects()
    {
        instantiatedWalls = new List<GameObject>();

        if (combinedObject != null)
        {
            Destroy(combinedObject);
            combinedObject = null;
        }
        if (startIndicator != null)
        {
            Destroy(startIndicator);
            startIndicator = null;
        }

        if (endIndicator != null)
        {
            Destroy(endIndicator);
            endIndicator = null;
        }

        if (spawnedCube != null)
        {
            Destroy(spawnedCube);
            spawnedCube = null;
        }

        if (winScreen != null)
        {
            Destroy(winScreen);
            hasShownWinScreen = false;
            winScreen = null;
        }
    }

    private void CheckEndNodeReached()
    {
        // Ensure the XR Origin reference is not null
        if (xrOrigin == null)
        {
            Debug.LogError("XR Origin reference is missing!");
            return;
        }

        // Get the world position of the end node
        Vector3 endNodePosition = gridXZ.GetWorldPos(gridXZ.posX(gridSpawner.endNode), gridXZ.posY(gridSpawner.endNode))
                                   + new Vector3(gridXZ.GetCellSize() / 2, 0, gridXZ.GetCellSize() / 2);

        // Get the player's position from XR Origin
        Vector3 playerPosition = xrOrigin.transform.position;

        // Define a threshold radius for triggering the win condition
        float triggerRadius = gridXZ.GetCellSize() / 2; // Adjust radius as needed

        // Check if player is within the trigger radius and has the object
        if (Vector3.Distance(playerPosition, endNodePosition) < triggerRadius && isCubeHeld)
        {
            ShowWinScreen();
        }
        else if (Vector3.Distance(playerPosition, endNodePosition) < triggerRadius && !isCubeHeld)
        {
            Debug.Log("You need to collect the object before finishing the maze!");
        }
    }


    public void GenerateMaze()
    {
        DespawnCurrentObjects();

        InitializeWalls();

        GenerateRandomizedPrim();

        ConfigureEntryAndExit();

        RemoveRandomWalls(gridSpawner.width);

        VertexText(gridXZ.GetAllVertices(gridSpawner.startNode), Color.yellow, 1);

        // Try finding a path
        if (FindAndDisplayPath())
        {
            Debug.Log("Path found and displayed successfully.");
        }
        else
        {
            // Retry pathfinding with additional wall removal
            int retryCount = 0;
            bool pathFound = false;
            while (retryCount < 10 && !pathFound)
            {
                // Remove a smaller number of random walls to increase the chance of finding a path
                RemoveRandomWalls(gridSpawner.width * 3);

                VertexText(gridXZ.GetAllVertices(gridSpawner.startNode), Color.yellow, 1);

                pathFound = FindAndDisplayPath();
                retryCount++;
            }

            if (!pathFound)
            {
                GenerateMaze();
            }
        }

        BuildMazeInUnity();
    }

    private void UpdatePathAndVisitedTexts(GPath path)
    {
        // Update text colors for visited vertices
        foreach (var vertex in path.Visited)
        {
            Vector2Int coords = new Vector2Int(gridXZ.posX(vertex), gridXZ.posY(vertex));
            gridXZ.UpdateTextColor(coords, Color.cyan);
        }

        // Update text colors for path vertices
        foreach (var vertex in path.Vertices)
        {
            Vector2Int coords = new Vector2Int(gridXZ.posX(vertex), gridXZ.posY(vertex));
            gridXZ.UpdateTextColor(coords, Color.red);
        }

        // Color end node vertex red
        Vector2Int endCoords = new Vector2Int(gridXZ.posX(gridSpawner.endNode), gridXZ.posY(gridSpawner.endNode));
        gridXZ.UpdateTextColor(endCoords, Color.red);
    }

    private bool FindAndDisplayPath()
    {
        GPath path = BreadthFirstSearch(gridSpawner.startNode, gridSpawner.endNode);
        if (path != null)
        {
            Debug.Log("Path found!");

            UpdatePathAndVisitedTexts(path);

            return true;
        }
        else
        {
            Debug.Log("No path found.");
            return false;
        }
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
        // Set the start node
        gridSpawner.startNode = gridXZ.CellNumber(2 * gridSpawner.width / 3, 2 * gridSpawner.height / 3);
        SetWalls(gridSpawner.startNode, false); // Remove walls at start node

        // Position the start node indicator
        Vector3 startNodePosition = gridXZ.GetWorldPos(gridXZ.posX(gridSpawner.startNode), gridXZ.posY(gridSpawner.startNode));
        startIndicator = Instantiate(gridCellIndicator, startNodePosition, Quaternion.identity);

        // Move position to center of the cell
        Vector3 cellCenterOffset = new Vector3(gridXZ.GetCellSize() / 2, 0, gridXZ.GetCellSize() / 2);
        startNodePosition += cellCenterOffset; 

        // Find the child object that has the MeshRenderer (if the plane is nested)
        Transform startPlane = startIndicator.transform.Find("Plane"); // Assuming the plane's name is "Plane"
        if (startPlane != null)
        {
            MeshRenderer startMeshRenderer = startPlane.GetComponent<MeshRenderer>();
            if (startMeshRenderer != null && gridSpawner.startNodeMaterial != null)
            {
                startMeshRenderer.material = gridSpawner.startNodeMaterial;  // Assign green material
                Debug.Log("Assigned green material to start node");
            }
            else
            {
                Debug.LogError("MeshRenderer or startNodeMaterial is missing on the start node plane!");
            }
        }
        else
        {
            Debug.LogError("Child object 'Plane' not found in startIndicator prefab!");
        }

        // Set the player's spawn point to the center of the start node
        if (xrOrigin != null)
        {
            Vector3 playerSpawnPosition = new Vector3(startNodePosition.x, xrOrigin.transform.position.y, startNodePosition.z); // Adjust Y to maintain current height
            xrOrigin.transform.position = playerSpawnPosition;
            Debug.Log($"Player spawn point set to center of start node at {playerSpawnPosition}");
        }
        else
        {
            Debug.LogError("XR Origin (player) reference is missing!");
        }

        // Set the end node
        bool choice = randomizer.Next(0, 2) == 1;
        CustomGrid.Direction exitDirection = choice ? CustomGrid.Direction.NORTH : CustomGrid.Direction.WEST;
        int exitX = choice ? randomizer.Next(gridSpawner.width) : 0;
        int exitY = choice ? 0 : randomizer.Next(gridSpawner.height);

        gridXZ.SetWall(exitX, exitY, exitDirection, false);
        gridSpawner.endNode = gridXZ.CellNumber(exitX, exitY);

        // Position the end node indicator
        Vector3 endNodePosition = gridXZ.GetWorldPos(exitX, exitY);
        endIndicator = Instantiate(gridCellIndicator, endNodePosition, Quaternion.identity);

        // Adjust the endNodePosition to the center of the cell
        endNodePosition += cellCenterOffset; // Move to center of the cell

        // Find the child object that has the MeshRenderer (if the plane is nested)
        Transform endPlane = endIndicator.transform.Find("Plane"); // Assuming the plane's name is "Plane"
        if (endPlane != null)
        {
            MeshRenderer endMeshRenderer = endPlane.GetComponent<MeshRenderer>();
            if (endMeshRenderer != null && gridSpawner.endNodeMaterial != null)
            {
                endMeshRenderer.material = gridSpawner.endNodeMaterial;  // Assign red material
                Debug.Log("Assigned red material to end node");
            }
            else
            {
                Debug.LogError("MeshRenderer or endNodeMaterial is missing on the end node plane!");
            }
        }
        else
        {
            Debug.LogError("Child object 'Plane' not found in endIndicator prefab!");
        }

        // Set up a trigger on the end node to detect when the player steps on it
        GameObject endTrigger = new GameObject("EndNodeTrigger");
        BoxCollider triggerCollider = endTrigger.AddComponent<BoxCollider>();
        triggerCollider.isTrigger = true;
        endTrigger.transform.position = gridXZ.GetWorldPos(exitX, exitY);
        endTrigger.AddComponent<EndNodeTrigger>().Initialize(this, xrOrigin);
    }

    public void OnPlayerSteppedOnEndNode()
    {
        if (isCubeHeld) // Check if the player is holding the cube
        {
            Debug.Log("Player has reached the end with the cube! You win!");
            ShowWinScreen(); // Show win screen and button to proceed to the next maze
        }
        else
        {
            Debug.Log("Player reached the end, but no cube!");
        }
    }

    private void ShowWinScreen()
    {
        if (hasShownWinScreen)
        {
            return; // Exit if the win screen has already been shown
        }

        // Get the position of the end node
        Vector3 endNodePosition = gridXZ.GetWorldPos(gridXZ.posX(gridSpawner.endNode), gridXZ.posY(gridSpawner.endNode))
                                   + new Vector3(gridXZ.GetCellSize() / 2, 0, gridXZ.GetCellSize() / 2);

        // Set the height where the win screen should appear
        float winScreenHeight = 2.0f; // Adjust this height as needed
        endNodePosition.y += winScreenHeight;

        // Instantiate the win screen at the end node
        winScreen = Instantiate(winScreenPrefab, endNodePosition, Quaternion.identity);

        winScreen.transform.rotation = Quaternion.Euler(0, 180, 0);
        winScreen.AddComponent<LookAtPlayer>().playerCamera = xrOrigin.Camera.transform;

        // Find the button and add a listener to regenerate the maze
        Button proceedButton = winScreen.GetComponentInChildren<Button>();
        if (proceedButton != null)
        {
            proceedButton.onClick.AddListener(() =>
            {
                gridXZ = gridSpawner.grid;

                GenerateMaze(); // Generate a new maze
                SpawnInteractableCube(); // Spawn a new interactable cube
            });
        }
        else
        {
            Debug.LogError("Button not found in win screen prefab!");
        }

        hasShownWinScreen = true; // Set the flag to true after showing the win screen
    }


    private void BuildMazeInUnity()
    {
        List<CombineInstance> combinedInstances = new List<CombineInstance>();

        // Build north walls
        for (int x = 0; x < gridSpawner.width; x++)
        {
            for (int y = 0; y <= gridSpawner.height; y++)
            {
                if (gridXZ.northWalls[x, y])
                {
                    Vector3 position = gridXZ.GetWorldPos(x, y);
                    GameObject northWall = Instantiate(wallPrefab, position, Quaternion.LookRotation(Vector3.right));

                    AddMeshToCombine(northWall, combinedInstances);

                    instantiatedWalls.Add(northWall);
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
                    GameObject westWall = Instantiate(wallPrefab, position, Quaternion.LookRotation(Vector3.forward));

                    AddMeshToCombine(westWall, combinedInstances);

                    instantiatedWalls.Add(westWall);
                }
            }
        }

        combinedObject = new GameObject("CombinedWalls");
        MeshFilter meshFilter = combinedObject.AddComponent<MeshFilter>();
        MeshRenderer meshRenderer = combinedObject.AddComponent<MeshRenderer>();

        Mesh combinedMesh = new Mesh
        {
            indexFormat = UnityEngine.Rendering.IndexFormat.UInt32
        };
        combinedMesh.CombineMeshes(combinedInstances.ToArray(), true, true);

        meshFilter.mesh = combinedMesh;
        meshRenderer.material = wallPrefab.GetComponentInChildren<MeshRenderer>().sharedMaterial;

        MeshCollider meshCollider = combinedObject.AddComponent<MeshCollider>();
        meshCollider.sharedMesh = combinedMesh;

        foreach (GameObject wall in instantiatedWalls)
        {
            Destroy(wall);
        }
    }

    private void AddMeshToCombine(GameObject wallObject, List<CombineInstance> combineInstances)
    {
        // Find the cube (mesh) child inside the wall prefab
        MeshFilter childMeshFilter = wallObject.GetComponentInChildren<MeshFilter>();

        if (childMeshFilter != null)
        {
            CombineInstance combineInstance = new CombineInstance
            {
                mesh = childMeshFilter.sharedMesh,
                transform = childMeshFilter.transform.localToWorldMatrix
            };
            combineInstances.Add(combineInstance);
        }
        else
        {
            Debug.LogWarning("No MeshFilter found in the child of the wall prefab.");
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

        // Check if we already have a GridText object
        GameObject gridTextObj = GameObject.Find("GridText");
        if (gridTextObj == null)
        {
            gridTextObj = new GameObject("GridText");
        }
        else
        {
            // Clear old text objects if they exist
            foreach (Transform child in gridTextObj.transform)
            {
                GameObject.Destroy(child.gameObject);
            }
        }

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


    private void EndOfMazeUI(HashSet<int> vertices, Color textColor, int yVal)
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

    private CustomGrid.Direction GetDirection(int fromX, int fromY, int toX, int toY)
    {
        if (fromX == toX)
        {
            return fromY < toY ? CustomGrid.Direction.NORTH : CustomGrid.Direction.SOUTH;
        }
        else
        {
            return fromX < toX ? CustomGrid.Direction.EAST : CustomGrid.Direction.WEST;
        }
    }

    private bool IsWallBetween(int fromVertex, int toVertex)
    {
        int fromX = gridXZ.posX(fromVertex);
        int fromY = gridXZ.posY(fromVertex);
        int toX = gridXZ.posX(toVertex);
        int toY = gridXZ.posY(toVertex);

        // Determine the direction of movement
        CustomGrid.Direction direction = GetDirection(fromX, fromY, toX, toY);

        // Check if there is a wall in the direction of movement
        return gridXZ.GetWall(fromX, fromY, direction);
    }

    // BFS Pathfinding Method
    public GPath BreadthFirstSearch(int startVertex, int targetVertex)
    {
        if (startVertex == -1 || targetVertex == -1) return null;

        GPath path = new GPath();
        path.Vertices.Add(startVertex);
        path.Visited.Add(startVertex);

        if (targetVertex == startVertex) return path;

        Queue<int> fifoQueue = new Queue<int>();
        Dictionary<int, int> visitedFrom = new Dictionary<int, int>();

        fifoQueue.Enqueue(startVertex);
        visitedFrom[startVertex] = -1;

        while (fifoQueue.Count > 0)
        {
            int current = fifoQueue.Dequeue();

            foreach (var neighbor in gridXZ.GetNeighbors(current))
            {
                path.Visited.Add(neighbor);

                if (neighbor == targetVertex)
                {
                    int trace = current;
                    while (trace != -1)
                    {
                        path.Vertices.Add(trace);
                        trace = visitedFrom[trace];
                    }
                    path.Vertices.Reverse();
                    return path;
                }
                else if (!visitedFrom.ContainsKey(neighbor))
                {
                    visitedFrom[neighbor] = current;
                    fifoQueue.Enqueue(neighbor);
                }
            }
        }
        return null;
    }

}

// Helper class to represent path and visited vertices
public class GPath
{
    public List<int> Vertices { get; set; } = new List<int>();
    public HashSet<int> Visited { get; set; } = new HashSet<int>();
}

using System.Collections.Generic;
using UnityEngine;
using Unity.Mathematics;
using NaughtyAttributes;
using Random = Unity.Mathematics.Random;
using System.Collections;
using System;
using System.Linq;
using UnityEngine.UIElements;
using UnityEngine.Events;

public class DungeonGenerator : MonoBehaviour
{
    public static DungeonGenerator instance;
    private void Awake() => instance = this;

    [Header("General")]
    public int seed;
    Random rng;

    public Vector2Int size = new Vector2Int(100, 50);
    public Vector2Int maxRoomSize = new Vector2Int(10, 10);

    public UnityEvent OnGenerationDone;

    [Header("Generation")]
    public List<RectInt> generatedRooms = new List<RectInt>();
    public float maxSplitOffset;
    bool generationFinished = false;

    [Header("Graph")]
    public Graph<RectInt> graph = new();
    bool graphFinished = false;

    [Header("Room Removal")]
    public float removePercentage;
    bool removalFinished = false;

    [Header("Path Removal")]
    [Tooltip("Switches between DFS and BFS")]
    public bool useDFS;
    bool removedCyclicPaths = false;

    [Header("Doors")]
    public int doorArea;
    public List<Door> doors = new();

    [Header("Assets")]
    public GameObject floor;

    public GameObject crossWall;

    public GameObject tWall;

    public GameObject straightWall;

    public GameObject cornerWall;

    public GameObject endWall;

    public GameObject standaloneWall;

    GameObject assetParent;

    [Header("Decorations")]
    public int maxDecorationsPerRoom;
    public GameObject[] decorationObjects;
    List<Vector3> spawnedDecorationPositions = new();

    [HorizontalLine]
    [Header("Navigation")]
    public Graph<Vector3> navigationGraph = new();

    [HorizontalLine]
    [Header("Animation")]
    public bool doAnimation;
    public float waitTime;

    Vector2Int[] orthogonalDirections = new Vector2Int[4]
    {
        new Vector2Int(1, 0),
        new Vector2Int(-1, 0),
        new Vector2Int(0, 1),
        new Vector2Int(0, -1)
    };

    Vector2Int[] diagonalDirections = new Vector2Int[4]
    {
        new Vector2Int(1, 1),
        new Vector2Int(-1, 1),
        new Vector2Int(1, -1),
        new Vector2Int(-1, -1)
    };

    IEnumerator Start()
    {
        Initilize();

        yield return StartCoroutine(CreateRooms());

        yield return StartCoroutine(GenerateGraph());

        yield return StartCoroutine(RemoveRooms());

        yield return StartCoroutine(RemoveCyclicPaths());

        yield return StartCoroutine(SpawnDoors());

        yield return StartCoroutine(SpawnAssets());

        CreateNavigationGraph();

        OnGenerationDone.Invoke();
    }

    void Initilize()
    {
        rng = new Random(Convert.ToUInt32(seed));

        RectInt startRoom = new RectInt(0, 0, size.x, size.y);
        roomsToCheck.Add(startRoom);
    }

    List<RectInt> roomsToCheck = new List<RectInt>();
    IEnumerator CreateRooms()
    {
        while (roomsToCheck.Count > 0)
        {
            List<RectInt> newRooms = new List<RectInt>();
            foreach (RectInt room in roomsToCheck)
            {
                RectInt[] createdRooms = new RectInt[0];
                if (room.width > maxRoomSize.x && room.height > maxRoomSize.y) // both dimensions are too big - choose a random one
                {
                    bool splitHorizontal = Mathf.Round(rng.NextFloat()) == 1;

                    if (splitHorizontal) createdRooms = SplitHorizontally(room);
                    else createdRooms = SplitVertically(room);
                }
                else if (room.width > maxRoomSize.x) // width is too big
                {
                    createdRooms = SplitVertically(room);
                }
                else if (room.height > maxRoomSize.y) // height is too big
                {
                    createdRooms = SplitHorizontally(room);
                }
                else // room is finished - move to generatedRooms
                {
                    generatedRooms.Add(room);
                }

                newRooms.AddRange(createdRooms);

                foreach (RectInt room2 in roomsToCheck) { AlgorithmsUtils.DebugRectInt(room2, Color.yellow, waitTime); }

                foreach (RectInt createdRoom in newRooms) { AlgorithmsUtils.DebugRectInt(createdRoom, Color.cyan, waitTime); }

                if(doAnimation) yield return new WaitForSeconds(waitTime);
            }
            roomsToCheck = newRooms;
        }

        generationFinished = true;
    }
    RectInt[] SplitVertically(RectInt room)
    {
        RectInt[] newRooms = new RectInt[2];

        float splitRatio = 2 + rng.NextFloat(-maxSplitOffset, maxSplitOffset);

        RectInt newRoom1 = new RectInt(room.x, room.y, Mathf.RoundToInt((float)(room.width / splitRatio)), room.height);
        RectInt newRoom2 = new RectInt(room.x + newRoom1.width - 1, room.y, room.width - newRoom1.width + 1, room.height);

        newRooms[0] = newRoom1;
        newRooms[1] = newRoom2;

        return newRooms;
    }
    RectInt[] SplitHorizontally(RectInt room)
    {
        RectInt[] newRooms = new RectInt[2];

        float splitRatio = 2 + rng.NextFloat(-maxSplitOffset, maxSplitOffset);

        RectInt newRoom1 = new RectInt(room.x, room.y, room.width, Mathf.RoundToInt((float)(room.height / splitRatio)));
        RectInt newRoom2 = new RectInt(room.x, room.y + newRoom1.height - 1, room.width, room.height - newRoom1.height + 1);

        newRooms[0] = newRoom1;
        newRooms[1] = newRoom2;

        return newRooms;
    }

    IEnumerator GenerateGraph()
    {
        foreach(RectInt room1 in generatedRooms)
        {
            graph.AddNode(room1);
            foreach(RectInt room2 in generatedRooms)
            {
                if (room1 == room2) continue;

                RectInt intersection = AlgorithmsUtils.Intersect(room1, room2);

                if (intersection.width * intersection.height >= doorArea) // the rooms are connectable
                {
                    graph.AddEdge(room1, room2);
                }

                AlgorithmsUtils.DebugRectInt(room1, Color.cyan, waitTime, false, 1);
                //AlgorithmsUtils.DebugRectInt(room2, Color.cyan, waitTime, false, 1);

                //if (doAnimation) yield return new WaitForSeconds(waitTime);
            }
            if (doAnimation) yield return new WaitForSeconds(waitTime);
        }

        graphFinished = true;
    }

    IEnumerator RemoveRooms()
    {
        int AmountToRemove = Mathf.RoundToInt(generatedRooms.Count * (removePercentage / 100f));
        List<RectInt> orderedRooms = new(graph.GetNodes());
        orderedRooms = orderedRooms.OrderBy(x => x.width * x.height).ToList();

        int indexToRemove = 0;
        for (int i = 0; i < AmountToRemove - 1; i++)
        {
            if (indexToRemove >= orderedRooms.Count) break;

            RectInt roomToRemove = orderedRooms[indexToRemove];

            RectInt[] connectedRooms = graph.GetNeighbors(roomToRemove).ToArray();

            graph.RemoveNode(roomToRemove);

            if(!graph.BFS(graph.GetNodes()[0])) // if the graph is no longer fully connected
            {
                // add the node and it's connections back to the graph
                graph.AddNode(roomToRemove);
                foreach (RectInt room in connectedRooms)
                {
                    graph.AddEdge(roomToRemove, room);
                    graph.AddEdge(room, roomToRemove);
                }

                i--; //retry
            }

            indexToRemove++;

            if (doAnimation) yield return new WaitForSeconds(waitTime);
        }

        removalFinished = true;
    }

    IEnumerator RemoveCyclicPaths()
    {
        Graph<RectInt> newGraph = new Graph<RectInt>();

        // generate new edges/paths
        if(useDFS) // use DFS
        {
            Stack<RectInt> stack = new();

            HashSet<RectInt> discovered = new();

            RectInt startNode = graph.GetNodes()[0];

            stack.Push(startNode);

            discovered.Add(startNode);

            while (stack.Count > 0)
            {
                RectInt node = stack.Pop();

                foreach (RectInt connectedNode in graph.GetNeighbors(node))
                {
                    if (!discovered.Contains(connectedNode)) // found a new room
                    {
                        newGraph.AddEdge(node, connectedNode); // add the connection

                        stack.Push(connectedNode);
                        discovered.Add(connectedNode);
                    }

                    DrawGraph(newGraph, waitTime);
                    if (doAnimation) yield return new WaitForSeconds(waitTime);
                }

                DrawGraph(newGraph, waitTime);
                if (doAnimation) yield return new WaitForSeconds(waitTime);
            }
        }
        else // use BFS
        {
            Queue<RectInt> queue = new();

            HashSet<RectInt> discovered = new();

            RectInt startNode = graph.GetNodes()[0];

            queue.Enqueue(startNode);
            discovered.Add(startNode);

            while (queue.Count > 0)
            {
                RectInt node = queue.Dequeue();

                foreach (RectInt connectedNode in graph.GetNeighbors(node))
                {
                    if (!discovered.Contains(connectedNode)) // found a new room
                    {
                        newGraph.AddEdge(node, connectedNode); // add the connection

                        queue.Enqueue(connectedNode);
                        discovered.Add(connectedNode);
                    }

                    DrawGraph(newGraph, waitTime);
                    if (doAnimation) yield return new WaitForSeconds(waitTime);
                }

                DrawGraph(newGraph, waitTime);
                if (doAnimation) yield return new WaitForSeconds(waitTime);
            }
        }

        // replace the old graph
        graph = newGraph;

        removedCyclicPaths = true;
    }

    IEnumerator SpawnDoors()
    {
        foreach(RectInt node in graph.GetNodes())
        {
            foreach(RectInt connectedNode in graph.GetNeighbors(node))
            {
                Door newDoor = new();

                newDoor.room1 = node;
                newDoor.room2 = connectedNode;

                RectInt intersection = AlgorithmsUtils.Intersect(node, connectedNode);

                newDoor.rect = intersection.height == 1 ? // check orientation
                    new RectInt(rng.NextInt(intersection.xMin + 1, intersection.xMax - 3), intersection.y, 2, 1) // door is horizontal
                    :
                    new RectInt(intersection.x, rng.NextInt(intersection.yMin + 1, intersection.yMax - 3), 1, 2); // door is vertical

                bool isDuplicate = doors.Any(door =>
                    (newDoor.room1 == door.room1 && newDoor.room2 == door.room2)
                    ||
                    (newDoor.room1 == door.room2 && newDoor.room2 == door.room1));

                if (!isDuplicate)
                {
                    doors.Add(newDoor);
                }

                if (doAnimation) yield return new WaitForSeconds(waitTime);
            }

            if (doAnimation) yield return new WaitForSeconds(waitTime);
        }
    }

    IEnumerator SpawnAssets()
    {
        // create parent
        assetParent = new GameObject("Dungeon");

        // Map floors and walls
        Dictionary<Vector2Int, Transform> floorMap = new();
        Dictionary<Vector2Int, Transform> wallMap = new();
        Dictionary<Vector2Int, Transform> decorationMap = new();
        int roomNumber = 0;
        foreach (RectInt room in graph.GetNodes())
        {
            // set up parents
            GameObject roomParent = new GameObject($"Room {roomNumber}");
            roomParent.transform.parent = assetParent.transform;

            GameObject floorParent = new GameObject("Floors");
            floorParent.transform.parent = roomParent.transform;

            GameObject wallParent = new GameObject("Walls");
            wallParent.transform.parent = roomParent.transform;

            foreach(Vector2Int position in room.allPositionsWithin)
            {
                if (!floorMap.ContainsKey(position)) floorMap.Add(position, floorParent.transform);

                if(position.x == room.x || position.x == room.xMax - 1 || position.y == room.y || position.y == room.yMax - 1)
                {
                    // position is on the edge of the room
                    if (!wallMap.ContainsKey(position)) wallMap.Add(position, wallParent.transform);
                }
            }

            roomNumber++;
        }

        // map decorations
        roomNumber = 0;
        foreach (RectInt room in graph.GetNodes())
        {
            GameObject roomParent = GameObject.Find($"Room {roomNumber}"); // find the roomparent created earlier

            GameObject decorationParent = new GameObject("Decorations");
            decorationParent.transform.parent = roomParent.transform;

            List<Vector2Int> possiblePositions = new();

            foreach (Vector2Int position in room.allPositionsWithin)
            {
                if (!(position.x == room.x || position.x == room.xMax - 1 || position.y == room.y || position.y == room.yMax - 1)) // wall positions, avoid spawning inside walls
                {
                    if (position.x == room.x + 1 || position.x == room.xMax - 2 || position.y == room.y + 1 || position.y == room.yMax - 2) // 1 away from walls
                    {
                        possiblePositions.Add(position);
                    }
                }
            }

            int decorationAmount = rng.NextInt(1, maxDecorationsPerRoom);

            for (int i = 0; i < decorationAmount; i++)
            {
                int randomIndex = rng.NextInt(0, possiblePositions.Count);

                decorationMap.Add(possiblePositions[randomIndex], decorationParent.transform);

                possiblePositions.RemoveAt(randomIndex);
            }

            roomNumber++;
        }

        // clear door positions
        foreach (Door door in doors)
        {
            foreach(Vector2Int position in door.rect.allPositionsWithin)
            {
                wallMap.Remove(position);

                // remove possible blocking decorations
                foreach(Vector2Int direction in orthogonalDirections)
                {
                    if (decorationMap.ContainsKey(position + direction)) decorationMap.Remove(position + direction);
                }
            }
        }

        // spawn floors
        Queue<Vector2Int> queue = new();
        HashSet<Vector2Int> discovered = new();

        Vector2Int startPosition = Vector2Int.RoundToInt(graph.GetNodes()[0].center);
        queue.Enqueue(startPosition);
        discovered.Add(startPosition);

        while (queue.Count > 0)
        {
            Vector2Int position = queue.Dequeue();

            Instantiate(floor, new Vector3(position.x + .5f, 0, position.y + .5f), Quaternion.identity, floorMap[position]);

            List<Vector2Int> neighbours = new();

            foreach (Vector2Int direction in orthogonalDirections)
            {
                Vector2Int newPos = position + direction;

                if (floorMap.ContainsKey(newPos)) neighbours.Add(newPos);
            }

            foreach (Vector2Int neighBouringPosition in neighbours)
            {
                if (!discovered.Contains(neighBouringPosition))
                {
                    queue.Enqueue(neighBouringPosition);
                    discovered.Add(neighBouringPosition);
                }
            }

            if (doAnimation) yield return new WaitForSeconds(waitTime);
        }

        // spawn walls
        foreach (var kvp in wallMap)
        {
            Vector2Int position = kvp.Key;

            // find Neighbooring positions
            bool up = wallMap.ContainsKey(new Vector2Int(position.x, position.y + 1));
            bool down = wallMap.ContainsKey(new Vector2Int(position.x, position.y - 1));
            bool right = wallMap.ContainsKey(new Vector2Int(position.x + 1, position.y));
            bool left = wallMap.ContainsKey(new Vector2Int(position.x - 1, position.y));

            GameObject prefabToSpawn = null;
            Vector3 rotation = default;

            // Cross wall
            if (up && down && right && left)
            {
                prefabToSpawn = crossWall;
            }

            // T junctions
            else if (up && right && left)
            {
                prefabToSpawn = tWall;
            }
            else if (down && right && left)
            {
                prefabToSpawn = tWall;
                rotation = new Vector3(0, 180, 0);
            }
            else if (right && up && down)
            {
                prefabToSpawn = tWall;
                rotation = new Vector3(0, 90, 0);
            }
            else if (left && up && down)
            {
                prefabToSpawn = tWall;
                rotation = new Vector3(0, -90, 0);
            }

            // straight walls
            else if (right && left)
            {
                prefabToSpawn = straightWall;
            }
            else if (up && down)
            {
                prefabToSpawn = straightWall;
                rotation = new Vector3(0, 90, 0);
            }

            // corner walls
            else if (up && right)
            {
                prefabToSpawn = cornerWall;
            }
            else if (down && right)
            {
                prefabToSpawn = cornerWall;
                rotation = new Vector3(0, 90, 0);
            }
            else if (up && left)
            {
                prefabToSpawn = cornerWall;
                rotation = new Vector3(0, -90, 0);
            }
            else if (down && left)
            {
                prefabToSpawn = cornerWall;
                rotation = new Vector3(0, 180, 0);
            }

            else if (up)
            {
                prefabToSpawn = endWall;
            }
            else if (down)
            {
                prefabToSpawn = endWall;
                rotation = new Vector3(0, 180, 0);
            }
            else if (right)
            {
                prefabToSpawn = endWall;
                rotation = new Vector3(0, 90, 0);
            }
            else if (left)
            {
                prefabToSpawn = endWall;
                rotation = new Vector3(0, -90, 0);
            }

            else prefabToSpawn = standaloneWall;

            Instantiate(prefabToSpawn, new Vector3(position.x + .5f, 0, position.y + .5f), Quaternion.Euler(rotation), kvp.Value);

            if (doAnimation) yield return new WaitForSeconds(waitTime);
        }

        // spawn decorations
        foreach (var kvp in decorationMap)
        {
            Vector2Int position = kvp.Key;
            Vector3 adjustedPosition = new Vector3(position.x + .5f, 0, position.y + .5f);
            Vector3 rotation = Vector3.up * rng.NextFloat(0, 360);

            GameObject randomDecoration = decorationObjects[rng.NextInt(0, decorationObjects.Length)];
            Instantiate(randomDecoration, adjustedPosition, Quaternion.Euler(rotation), kvp.Value);

            spawnedDecorationPositions.Add(adjustedPosition);

            if (doAnimation) yield return new WaitForSeconds(waitTime);
        }
    }

    void CreateNavigationGraph()
    {
        // map all detailed positions
        foreach(RectInt room in graph.GetNodes())
        {
            foreach(Vector2Int position in room.allPositionsWithin)
            {
                Vector3 adjustedPosition = new Vector3(position.x + .5f, 0, position.y + .5f);

                if (spawnedDecorationPositions.Contains(adjustedPosition)) continue;

                if (!(position.x == room.x || position.x == room.xMax - 1 || position.y == room.y || position.y == room.yMax - 1))
                {
                    navigationGraph.AddNode(new Vector3(position.x + .5f, 0, position.y + .5f));
                }
            }
        }

        Vector3[] positions = navigationGraph.GetNodes().ToArray();

        // connect all neighbours
        foreach (Vector3 position in positions)
        {
            foreach(Vector2Int direction in orthogonalDirections)
            {
                if (positions.Contains(position + new Vector3(direction.x, 0, direction.y)))
                {
                    navigationGraph.AddEdge(position, position + new Vector3(direction.x, 0, direction.y));
                }
            }
            
            //foreach(Vector2Int direction in diagonalDirections)
            //{
            //    if (positions.Contains(position + new Vector3(direction.x, 0, direction.y)))
            //    {
            //        navigationGraph.AddEdge(position, position + new Vector3(direction.x, 0, direction.y));
            //    }
            //}
        }

        foreach (Door door in doors)
        {
            // add door positions
            foreach (Vector2Int position in door.rect.allPositionsWithin)
            {
                Vector3 adjustedPosition = new Vector3(position.x + .5f, 0, position.y + .5f);

                navigationGraph.AddNode(adjustedPosition);
            }
        }

        foreach(Door door in doors)
        {
            // connect door positions to other nodes
            foreach (Vector2Int position in door.rect.allPositionsWithin)
            {
                Vector3 adjustedPosition = new Vector3(position.x + .5f, 0, position.y + .5f);

                foreach (Vector2Int direction in orthogonalDirections)
                {
                    if (positions.Contains(adjustedPosition + new Vector3(direction.x, 0, direction.y)))
                    {
                        navigationGraph.AddEdge(adjustedPosition, adjustedPosition + new Vector3(direction.x, 0, direction.y));
                    }
                }

                // don't include diagonal connections for doors in order to stay clear of walls
                //foreach (Vector2Int direction in diagonalDirections)
                //{
                //    if (positions.Contains(adjustedPosition + new Vector3(direction.x, 0, direction.y)))
                //    {
                //        navigationGraph.AddEdge(adjustedPosition, adjustedPosition + new Vector3(direction.x, 0, direction.y));
                //    }
                //}
            }
        }
    }

    [HorizontalLine]
    [Header("Debugging")]
    public Transform cursor;
    public bool showNavigationGraph;
    public bool showRooms;
    private void Update()
    {
        if (showRooms)
        {
            DrawRooms();
            if (!removalFinished || removedCyclicPaths)
                DrawGraph(graph, Time.deltaTime);
            DrawDoors(); 
        }

        if (Input.GetMouseButtonDown(1))
        {
            Debug.Log(FindRoomAtPosition(cursor.position));
        }

        if (showNavigationGraph)
        {
            foreach (Vector3 node in navigationGraph.GetNodes())
            {
                DebugExtension.DebugCircle(node, Vector3.up, Color.magenta, .2f);
                foreach (Vector3 connection in navigationGraph.GetNeighbors(node))
                {
                    Debug.DrawLine(node, connection, Color.magenta);
                }
            } 
        }
    }
    void DrawRooms()
    {
        if (!graphFinished)
            foreach (RectInt room in generatedRooms) { AlgorithmsUtils.DebugRectInt(room, Color.green, Time.deltaTime); }
    }
    void DrawGraph(Graph<RectInt> graph, float time)
    {
        foreach(RectInt node in graph.GetNodes()) 
        {
            AlgorithmsUtils.DebugRectInt(node, Color.green, time);
            DebugExtension.DebugCircle(GetMiddle(node), Color.magenta, Mathf.Min(node.width, node.height) / 4, time); 
            foreach(RectInt connection in graph.GetNeighbors(node))
            {
                Debug.DrawLine(GetMiddle(node), GetMiddle(connection), Color.magenta, time);
            }
        }
    }
    void DrawDoors()
    {
        foreach (Door door in doors)
        {
            AlgorithmsUtils.DebugRectInt(door.rect, Color.cyan, Time.deltaTime, false, .1f);
        }
    }

    [Button] void Redraw()
    {
        StopAllCoroutines();
        roomsToCheck.Clear();
        generatedRooms.Clear();
        generationFinished = false;
        graph.Clear();
        graphFinished = false;
        removalFinished = false;
        removedCyclicPaths = false;
        doors.Clear();
        Destroy(assetParent);
        spawnedDecorationPositions.Clear();
        navigationGraph.Clear();
        StartCoroutine(Start());
    }
    [Button] void CheckGraphBFS()
    {
        graph.BFS(graph.GetNodes()[0], true);
    }
    [Button] void CheckGraphDFS()
    {
        graph.DFS(graph.GetNodes()[0], true);
    }

    // helper functions
    Vector3 GetMiddle(RectInt rect)
    {
        return new Vector3((float)rect.x + (float)rect.width / 2f, 0f, (float)rect.y + (float)rect.height / 2f);
    }
    RectInt FindRoomAtPosition(Vector3 position)
    {
        foreach (RectInt room in graph.GetNodes())
        {
            if (room.x + room.width < position.x) continue;
            if (room.y + room.height < position.z) continue;
            if (room.x > position.x) continue;
            if (room.y > position.z) continue;

            return room;
        }
        return default;
    }
}

[System.Serializable] public struct Door
{
    public RectInt rect;
    public RectInt room1;
    public RectInt room2;
}
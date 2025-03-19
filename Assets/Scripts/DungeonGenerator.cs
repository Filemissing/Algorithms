using System.Collections.Generic;
using UnityEngine;
using Unity.Mathematics;
using NaughtyAttributes;
using Random = Unity.Mathematics.Random;
using System.Collections;
using System;
using System.Linq;
using UnityEngine.UIElements;

public class DungeonGenerator : MonoBehaviour
{
    [Header("General")]
    public int seed;
    Random rng;

    public Vector2Int size = new Vector2Int(100, 50);
    public Vector2Int maxRoomSize = new Vector2Int(10, 10);

    [Header("Generation")]
    public List<RectInt> generatedRooms = new List<RectInt>();
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

    [HorizontalLine]
    [Header("Animation")]
    public bool doAnimation;
    public float waitTime;

    IEnumerator Start()
    {
        Initilize();

        yield return StartCoroutine(CreateRooms());

        yield return StartCoroutine(GenerateGraph());

        yield return StartCoroutine(RemoveRooms());

        yield return StartCoroutine(RemoveCyclicPaths());

        yield return StartCoroutine(SpawnDoors());
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

        float splitRatio = 2 + (rng.NextFloat() - .5f) / 2;

        RectInt newRoom1 = new RectInt(room.x, room.y, Mathf.RoundToInt((float)(room.width / splitRatio)), room.height);
        RectInt newRoom2 = new RectInt(room.x + newRoom1.width - 1, room.y, room.width - newRoom1.width + 1, room.height);

        newRooms[0] = newRoom1;
        newRooms[1] = newRoom2;

        return newRooms;
    }
    RectInt[] SplitHorizontally(RectInt room)
    {
        RectInt[] newRooms = new RectInt[2];

        float splitRatio = 2 + (rng.NextFloat() - .5f) / 2;

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
                AlgorithmsUtils.DebugRectInt(room2, Color.cyan, waitTime, false, 1);

                if (doAnimation) yield return new WaitForSeconds(waitTime);
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

    [HorizontalLine]
    [Header("Debugging")]
    public Transform cursor;
    private void Update()
    {
        DrawRooms();
        if(!removalFinished || removedCyclicPaths)
            DrawGraph(graph, Time.deltaTime);
        DrawDoors();

        if (Input.GetMouseButtonDown(0))
        {
            Debug.Log(FindRoomAtPosition(cursor.position));
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
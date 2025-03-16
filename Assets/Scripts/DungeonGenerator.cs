using System.Collections.Generic;
using UnityEngine;
using Unity.Mathematics;
using NaughtyAttributes;
using Random = Unity.Mathematics.Random;
using System.Collections;
using System;
using System.Linq;
using UnityEditor.Experimental.GraphView;
using UnityEngine.Analytics;
using Unity.VisualScripting;

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

    [Header("Doors")]
    public int doorArea;
    public List<Door> doors = new();


    [HorizontalLine]
    [Header("Animation")]
    public bool doAnimation;
    public float waitTime;
    public float waitAfterGeneration;

    [HorizontalLine]
    [Header("Graph")]
    public Graph<RectInt> graph = new();
    bool graphFinished = false;

    IEnumerator Start()
    {
        Initilize();

        StartCoroutine(CreateRooms());

        yield return new WaitUntil(() => generationFinished);

        StartCoroutine(GenerateGraph());

        yield return new WaitUntil(() => graphFinished);

        StartCoroutine(RemoveRooms());
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

        if (doAnimation) yield return new WaitForSeconds(waitAfterGeneration);

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

                AlgorithmsUtils.DebugRectInt(room1, Color.cyan, waitTime);
                AlgorithmsUtils.DebugRectInt(room2, Color.cyan, waitTime);

                if (doAnimation) yield return new WaitForSeconds(waitTime);
            }
            if (doAnimation) yield return new WaitForSeconds(waitTime);
        }

        graphFinished = true;
    }

    IEnumerator RemoveRooms()
    {
        List<RectInt> roomsToRemove = new(graph.GetNodes());
        roomsToRemove = roomsToRemove.OrderBy(x => x.width * x.height).Take(Mathf.RoundToInt(generatedRooms.Count * 0.1f)).ToList(); // get the 10% smallest rooms

        for (int i = 0; i < roomsToRemove.Count - 1; i++)
        {
            RectInt roomToRemove = roomsToRemove[i];

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
                break;
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
        DrawGraph();

        if (Input.GetMouseButtonDown(0))
        {
            Debug.Log(FindRoomAtPosition(cursor.position));
        }
    }
    void DrawRooms()
    {
        if (!generationFinished)
            foreach (RectInt room in generatedRooms) { AlgorithmsUtils.DebugRectInt(room, Color.green, Time.deltaTime, false, 0.1f); }
    }
    void DrawGraph()
    {
        foreach(RectInt node in graph.GetNodes()) 
        {
            AlgorithmsUtils.DebugRectInt(node, Color.green);
            DebugExtension.DebugCircle(GetMiddle(node), Color.magenta, Mathf.Min(node.width, node.height) / 4); 
            foreach(RectInt connection in graph.GetNeighbors(node))
            {
                Debug.DrawLine(GetMiddle(node), GetMiddle(connection), Color.magenta);
            }
        }
    }

    // helper functions
    Vector3 GetMiddle(RectInt rect)
    {
        return new Vector3(rect.x + rect.width / 2, 0, rect.y + rect.height / 2);
    }

    [Button] void Redraw()
    {
        StopAllCoroutines();
        roomsToCheck.Clear();
        generatedRooms.Clear();
        generationFinished = false;
        graph.Clear();
        graphFinished = false;
        StartCoroutine(Start());
    }
    [Button] void CheckGraphBFS()
    {
        graph.BFS(graph.GetNodes()[0]);
    }
    [Button] void CheckGraphDFS()
    {
        graph.DFS(graph.GetNodes()[0]);
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
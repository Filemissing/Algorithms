using System.Collections.Generic;
using UnityEngine;
using Unity.Mathematics;
using NaughtyAttributes;
using Random = Unity.Mathematics.Random;
using System.Collections;
using System;
using System.Linq;
using UnityEditor.Experimental.GraphView;

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

    [Header("Path")]
    public float removePercentage = 75;
    public float DoorSize;

    Dictionary<RectInt, RectInt[]> adjacentRooms = new Dictionary<RectInt, RectInt[]>();
    public RectInt[] path;
    public List<Door> doors = new List<Door>();
    public List<RectInt> finalRooms = new List<RectInt>();

    [HorizontalLine]
    [Header("Animation")]
    public float waitTime;
    public float waitAfterGeneration;

    List<RectInt> roomsToCheck = new List<RectInt>();

    IEnumerator Start()
    {
        Initilize();

        StartCoroutine(CreateRooms());

        yield return new WaitUntil(() => generationFinished);

        GetAdjacentRooms();

        StartCoroutine(GeneratePath());
    }

    void Initilize()
    {
        rng = new Random(Convert.ToUInt32(seed));

        RectInt startRoom = new RectInt(0, 0, size.x, size.y);
        roomsToCheck.Add(startRoom);
    }

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

                yield return new WaitForSeconds(waitTime);
            }
            roomsToCheck = newRooms;
        }

        yield return new WaitForSeconds(waitAfterGeneration);

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

    void GetAdjacentRooms()
    {
        foreach (RectInt room1 in generatedRooms)
        {
            List<RectInt> _adjacentRooms = new List<RectInt>();

            foreach (RectInt room2 in generatedRooms)
            {
                RectInt intersection = AlgorithmsUtils.Intersect(room1, room2);

                if (intersection.width * intersection.height >= DoorSize)
                {
                    _adjacentRooms.Add(room2);
                }
            }

            adjacentRooms.Add(room1, _adjacentRooms.ToArray());
        }
    }
    IEnumerator GeneratePath()
    {
        RectInt startingRoom = generatedRooms[rng.NextInt(0, generatedRooms.Count - 1)];

        path = new RectInt[Mathf.RoundToInt(generatedRooms.Count * (1 - removePercentage / 100))];

        path[0] = startingRoom;

        for (int i = 1; i < path.Length; i++)
        {
            int neighboorRetries = 0;
            int keyRetries = 0;

            RectInt key = path[i - 1];
            int neighbourCount = adjacentRooms[key].Length - 1;
            int index = rng.NextInt(0, neighbourCount);

        retry:
            //Debug.Log("neignbors = " + adjacentRooms[key].Length + " index: " + index);
            if (!path.Contains(adjacentRooms[key][index]))
            {
                RectInt startRoom = path[i - 1];
                RectInt newRoom = adjacentRooms[key][index];
                path[i] = newRoom;

                Door newDoor = new Door();
                newDoor.room1 = startRoom;
                newDoor.room2 = newRoom;
                RectInt intersection = AlgorithmsUtils.Intersect(startRoom, newRoom);

                Debug.Log($"startRoom: {startRoom}, newRoom: {newRoom} | intersection {intersection}");

                if (intersection.height == 1)
                {
                    newDoor.rect = new RectInt(intersection.x + Mathf.RoundToInt(intersection.width / 2) - 1, intersection.y, 2, 1);
                }
                else
                {
                    newDoor.rect = new RectInt(intersection.x, intersection.y + Mathf.RoundToInt(intersection.height / 2) - 1, 1, 2);
                }

                doors.Add(newDoor);
            }
            else if (neighboorRetries < neighbourCount)
            {
                // retry with a different neighboor
                neighboorRetries++;

                //Debug.Log($"oldIndex {index}, neighboorCount {neighbourCount}");

                index = (index + 1) % neighbourCount;
                goto retry;
            }
            else if(keyRetries < i - 1)// dead end - go back to a previous room
            {
                keyRetries++;
                neighboorRetries = 0;

                //Debug.Log($"i: {i}, keyRetries: {keyRetries} | index: {i - 1 - keyRetries}");

                key = path[i - 1 - keyRetries];
                neighbourCount = adjacentRooms[key].Length - 1;
                index = rng.NextInt(0, neighbourCount);
                goto retry;
            }
            else // no more possible rooms - probably will never trigger
            {
                //Debug.LogWarning($"returned - i: {i}, neighboorRetries: {neighboorRetries}, keyRetries: {keyRetries}, index: {index}");
                yield break;
            }

            AlgorithmsUtils.DebugRectInt(key, Color.magenta, waitTime, false, .5f);

            yield return new WaitForSeconds(waitTime);
        }
    }

    bool MapIsValid(List<RectInt> remainingRooms)
    {
        // TO DO: Increase efficiency

        List<RectInt> roomsToCheck = new List<RectInt>(remainingRooms);
        HashSet<RectInt> connectedRooms = new HashSet<RectInt>() { remainingRooms[0] };

    point:
        foreach (RectInt room in roomsToCheck)
        {
            foreach (RectInt room2 in connectedRooms)
            {
                if (room.Equals(room2)) continue;

                // if room1 intersects with any room already in connectedRooms add it to connectedrooms
                if (AlgorithmsUtils.Intersects(room, room2))
                {
                    connectedRooms.Add(room);
                    roomsToCheck.Remove(room);

                    goto point;
                }
            }
        }

        // if connectedRooms contains all remainingRooms the map is valid
        return connectedRooms.Count == remainingRooms.Count;
    }

    private void Update()
    {
        DrawRooms();
    }
    void DrawRooms()
    {
        if (!generationFinished)
            foreach (RectInt room in generatedRooms) { AlgorithmsUtils.DebugRectInt(room, Color.green, Time.deltaTime, false, 0.1f); }
        else
        {
            foreach (RectInt room in path) { AlgorithmsUtils.DebugRectInt(room, Color.green, Time.deltaTime, false, 0.1f); }
            foreach (Door door in doors) { AlgorithmsUtils.DebugRectInt(door.rect, Color.cyan, Time.deltaTime, false, 0.1f); }
        }
            
    }
    [Button]
    void Redraw()
    {
        StopAllCoroutines();
        roomsToCheck.Clear();
        generatedRooms.Clear();
        generationFinished = false;
        finalRooms.Clear();
        adjacentRooms.Clear();
        path = new RectInt[0];
        doors.Clear();
        StartCoroutine(Start());
    }
}

[System.Serializable] public struct Door
{
    public RectInt rect;
    public RectInt room1;
    public RectInt room2;
}
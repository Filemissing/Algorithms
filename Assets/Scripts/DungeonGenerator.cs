using System.Collections.Generic;
using UnityEngine;
using Unity.Mathematics;
using NaughtyAttributes;
using Random = Unity.Mathematics.Random;
using System.Collections;
using System;

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

    [Header("Removal")]
    public float removePercentage = 75;
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

        RemoveRooms();
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

    void RemoveRooms()
    {
        List<RectInt> remainingRooms = new List<RectInt>(generatedRooms);
        List<RectInt> roomsToCheck = new List<RectInt>(generatedRooms);
        RectInt lastRemovedRoom = default;

    again:
        while (MapIsValid(remainingRooms) && remainingRooms.Count > generatedRooms.Count * (1 - (removePercentage / 100)) && roomsToCheck.Count > 0)
        {
            int index = Mathf.RoundToInt(rng.NextFloat() * (roomsToCheck.Count - 1));

            Debug.Log(index);

            lastRemovedRoom = roomsToCheck[index];

            remainingRooms.Remove(roomsToCheck[index]);
            roomsToCheck.RemoveAt(index);
        }

        if (!MapIsValid(remainingRooms))
        {
            remainingRooms.Add(lastRemovedRoom);
            goto again;
        }

        finalRooms = remainingRooms;
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
            foreach(RectInt room in finalRooms) { AlgorithmsUtils.DebugRectInt(room, Color.green, Time.deltaTime, false, 0.1f); }
    }
    [Button] void Redraw()
    {
        StopAllCoroutines();
        roomsToCheck.Clear();
        generatedRooms.Clear();
        generationFinished = false;
        finalRooms.Clear();
        StartCoroutine(Start());
    }
}
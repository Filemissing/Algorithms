using System.Collections.Generic;
using UnityEngine;
using NaughtyAttributes;

public class DungeonGenerator : MonoBehaviour
{
    public int seed;

    public Vector2Int size = new Vector2Int(100, 50);
    public Vector2Int maxRoomSize = new Vector2Int(10, 10);
    List<RectInt> roomsToCheck = new List<RectInt>();
    public int splitAmount = 3;

    private void Start()
    {
        Random.InitState(seed);

        RectInt startRoom = new RectInt(0, 0, size.x, size.y);
        roomsToCheck.Add(startRoom);

        for (int i = 0; i < splitAmount; i++)
        {
            List<RectInt> newRooms = new List<RectInt>();
            foreach (RectInt room in roomsToCheck)
            {
                bool splitHorizontal = Mathf.Round(Random.value) == 1;
                if (splitHorizontal) newRooms.AddRange(SplitHorizontally(room));
                else newRooms.AddRange(SplitVertically(room));

                //splitHorizontally = !splitHorizontally;
            }
            roomsToCheck = newRooms;
        }
    }

    private void Update()
    {
        DrawRooms();
    }

    RectInt[] SplitVertically(RectInt room)
    {
        RectInt[] newRooms = new RectInt[2];

        RectInt newRoom1 = new RectInt(room.x, room.y, room.width / 2, room.height);
        RectInt newRoom2 = new RectInt(room.x + room.width / 2 - 1, room.y, room.width - newRoom1.width + 1, room.height);

        newRooms[0] = newRoom1;
        newRooms[1] = newRoom2;

        return newRooms;
    }

    RectInt[] SplitHorizontally(RectInt room)
    {
        RectInt[] newRooms = new RectInt[2];

        RectInt newRoom1 = new RectInt(room.x, room.y, room.width, room.height / 2);
        RectInt newRoom2 = new RectInt(room.x, room.y + room.height / 2 - 1, room.width, room.height - newRoom1.height + 1);

        newRooms[0] = newRoom1;
        newRooms[1] = newRoom2;

        return newRooms;
    }

    void DrawRooms()
    {
        foreach (RectInt room in roomsToCheck)
        {
            AlgorithmsUtils.DebugRectInt(room, Color.yellow, Time.deltaTime);
        }
    }

    [Button]
    void Redraw()
    {
        roomsToCheck.Clear();
        Start();
    }
}

using System.Collections.Generic;
using UnityEngine;
using NaughtyAttributes;
using UnityEngine.SearchService;

public class DungeonGenerator : MonoBehaviour
{
    public int seed;

    public Vector2Int size = new Vector2Int(100, 50);
    public Vector2Int maxRoomSize = new Vector2Int(10, 10);
    public List<RectInt> finalRooms = new List<RectInt>();

    List<RectInt> roomsToCheck = new List<RectInt>();
    
    private void Start()
    {
        Initilize();

        CreateRooms();
    }

    void Initilize()
    {
        Random.InitState(seed);

        RectInt startRoom = new RectInt(0, 0, size.x, size.y);
        roomsToCheck.Add(startRoom);
    }

    void CreateRooms()
    {
        while (roomsToCheck.Count > 0)
        {
            List<RectInt> newRooms = new List<RectInt>();
            List<RectInt> finishedRooms = new List<RectInt>();
            foreach (RectInt room in roomsToCheck)
            {
                if (room.width > maxRoomSize.x && room.height > maxRoomSize.y) // both dimensions are too big - choose a random one
                {
                    bool splitHorizontal = Mathf.Round(Random.value) == 1;
                    if (splitHorizontal) newRooms.AddRange(SplitHorizontally(room));
                    else newRooms.AddRange(SplitVertically(room));
                }
                else if (room.width > maxRoomSize.x) // width is too big
                {
                    newRooms.AddRange(SplitVertically(room));
                }
                else if (room.height > maxRoomSize.y) // height is too big
                {
                    newRooms.AddRange(SplitHorizontally(room));
                }
                else // room is finished - move to finalRooms
                {
                    finalRooms.Add(room);
                }
            }
            roomsToCheck = newRooms;
        }
    }

    RectInt[] SplitVertically(RectInt room)
    {
        RectInt[] newRooms = new RectInt[2];

        double splitRatio = 2 + (Random.value - .5f) / 2;

        RectInt newRoom1 = new RectInt(room.x, room.y, Mathf.RoundToInt((float)(room.width / splitRatio)), room.height);
        RectInt newRoom2 = new RectInt(room.x + newRoom1.width - 1, room.y, room.width - newRoom1.width + 1, room.height);

        newRooms[0] = newRoom1;
        newRooms[1] = newRoom2;

        return newRooms;
    }

    RectInt[] SplitHorizontally(RectInt room)
    {
        RectInt[] newRooms = new RectInt[2];

        double splitRatio = 2 + (Random.value - .5f) / 2;

        RectInt newRoom1 = new RectInt(room.x, room.y, room.width, Mathf.RoundToInt((float)(room.height / splitRatio)));
        RectInt newRoom2 = new RectInt(room.x, room.y + newRoom1.height - 1, room.width, room.height - newRoom1.height + 1);

        newRooms[0] = newRoom1;
        newRooms[1] = newRoom2;

        return newRooms;
    }

    private void Update()
    {
        DrawRooms();
    }

    void DrawRooms()
    {
        foreach (RectInt room in finalRooms)
        {
            AlgorithmsUtils.DebugRectInt(room, Color.yellow, Time.deltaTime);
        }
    }

    [Button]
    void Redraw()
    {
        finalRooms.Clear();
        Start();
    }
}

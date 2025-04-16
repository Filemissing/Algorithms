using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.AI;

[RequireComponent(typeof(Rigidbody))]
public class PlayerController : MonoBehaviour
{
    public float speed;
    public LayerMask walkableMask;
    public float proximityMargin;
    new bool enabled = false;

    Vector3 targetPosition;
    Vector3[] currentPath;
    int currentStep;
    Rigidbody rb;
    public NavMeshAgent agent;
    public void Enable()
    {
        rb = GetComponent<Rigidbody>();
        Vector2 pos = DungeonGenerator.instance.graph.GetNodes()[0].center;
        transform.position = new Vector3(pos.x, 1, pos.y);
        targetPosition = transform.position;
        rb.isKinematic = false;
        enabled = true;
        agent = gameObject.AddComponent<NavMeshAgent>();
    }

    public void Disable()
    {
        transform.position = Vector3.zero;
        targetPosition = Vector3.zero;
        currentPath = null;
        currentStep = 0;
        rb.isKinematic = true;
        enabled = false;
        Destroy(agent);
    }

    void Update()
    {
        if (!enabled) return;

        rb.isKinematic = !DungeonGenerator.instance.useNavMesh;

        if (Input.GetMouseButtonDown(0))
        {
            if(Physics.Raycast(Camera.main.ScreenPointToRay(Input.mousePosition), out RaycastHit hit, Mathf.Infinity, walkableMask))
            {
                Debug.Log($"hit {hit.point}");
                targetPosition = hit.point;

                if (DungeonGenerator.instance.useNavMesh)
                {
                    agent.SetDestination(targetPosition);
                }
                else
                {
                    if (transform.position != targetPosition)
                    {
                        Vector3[] path = FindShortestPath(targetPosition);
                        currentPath = path;
                        currentStep = 0;
                    }
                }
            }
        }
    }

    private void FixedUpdate()
    {
        if (currentPath == null || currentStep >= currentPath.Length) return;

        Vector3 target = currentPath[currentStep] + Vector3.up * transform.position.y; // keep y position
        Vector3 moveDir = (target - transform.position).normalized;

        if (Vector3.Distance(transform.position, target) < proximityMargin)
        {
            currentStep++;
        }
        else
        {
            rb.MovePosition(transform.position + moveDir * speed);
        }

        if (currentStep < currentPath.Length - 1)
        {
            DebugExtension.DebugArrow(transform.position, currentPath[currentStep] - transform.position, Color.red);
            for (int i = currentStep; i < currentPath.Length - 2; i++)
            {
                DebugExtension.DebugArrow(currentPath[i], currentPath[i + 1] - currentPath[i], Color.red);
            } 
        }
    }

    Vector3[] FindShortestPath(Vector3 target)
    {
        Vector3 adjustedTarget = Vector3.ProjectOnPlane(Vector3Int.RoundToInt(target), Vector3.up) + new Vector3(.5f, 0, .5f); // adjust position for grid

        // key - position, values - distance from start(g), Heuristic distance(h), f = g + h, previous position
        Dictionary<Vector3, (int, int, int, Vector3)> storedData = new();

        List<Vector3> open = new();
        List<Vector3> closed = new();

        Vector3 startPos = Vector3.ProjectOnPlane(Vector3Int.RoundToInt(transform.position), Vector3.up) + new Vector3(.5f, 0, .5f);

        Vector3 current = startPos;

        int g = 0;
        int h = Mathf.RoundToInt(Mathf.Abs((target.x - current.x)) + Mathf.Abs((target.z - current.z))); // estimate manhattan distance
        int f = g + h;

        storedData.Add(current, (g, h, f, default));

        while (current != adjustedTarget)
        {
            foreach(Vector3 pos in DungeonGenerator.instance.navigationGraph.GetNeighbors(current))
            {
                if(!open.Contains(pos) && !closed.Contains(pos))
                {
                    open.Add(pos);
                    g = storedData[current].Item1 + Mathf.RoundToInt(Mathf.Abs(pos.x - current.x) + Mathf.Abs(pos.z - current.z));
                    h = Mathf.RoundToInt(Mathf.Abs(adjustedTarget.x - pos.x) + Mathf.Abs(adjustedTarget.z - pos.z));
                    f = g + h;

                    if (storedData.ContainsKey(pos) && storedData[pos].Item3 > f)
                    {
                        (int, int, int, Vector3) oldData = storedData[pos];
                        storedData[pos] = new(oldData.Item1, oldData.Item2, f, current);
                    }
                    else if (!storedData.ContainsKey(pos))
                    {
                        storedData.Add(pos, (g, h, f, current));
                    }
                }
            }
            closed.Add(current);

            if (open.Count == 0)
            {
                Debug.LogWarning($"No path found for Target: {adjustedTarget}");
                return Array.Empty<Vector3>();
            }

            // get the node with lowest f value
            Vector3 best = open[0];
            int bestF = storedData[best].Item3;
            foreach (var v in open)
            {
                int F = storedData[v].Item3;
                if (F < bestF)
                {
                    best = v;
                    bestF = F;
                }
            }
            open.Remove(best);
            current = best;
        }

        List<Vector3> path = new();
        Vector3 backTrackCurrent = adjustedTarget;
        while (storedData[backTrackCurrent].Item4 != default)
        {
            path.Add(backTrackCurrent);
            backTrackCurrent = storedData[backTrackCurrent].Item4;
        }

        path.Reverse();

        path.Add(target); // add the final precise position

        return path.ToArray();
    }
}

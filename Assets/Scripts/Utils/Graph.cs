using System.Collections.Generic;
using Unity.VisualScripting;
using UnityEngine;

public class Graph<T>
{
    private Dictionary<T, List<T>> adjacencyList;

    public Graph()
    {
        adjacencyList = new Dictionary<T, List<T>>();
    }
    
    public void Clear() 
    { 
        adjacencyList.Clear(); 
    }
    
    public void RemoveNode(T node)
    {
        if (adjacencyList.ContainsKey(node))
        {
            adjacencyList.Remove(node);
        }

        foreach (var key in adjacencyList.Keys)
        {
            adjacencyList[key].Remove(node);
        }
    }
    
    public List<T> GetNodes()
    {
        return new List<T>(adjacencyList.Keys);
    }
    
    public void AddNode(T node)
    {
        if (!adjacencyList.ContainsKey(node))
        {
            adjacencyList[node] = new List<T>();
        }
    }

    public void RemoveEdge(T fromNode, T toNode)
    {
        if (adjacencyList.ContainsKey(fromNode))
        {
            adjacencyList[fromNode].Remove(toNode);
        }
        if (adjacencyList.ContainsKey(toNode))
        {
            adjacencyList[toNode].Remove(fromNode);
        }
    }

    //<summary> "Adds a BiDirectional edge to the graph" </summary>
    public void AddEdge(T fromNode, T toNode) { 
        if (!adjacencyList.ContainsKey(fromNode))
        {
            AddNode(fromNode);
        }
        if (!adjacencyList.ContainsKey(toNode)) { 
            AddNode(toNode);
        }

        // Prevent duplicate edges
        if (!adjacencyList[fromNode].Contains(toNode))
        {
            adjacencyList[fromNode].Add(toNode);
        }
        if (!adjacencyList[toNode].Contains(fromNode))
        {
            adjacencyList[toNode].Add(fromNode);
        }
    } 
    
    public List<T> GetNeighbors(T node) 
    { 
        return new List<T>(adjacencyList[node]); 
    }

    public int GetNodeCount()
    {
        return adjacencyList.Count;
    }
    
    public void PrintGraph()
    {
        foreach (var node in adjacencyList)
        {
            Debug.Log($"{node.Key}: {string.Join(", ", node.Value)}");
        }
    }
    
    // Breadth-First Search (BFS)
    public bool BFS(T startNode)
    {
        Queue<T> queue = new();

        HashSet<T> discovered = new();

        queue.Enqueue(startNode);

        while(queue.Count > 0)
        {
            T node = queue.Dequeue();

            foreach(T connectedNode in adjacencyList[node])
            {
                if (!discovered.Contains(connectedNode))
                {
                    queue.Enqueue(connectedNode);
                    discovered.Add(connectedNode);
                }
            }
        }

        if (discovered.Count == GetNodeCount())
        {
            Debug.Log("Graph is fully connected");
            return true;
        }
        else
        {
            Debug.Log($"Graph is not fully connected | Connected Rooms: {discovered.Count}, Graph Size: {GetNodeCount()}");
            return false;
        }
    }

    // Depth-First Search (DFS)
    public bool DFS(T startNode)
    {
        Stack<T> stack = new();

        HashSet<T> discovered = new();

        stack.Push(startNode);

        while (stack.Count > 0)
        {
            T node = stack.Pop();

            foreach (T connectedNode in adjacencyList[node])
            {
                if (!discovered.Contains(connectedNode))
                {
                    stack.Push(connectedNode);
                    discovered.Add(connectedNode);
                }
            }
        }

        if (discovered.Count == GetNodeCount())
        {
            Debug.Log("Graph is fully connected");
            return true;
        }
        else
        {
            Debug.Log($"Graph is not fully connected | Connected Rooms: {discovered.Count}, Graph Size: {GetNodeCount()}");
            return false;
        }
    }
}
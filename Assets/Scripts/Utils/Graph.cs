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

    public void AddEdge(T fromNode, T toNode) { 
        if (!adjacencyList.ContainsKey(fromNode))
        {
            AddNode(fromNode);
        }
        if (!adjacencyList.ContainsKey(toNode)) { 
            AddNode(toNode);
        } 
        
        adjacencyList[fromNode].Add(toNode); 
        adjacencyList[toNode].Add(fromNode); 
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
    public void BFS(T startNode)
    {
        Queue<T> queue = new();

        HashSet<T> visited = new();

        queue.Enqueue(startNode);

        while(queue.Count > 0)
        {
            T node = queue.Dequeue();

            visited.Add(node);

            foreach(T connectedNode in adjacencyList[node])
            {
                if (!visited.Contains(connectedNode))
                {
                    queue.Enqueue(connectedNode); 
                }
            }
        }

        if (visited.Count == GetNodeCount())
        {
            Debug.Log("Graph is fully connected");
        }
        else
        {
            Debug.LogWarning($"Graph is not fully connected | Connected Rooms: {visited.Count}, Graph Size: {GetNodeCount()}");
        }
    }

    // Depth-First Search (DFS)
    public void DFS(T startNode)
    {
        Stack<T> queue = new();

        HashSet<T> visited = new();

        queue.Push(startNode);

        while (queue.Count > 0)
        {
            T node = queue.Pop();

            visited.Add(node);

            foreach (T connectedNode in adjacencyList[node])
            {
                if (!visited.Contains(connectedNode))
                {
                    queue.Push(connectedNode);
                }
            }
        }

        if (visited.Count == GetNodeCount())
        {
            Debug.Log("Graph is fully connected");
        }
        else
        {
            Debug.LogWarning($"Graph is not fully connected | Connected Rooms: {visited.Count}, Graph Size: {GetNodeCount()}");
        }
    }
}
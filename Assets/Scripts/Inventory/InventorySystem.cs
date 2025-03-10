using System.Collections.Generic;
using System.Text;
using UnityEngine;

public class InventorySystem : MonoBehaviour
{
    public static Dictionary<string, int> inventory = new Dictionary<string, int>();

    public void AddItem(string name) => AddItem(name, 1);
    public void AddItem(string name, int count)
    {
        if(count <= 0)
        {
            Debug.LogWarning($"Cannot add non-Positive amount {count} of {name} to inventory");
            return;
        }

        if (inventory.ContainsKey(name))
        {
            inventory[name] += count;
        }
        else
        {
            inventory.Add(name, count);
        }
        Debug.Log($"Added {count} {name}'s to inventory");
    }

    public void RemoveItem(string name) => RemoveItem(name, 1);
    public void RemoveItem(string name, int count)
    {
        if (inventory.ContainsKey(name))
        {
            inventory[name] -= count;

            if (inventory[name] <= 0)
            {
                inventory.Remove(name);
                Debug.Log($"removed last of item {name}");
            }
            else
            {
                Debug.Log($"removed {count} {name}'s, {inventory[name]} remaining");
            }
        }
        else
        {
            Debug.LogWarning($"Item \"{name}\" does not exist");
        }
    }

    public void PrintInventory()
    {
        StringBuilder builder = new StringBuilder();
        foreach(string name in inventory.Keys)
        {
            builder.AppendLine($"Item: {name} | Count: {inventory[name]}");
        }

        Debug.Log(builder.ToString());
    }
}
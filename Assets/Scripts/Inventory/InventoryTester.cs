using System;
using UnityEngine;

public class InventoryTester : MonoBehaviour
{
    InventorySystem inventorySystem;
    private void Awake()
    {
        inventorySystem = GetComponent<InventorySystem>();
    }

    void Start()
    {
        inventorySystem.AddItem("Health Potion", 2);
        inventorySystem.AddItem("Rusty Key", 3);
        inventorySystem.AddItem("Death Itself");
        inventorySystem.AddItem("The Cube");

        inventorySystem.RemoveItem("Health Potion");
        inventorySystem.RemoveItem("Health Potion", 5);

        inventorySystem.RemoveItem("God's Leg");

        inventorySystem.AddItem("Atoms", int.MaxValue);

        inventorySystem.PrintInventory();
    }
}

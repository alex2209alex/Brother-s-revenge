using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

[Serializable]

public class InventoryItem
{
    public ItemData itemData;
    public GameObject itemObject;
    public int stackSize;

    public InventoryItem(ItemData item)
    {
        itemData = item;
        AddToStack();
    }

    public void AddToStack()
    {
        stackSize++;
    }

    public void RemoveFromStack()
    {
        stackSize--;
    }
}

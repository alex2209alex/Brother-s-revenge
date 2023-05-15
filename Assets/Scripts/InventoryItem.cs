using System;
using System.Collections;
using System.Collections.Generic;
using DefaultNamespace;
using UnityEngine;

[Serializable]

public class InventoryItem
{
    public ItemData itemData;
    public ItemSlot itemSlot;
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

    public virtual void UseItem(ItemSlot itemSlot)
    {
        stackSize--;
        if (stackSize <= 0) itemSlot.RemoveItem();
        itemSlot.ItemStackSize.text = stackSize.ToString();
        itemData.UseItem();
    }
}

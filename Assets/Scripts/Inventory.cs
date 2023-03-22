using System;
using System.Collections;
using System.Collections.Generic;
using DefaultNamespace;
using TMPro;
using Unity.VisualScripting;
using UnityEngine;
using UnityEngine.UI;

public class Inventory : MonoBehaviour
{
    private Dictionary<ItemData, InventoryItem> itemDictionary = new Dictionary<ItemData, InventoryItem>();

    public Transform itemContent;
    public ItemSlot itemSlotPrefab;

    public Toggle enableRemove;
    
    private void OnEnable() => Collectible.OnItemCollected += Add;
    private void OnDisable() => Collectible.OnItemCollected -= Add;

    public void Add(ItemData itemData)
    {
        if (itemDictionary.TryGetValue(itemData, out InventoryItem item))
        {
            item.AddToStack();
            var itemStackSize = item.itemSlot.ItemStackSize;
            itemStackSize.text = item.stackSize.ToString();
            Debug.Log($"{item.itemData.displayName} total stack is now {item.stackSize}");
        }
        else
        {
            var newItem = new InventoryItem(itemData);
            itemDictionary.Add(itemData, newItem);
            var itemSlot = Instantiate(itemSlotPrefab, itemContent);
            newItem.itemSlot = itemSlot;
            itemSlot.OnRemove += RemoveStack;
            itemSlot.ItemName.text = newItem.itemData.displayName;
            itemSlot.ItemIcon.sprite = newItem.itemData.icon;
            itemSlot.ItemStackSize.text = newItem.stackSize.ToString();
            Debug.Log($"Added {itemData.displayName} to the inventory for the first time");
        }
    }

    public void Remove(ItemData itemData)
    {
        if (itemDictionary.TryGetValue(itemData, out InventoryItem item))
        {
            item.RemoveFromStack();
            if (item.stackSize == 0)
            {
                Destroy(item.itemSlot.gameObject);
                itemDictionary.Remove(itemData);
            }
        }
    }

    public void EnableItemsRemove()
    {
        if (enableRemove.isOn)
        {
            foreach (Transform itemSlot in itemContent)
            {
                itemSlot.Find("RemoveButton").gameObject.SetActive(true);
            }
        }
        else
        {
            foreach (Transform itemSlot in itemContent)
            {
                itemSlot.Find("RemoveButton").gameObject.SetActive(false);
            }
        }
    }
    
    public void RemoveStack(ItemSlot itemSlot)
    {
        Debug.Log("RemoveStack is called");
        itemSlot.OnRemove -= RemoveStack;
        foreach (var key in itemDictionary.Keys)
        {
            if (key.displayName == itemSlot.ItemName.text)
            {
                itemDictionary.Remove(key);
                break;
            }
        }
        Destroy(itemSlot.gameObject);
        Debug.Log("itemSlot destroyed");
    }
}

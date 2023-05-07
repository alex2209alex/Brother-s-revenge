using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using DefaultNamespace;
using TMPro;
using Unity.VisualScripting;
using UnityEngine;
using UnityEngine.UI;

public class Inventory : MonoBehaviour
{
    [SerializeField] private int maxSlots = 12;
    private List<InventoryItem> items = new List<InventoryItem>();

    public Transform itemContent;
    public ItemSlot itemSlotPrefab;

    public Toggle enableRemove;
    
    private void OnEnable() => Collectible.OnItemCollected += Add;
    private void OnDisable() => Collectible.OnItemCollected -= Add;
    
    private InventoryItem foundItem;
    public void Add(ItemData itemData)
    {
        foundItem = items.FirstOrDefault(item => item.itemData == itemData && item.itemData.maxStackSize > item.stackSize);
        if (foundItem != null)
        {
            foundItem.AddToStack();
            var itemStackSize = foundItem.itemSlot.ItemStackSize;
            itemStackSize.text = foundItem.stackSize.ToString();
            Debug.Log($"{foundItem.itemData.displayName} total stack is now {foundItem.stackSize}");
        }
        else
        {
            if(items.Count >= maxSlots) return;
            var newItem = new InventoryItem(itemData);
            items.Add(newItem);
            var itemSlot = Instantiate(itemSlotPrefab, itemContent);
            newItem.itemSlot = itemSlot;
            itemSlot.OnRemove += RemoveStack;
            itemSlot.ItemName.text = newItem.itemData.displayName;
            itemSlot.ItemIcon.sprite = newItem.itemData.icon;
            itemSlot.ItemStackSize.text = newItem.stackSize.ToString();
            EnableItemsRemove();
            Debug.Log($"Added {itemData.displayName} to the inventory for the first time");
        }
    }

    public void Remove(ItemData itemData)
    {
        foundItem = items.FirstOrDefault(item => item.itemData == itemData);
        if (foundItem != null)
        {
            foundItem.RemoveFromStack();
            if (foundItem.stackSize == 0)
            {
                Destroy(foundItem.itemSlot.gameObject);
                items.RemoveAt(items.IndexOf(foundItem));
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
        if (!enableRemove.isOn) return;
        Debug.Log("RemoveStack is called");
        itemSlot.OnRemove -= RemoveStack;
        foreach (var item in items)
        {
            if (item.itemData.displayName == itemSlot.ItemName.text)
            {
                items.Remove(item);
                break;
            }
        }
        Destroy(itemSlot.gameObject);
        Debug.Log("itemSlot destroyed");
    }
}
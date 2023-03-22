using System;
using System.Collections;
using System.Collections.Generic;
using TMPro;
using Unity.VisualScripting;
using UnityEngine;
using UnityEngine.UI;

public class Inventory : MonoBehaviour
{
    private Dictionary<ItemData, InventoryItem> itemDictionary = new Dictionary<ItemData, InventoryItem>();

    public Transform itemContent;
    public GameObject itemSlot;

    public Toggle enableRemove;
    
    private void OnEnable() => Collectible.OnItemCollected += Add;
    private void OnDisable() => Collectible.OnItemCollected -= Add;

    public void Add(ItemData itemData)
    {
        if (itemDictionary.TryGetValue(itemData, out InventoryItem item))
        {
            item.AddToStack();
            var itemStackSize = item.itemObject.transform.Find("StackSize").GetComponent<TextMeshProUGUI>();
            itemStackSize.text = item.stackSize.ToString();
            Debug.Log($"{item.itemData.displayName} total stack is now {item.stackSize}");
        }
        else
        {
            InventoryItem newItem = new InventoryItem(itemData);
            itemDictionary.Add(itemData, newItem);

            GameObject gameObject = Instantiate(itemSlot, itemContent);
            newItem.itemObject = gameObject;
            var itemName = gameObject.transform.Find("ItemName").GetComponent<TextMeshProUGUI>();
            var itemIcon = gameObject.transform.Find("ItemIcon").GetComponent<Image>();
            var itemStackSize = gameObject.transform.Find("StackSize").GetComponent<TextMeshProUGUI>();

            itemName.text = newItem.itemData.displayName;
            itemIcon.sprite = newItem.itemData.icon;
            itemStackSize.text = newItem.stackSize.ToString();
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
                Destroy(item.itemObject);
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

    public void RemoveStack(GameObject itemSlot)
    {
        Debug.Log("RemoveStack is called");
        foreach (var key in itemDictionary.Keys)
        {
            if (key.displayName == itemSlot.transform.Find("ItemName").GetComponent<TextMeshProUGUI>().text)
            {
                itemDictionary.Remove(key);
                break;
            }
        }
        Destroy(itemSlot);
        Debug.Log("itemSlot destroyed");
    }
}

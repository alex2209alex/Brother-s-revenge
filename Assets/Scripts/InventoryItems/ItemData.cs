using UnityEngine;

public abstract class ItemData : ScriptableObject
{
    public string displayName;
    public Sprite icon;
    public int maxStackSize;
    public string itemType;
    public Inventory Inventory { get; set; }
    public abstract void UseItem();
}

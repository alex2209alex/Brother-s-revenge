using System;
using UnityEngine;

public abstract class Collectible : MonoBehaviour
{
    public static event Action<ItemData> OnItemCollected;
    
    [SerializeField] private ItemData collectibleData;

    public ItemData CollectibleData => collectibleData;

    public virtual void Collect()
    {
        Destroy(gameObject);
        OnItemCollected?.Invoke(collectibleData);
    }
}

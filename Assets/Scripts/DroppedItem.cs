using UnityEngine;

public class DroppedItem : Collectible
{
    public override void Collect()
    {
        Debug.Log("Item Collected");
        base.Collect();
    }
}

using UnityEngine;

public class Coin : Collectible
{
    public override void Collect()
    {
        Debug.Log("Coin Collected");
        base.Collect();
    }
}

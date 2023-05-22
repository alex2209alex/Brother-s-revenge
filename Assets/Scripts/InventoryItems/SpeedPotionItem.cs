using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.PlayerLoop;

namespace InventoryItems
{
    [CreateAssetMenu(fileName = "SpeedPotionItem", menuName = "Inventory/SpeedPotionItem")]

    public class SpeedPotionItem : ItemData
    {

        [SerializeField] private float multiplier;

        public override void UseItem()
        {
            Inventory.StartCoroutine(MyCoroutine());
        }

        IEnumerator MyCoroutine()
        {
            Inventory.PlayerAttributes.PlayerMovement.Speed *= multiplier;

            yield return new WaitForSeconds(4f);
            
            Inventory.PlayerAttributes.PlayerMovement.Speed /= multiplier;
        }
    }
}

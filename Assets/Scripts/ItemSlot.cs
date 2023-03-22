using System;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace DefaultNamespace
{
    public class ItemSlot : MonoBehaviour
    {
        [SerializeField] private TextMeshProUGUI itemName; 
        [SerializeField] private Image itemIcon; 
        [SerializeField] private TextMeshProUGUI itemStackSize;

        public TextMeshProUGUI ItemName => itemName;
        public Image ItemIcon => itemIcon;
        public TextMeshProUGUI ItemStackSize => itemStackSize;

        public event Action<ItemSlot> OnRemove;

        public void RemoveItem() => OnRemove?.Invoke(this);
    }
}
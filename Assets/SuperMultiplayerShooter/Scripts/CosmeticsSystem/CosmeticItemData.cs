using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace Visyde
{
    /// <summary>
    /// Cosmetic Item Data
    /// - Holds data for a cosmetic item.
    /// </summary>

    [CreateAssetMenu(fileName = "New Cosmetic Item", menuName = "Visyde/Cosmetic Item")]
    public class CosmeticItemData : ScriptableObject
    {
        public Sprite icon;
        public CosmeticType itemType = CosmeticType.Hat;
        public int price = 0;
        public string inAppID;
        public bool isRewarded = false;
        public GameObject prefab;
    }

    public enum CosmeticType{
        Hat,
        Glasses,
        Necklace
        //...add more!
    }
}
using UnityEngine;
using UnityEngine.UI;
//using UnityEngine.Purchasing;

namespace Visyde
{
    /// <summary>
    /// Inventory Slot
    /// - Sample main menu inventory slot.
    /// - Each slot has this component.
    /// </summary>

    public class InventorySlot : MonoBehaviour
    {
        public CosmeticItemData curItem;
        public Image iconImage;
        public GameObject check;
        public Text priceText;
        public GameObject locked;
        public Button rewardedBtn;
        //public IAPButton iapBtn;

        [HideInInspector] public CharacterCustomizer cc;

        public void Refresh(){
            // Display icon:
            if (curItem){
                iconImage.sprite = curItem.icon;
                iconImage.SetNativeSize();
                //transform.localPosition = Vector2.zero;
                iconImage.rectTransform.anchorMin = new Vector2(0.5f, 0.5f);
                iconImage.rectTransform.anchorMax = new Vector2(0.5f, 0.5f);
                iconImage.rectTransform.anchoredPosition = Vector2.zero;
                iconImage.enabled = true;
                if (curItem.price != 0)
                {
                    if (PlayerPrefs.GetInt(curItem.inAppID,0) == 0)
                    {
                        //priceText.text = curItem.price.ToString();
                        //locked.SetActive(true);
                        //iapBtn.productId = curItem.inAppID;
                        //iapBtn.onPurchaseComplete.AddListener((call) =>
                        //{
                        //    locked.SetActive(false);
                        //    PlayerPrefs.SetInt(curItem.inAppID, 1);
                        //    FindObjectOfType<PlayfabLogin>().SetPlayerData();
                        //});
                    }
                    else
                    {
                        priceText.text = "";
                        locked.SetActive(false);
                    }                  
                }
                else if (curItem.isRewarded)
                {
                    if (PlayerPrefs.GetInt(curItem.inAppID, 0) == 0)
                    {
                        priceText.text = "Watch 1 RewardedAd";
                        locked.SetActive(true);
                        //iapBtn.productId = curItem.inAppID;
                        rewardedBtn.onClick.AddListener(()=> 
                        {
                            //AdsManager.instance.index = 0;
                            //AdsManager.instance.onRewardeCosmeticItems += () =>
                            //{
                            //    locked.SetActive(false);
                            //    PlayerPrefs.SetInt(curItem.inAppID, 1);
                            //    FindObjectOfType<PlayfabLogin>().SetPlayerData();
                            //};
                            //AdsManager.instance.ShowRewardedAd();
                        });
                    }
                    else
                    {
                        priceText.text = "";
                        locked.SetActive(false);
                    }
                }
            }
            else{
                iconImage.enabled = false;
            }

            // Show/hide checkmark:
            if (curItem.itemType == CosmeticType.Hat)
            {
                check.SetActive(DataCarrier.chosenHat >= 0 && ItemDatabase.instance.hats[DataCarrier.chosenHat] == curItem);
            }
            if (curItem.itemType == CosmeticType.Glasses)
            {
                check.SetActive(DataCarrier.chosenGlasses >= 0 && ItemDatabase.instance.glasses[DataCarrier.chosenGlasses] == curItem);
            }
            if (curItem.itemType == CosmeticType.Necklace)
            {
                check.SetActive(DataCarrier.chosenNecklace >= 0 && ItemDatabase.instance.necklaces[DataCarrier.chosenNecklace] == curItem);
            }
        }

        public void Select(){
            if (curItem) cc.Equip(curItem);
        }
    }
}
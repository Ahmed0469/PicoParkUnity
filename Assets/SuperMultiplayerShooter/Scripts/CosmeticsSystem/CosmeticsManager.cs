using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace Visyde
{
    /// <summary>
    /// Cosmetics Manager
    /// - Manages all the player cosmetic side of things
    /// - Attached on the player prefab (along with the PlayerController component)
    /// </summary>

    public class CosmeticsManager : MonoBehaviour
    {
        // Internals:
        public int chosenHat;
        public int lastChosenHat;
        public int chosenGlasses;
        public int lastChosenGlasses;
        public int chosenNecklace;
        public int lastChosenNecklace;
        PlayerController player;
        List<GameObject> spawnedHats = new List<GameObject>();
        List<GameObject> spawnedGlasses = new List<GameObject>();
        List<GameObject> spawnedNecklaces = new List<GameObject>();

        void OnEnable(){
            player = GetComponent<PlayerController>();

            if (!player){
                Destroy(this);
                Debug.LogError("Cosmetics Manager should be attach to the same object the Player Controller is attached to.");
            }
        }

        public void Refresh(Cosmetics cosmetics){
            chosenHat = cosmetics.hat;
            chosenGlasses = cosmetics.glasses;
            chosenNecklace = cosmetics.necklace;

            Refresh();
        }
        public void Refresh()
        {
            // Remove existing cosmetic items if there's any:
            if (lastChosenHat != chosenHat)
            {
                for (int i = 0; i < spawnedHats.Count; i++)
                {
                    if (spawnedHats[i]) Destroy(spawnedHats[i]);
                }
                spawnedHats = new List<GameObject>();
                lastChosenHat = chosenHat;
            }            
            // If has hat:
            if (chosenHat >= 0)
            {
                GameObject item = Instantiate(ItemDatabase.instance.hats[chosenHat].prefab, player.character.hatPoint);
                ResetItemPosition(item.transform);
                spawnedHats.Add(item);
            }

            if (lastChosenGlasses != chosenGlasses)
            {
                // Remove existing cosmetic items if there's any:
                for (int i = 0; i < spawnedGlasses.Count; i++)
                {
                    if (spawnedGlasses[i]) Destroy(spawnedGlasses[i]);
                }
                spawnedGlasses = new List<GameObject>();
                lastChosenGlasses = chosenGlasses;
            }            
            // If has hat:
            if (chosenGlasses >= 0)
            {
                GameObject item = Instantiate(ItemDatabase.instance.glasses[chosenGlasses].prefab, player.character.glassesPoint);
                ResetItemPosition(item.transform);
                spawnedGlasses.Add(item);
            }

            if (lastChosenNecklace != chosenNecklace)
            {
                // Remove existing cosmetic items if there's any:
                for (int i = 0; i < spawnedNecklaces.Count; i++)
                {
                    if (spawnedNecklaces[i]) Destroy(spawnedNecklaces[i]);
                }
                spawnedNecklaces = new List<GameObject>();
                lastChosenNecklace = chosenNecklace;
            }            
            // If has hat:
            if (chosenNecklace >= 0)
            {
                GameObject item = Instantiate(ItemDatabase.instance.necklaces[chosenNecklace].prefab, player.character.necklacePoint);
                ResetItemPosition(item.transform);
                spawnedNecklaces.Add(item);
            }
        }

        void ResetItemPosition(Transform item){
            item.localEulerAngles = Vector3.zero;
            item.localPosition = Vector3.zero;
            //item.localScale = Vector3.one;
        }
    }
}
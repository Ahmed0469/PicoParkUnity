﻿using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace Visyde
{
    public class ItemDatabase : MonoBehaviour
    {
        public static ItemDatabase instance;

        public CosmeticItemData[] hats;
        public CosmeticItemData[] glasses;
        public CosmeticItemData[] necklaces;

        void Awake()
        {
            if (instance){
                Destroy(gameObject);
                return;
            }
            instance = this;
            DontDestroyOnLoad(gameObject);
        }
    }
}
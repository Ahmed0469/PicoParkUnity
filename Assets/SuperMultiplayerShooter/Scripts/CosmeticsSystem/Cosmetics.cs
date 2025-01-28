using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace Visyde
{
    /// <summary>
    /// Cosmetics
    /// - Used by the PlayerInstance class.
    /// - Downloaded and readable version of a player's cosmetic data.
    /// </summary>

    public class Cosmetics
    {
        public int hat;
        public int glasses;
        public int necklace;

        public Cosmetics(int hat = -1, int glasses = -1,int necklace = -1)
        {
            this.hat = hat;
            this.glasses = glasses;
            this.necklace = necklace;
        }
    }
}
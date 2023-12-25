﻿using UnityEngine;

namespace GameKit.Dependencies.Utilities.Types
{


    [System.Serializable]
    public struct IntRange
    {
        public IntRange(int minimum, int maximum)
        {
            Minimum = minimum;
            Maximum = maximum;
        }
        /// <summary>
        /// Minimum range.
        /// </summary>
        public int Minimum;
        /// <summary>
        /// Maximum range.
        /// </summary>
        public int Maximum;

        /// <summary>
        /// Returns an exclusive random value between Minimum and Maximum.
        /// </summary>
        /// <returns></returns>
        public int RandomExclusive() => Ints.RandomExclusiveRange(Minimum, Maximum);
        /// <summary>
        /// Returns an inclusive random value between Minimum and Maximum.
        /// </summary>
        /// <returns></returns>
        public int RandomInclusive() => Ints.RandomInclusiveRange(Minimum, Maximum);

        /// <summary>
        /// Clamps value between Minimum and Maximum.
        /// </summary>
        public int Clamp(int value) => Ints.Clamp(value, Minimum, Maximum);

        /// <summary>
        /// True if value is within range of Minimum and Maximum.
        /// </summary>
        public bool InRange(int value) => (value >= Minimum) && (value <= Maximum);

    }


}
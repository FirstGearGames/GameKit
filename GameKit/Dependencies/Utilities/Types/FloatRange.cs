using UnityEngine;

namespace GameKit.Dependencies.Utilities.Types
{


    [System.Serializable]
    public struct FloatRange
    {
        public FloatRange(float minimum, float maximum)
        {
            Minimum = minimum;
            Maximum = maximum;
        }
        /// <summary>
        /// Minimum range.
        /// </summary>
        public float Minimum;
        /// <summary>
        /// Maximum range.
        /// </summary>
        public float Maximum;

        /// <summary>
        /// Returns a random value between Minimum and Maximum.
        /// </summary>
        public float RandomInclusive()
        {
            return Floats.RandomInclusiveRange(Minimum, Maximum);
        }
        /// <summary>
        /// Lerps between Minimum and Maximum.
        /// </summary>
        public float Lerp(float percent)
        {
            return Mathf.Lerp(Minimum, Maximum, percent);
        }

        /// <summary>
        /// Clamps value between Minimum and Maximum.
        /// </summary>
        public float Clamp(float value)
        {
            return Mathf.Clamp(value, Minimum, Maximum);
        }

    }


}
using UnityEngine;

namespace GameKit.Dependencies.Utilities.Types
{


    [System.Serializable]

    public struct FloatRange2D
    {
        public FloatRange X;
        public FloatRange Y;

        public FloatRange2D(FloatRange x, FloatRange y)
        {
            X = x;
            Y = y;
        }


        public FloatRange2D(float xMin, float xMax, float yMin, float yMax)
        {
            X = new FloatRange(xMin, xMax);
            Y = new FloatRange(yMin, yMax);
        }

        /// <summary>
        /// Clamps value between X, Y Minimum and Maximum.
        /// </summary>
        public Vector2 Clamp(Vector2 value)
        {
            return new Vector2(
                ClampX(value.x),
                ClampY(value.y)
                );
        }

        /// <summary>
        /// Clamps value between X, Y Minimum and Maximum, while keeping Z.
        /// </summary>
        public Vector3 Clamp(Vector3 value)
        {
            return new Vector3(
                ClampX(value.x),
                ClampY(value.y),
                value.z
                );
        }

        /// <summary>
        /// Clamps value between X Minimum and Maximum.
        /// </summary>
        public float ClampX(float value)
        {
            return Mathf.Clamp(value, X.Minimum, X.Maximum);
        }

        /// <summary>
        /// Clamps value between Y Minimum and Maximum.
        /// </summary>
        public float ClampY(float value)
        {
            return Mathf.Clamp(value, Y.Minimum, Y.Maximum);
        }

    }



}
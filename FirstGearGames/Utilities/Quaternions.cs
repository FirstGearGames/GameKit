using UnityEngine;

namespace FirstGearGames.Utilities.Maths
{

    public static class Quaternions
    {

        /// <summary>
        /// Returns if a rotational value matches another. This method is preferred over Equals or == since those variations allow larger differences before returning false.
        /// </summary>
        /// <param name="r"></param>
        /// <param name="target"></param>
        /// <param name="distance"></param>
        /// <returns></returns>
        public static bool Matches(this Quaternion r, Quaternion target, float? distance = null)
        {
            if (distance == null)
            {
                return (r.w == target.w && r.x == target.x && r.y == target.y && r.z == target.z);
            }
            else
            {
                float a = Vector3.SqrMagnitude(r.eulerAngles - target.eulerAngles);
                return (a <= (distance * distance));
            }
        }
    }

}
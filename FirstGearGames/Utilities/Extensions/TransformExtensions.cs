using UnityEngine;

namespace FirstGearGames.Utilities
{

    public static class TransformExtensions
    {
        /// <summary>
        /// Returns how many entries can fit into a GridLayoutGroup
        /// </summary>
        /// <param name=""></param>
        public static void SetParentAndKeepTransformValues(this Transform src, Transform parent)
        {
            Vector3 pos = src.position;
            Quaternion rot = src.rotation;
            Vector3 scale = src.localScale;

            src.SetParent(parent);
            src.position = pos;
            src.rotation = rot;
            src.localScale = scale;
        }

    }


}
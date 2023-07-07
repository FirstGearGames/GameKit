
using System.Collections.Generic;
using UnityEngine;

namespace FirstGearGames.Utilities.Objects
{

    public static class Transforms
    {
        /// <summary>
        /// Destroys all children under the specified transform.
        /// </summary>
        /// <param name="t"></param>
        public static void DestroyChildren(this Transform t, bool destroyImmediately = false)
        {
            foreach (Transform child in t)
            {
                if (destroyImmediately)
                    MonoBehaviour.DestroyImmediate(child.gameObject);
                else
                    MonoBehaviour.Destroy(child.gameObject);
            }
        }

        /// <summary>
        /// Gets components in children and optionally parent.
        /// </summary>
        /// <typeparam name="T"></typeparam>
        /// <param name="results"></param>
        /// <param name="parent"></param>
        /// <param name="includeParent"></param>
        /// <param name="includeInactive"></param>
        public static void GetComponentsInChildren<T>(Transform parent, List<T> results, bool includeParent = true, bool includeInactive = false) where T : Component
        {
            if (!includeParent)
            {
                List<T> current = new List<T>();
                for (int i = 0; i < parent.childCount; i++)
                {
                    parent.GetChild(i).GetComponentsInChildren(includeInactive, current);
                    results.AddRange(current);
                }
            }
            else
            {
                parent.GetComponentsInChildren(includeInactive, results);
            }
        }

        /// <summary>
        /// Returns the position of this transform.
        /// </summary>
        public static Vector3 GetPosition(this Transform t, bool localSpace)
        {
            return (localSpace) ? t.localPosition : t.position;
        }
        /// <summary>
        /// Returns the rotation of this transform.
        /// </summary>
        public static Quaternion GetRotation(this Transform t, bool localSpace)
        {
            return (localSpace) ? t.localRotation : t.rotation;
        }
        /// <summary>
        /// Returns the scale of this transform.
        /// </summary>
        public static Vector3 GetScale(this Transform t)
        {
            return t.localScale;
        }

        /// <summary>
        /// Sets the position of this transform.
        /// </summary>
        /// <param name="t"></param>
        /// <param name="localSpace"></param>
        public static void SetPosition(this Transform t, bool localSpace, Vector3 pos)
        {
            if (localSpace)
                t.localPosition = pos;
            else
                t.position = pos;
        }
        /// <summary>
        /// Sets the position of this transform.
        /// </summary>
        /// <param name="t"></param>
        /// <param name="localSpace"></param>
        public static void SetRotation(this Transform t, bool localSpace, Quaternion rot)
        {
            if (localSpace)
                t.localRotation = rot;
            else
                t.rotation = rot;
        }
        /// <summary>
        /// Sets the position of this transform.
        /// </summary>
        /// <param name="t"></param>
        /// <param name="localSpace"></param>
        public static void SetScale(this Transform t, Vector3 scale)
        {
            t.localScale = scale;
        }



    }



}
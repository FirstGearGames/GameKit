using FirstGearGames.Utilities.Objects;
using FishNet;
using System.Runtime.CompilerServices;
using UnityEngine;


namespace GameKit.Utilities.FloatingOptionMenus
{

    public class FloatingOptionMenu : CanvasGroupFader
    {
        /// <summary>
        /// True if not visible, or in the process of resetting.
        /// </summary>
        public bool IsHiding => !IsVisible;
        /// <summary>
        /// True if visible. Could be true if in the progress of resetting as well; see IsResetting and IsHiding.
        /// </summary>
        public bool IsVisible { get; protected set; }
        
        public virtual void Show(Vector3 position, Quaternion rotation, Vector3 scale, params ButtonData[] buttonDatas)
        {
            gameObject.SetActive(true);
            IsVisible = true;
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public virtual void Show(Vector3 position, params ButtonData[] buttonDatas)
        {
            Show(position, Quaternion.identity, Vector3.one, buttonDatas);
        }
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public virtual void Show(Vector3 position, Quaternion rotation, params ButtonData[] buttonDatas)
        {
            Show(position, rotation, Vector3.one, buttonDatas);
        }
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public virtual void Show(Transform startingPoint, params ButtonData[] buttonDatas)
        {
            if (startingPoint == null)
            {
                InstanceFinder.NetworkManager.LogError($"A null Transform cannot be used as the starting point.");
                return;
            }

            Show(startingPoint.position, startingPoint.rotation, startingPoint.localScale, buttonDatas);
        }

        public void Hide()
        {
            IsVisible = false;
            gameObject.SetActive(false);
        }

    }


}
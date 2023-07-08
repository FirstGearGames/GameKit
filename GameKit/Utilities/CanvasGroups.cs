using UnityEngine;


namespace GameKit.Utilities
{
    /// <summary>
    /// Ways a CanvasGroup can have it's blocking properties modified.
    /// </summary>
    public enum CanvasGroupBlockingTypes
    {
        DoNotBlock = 0,
        Block = 1,
    }

    public static class CanvaseGroups
    {

        /// <summary>
        /// Sets a CanvasGroup blocking type and alpha.
        /// </summary>
        /// <param name="blockingType">How to handle interactions.</param>
        /// <param name="alpha">Alpha for CanvasGroup.</param>
        public static void SetActive(this CanvasGroup group, CanvasGroupBlockingTypes blockingType, float alpha)
        {
            bool block = (blockingType == CanvasGroupBlockingTypes.Block);
            group.blocksRaycasts = block;
            group.interactable = block;
            group.alpha = alpha;
        }

        /// <summary>
        /// Sets a canvasGroup active with specified alpha.
        /// </summary>
        public static void SetActive(this CanvasGroup group, float alpha)
        {
            group.SetActive(true, false);
            group.alpha = alpha;
        }

        /// <summary>
        /// Sets a canvasGroup inactive with specified alpha.
        /// </summary>
        public static void SetInactive(this CanvasGroup group, float alpha)
        {
            group.SetActive(false, false);
            group.alpha = alpha;
        }

        /// <summary>
        /// Sets a group active state by changing alpha and interaction toggles.
        /// </summary>
        public static void SetActive(this CanvasGroup group, bool active, bool setAlpha)
        {
            if (group == null)
                return;

            if (setAlpha)
            {
                if (active)
                    group.alpha = 1f;
                else
                    group.alpha = 0f;
            }

            group.interactable = active;
            group.blocksRaycasts = active;
        }

        /// <summary>
        /// Sets a group active state by changing alpha and interaction toggles with a custom alpha.
        /// </summary>
        public static void SetActive(this CanvasGroup group, bool active, float alpha)
        {
            if (group == null)
                return;

            group.alpha = alpha;

            group.interactable = active;
            group.blocksRaycasts = active;
        }
    }

}
using UnityEngine;


namespace GameKit.Utilities
{


    public static class CanvaseGroups
    {

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
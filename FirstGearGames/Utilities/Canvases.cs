using UnityEngine;



namespace FirstGearGames.Utilities.Objects
{


    public static class Canvases
    {
        /// <summary>
        /// Sets a group visible by changing alpha without changing interaction toggles.
        /// </summary>
        /// <param name="visible"></param>
        public static void SetVisible(this CanvasGroup group, bool visible)
        {
            group.alpha = (visible) ? 1f : 0f;
        }
        /// <summary>
        /// Sets a group visible by changing alpha without changing interaction toggles.
        /// </summary>
        /// <param name="visible"></param>
        public static void SetVisible(this CanvasGroup group, float alpha)
        {
            group.alpha = alpha;
        }

        /// <summary>
        /// Sets a group active state by changing alpha and interaction toggles.
        /// </summary>
        /// <param name="group"></param>
        /// <param name="active"></param>
        /// <param name="setAlpha"></param>
        public static void SetActive(this CanvasGroup group, bool active, bool setAlpha)
        {
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
        /// <param name="group"></param>
        /// <param name="active"></param>
        /// <param name="setAlpha"></param>
        public static void SetActive(this CanvasGroup group, bool active, float alpha)
        {
            group.alpha = alpha;

            group.interactable = active;
            group.blocksRaycasts = active;
        }
    }

}
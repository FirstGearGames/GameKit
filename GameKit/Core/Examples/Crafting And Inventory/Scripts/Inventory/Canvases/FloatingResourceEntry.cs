using GameKit.Utilities;
using GameKit.Utilities.Types.CanvasContainers;
using TMPro;
using UnityEngine;

namespace GameKit.Examples.Inventories.Canvases
{

    public class FloatingResourceEntry : FloatingImage
    {
        #region Serialized.
        /// <summary>
        /// Text used to show the items count.
        /// </summary>
        [Tooltip("Text used to show the items count.")]
        [SerializeField]
        protected TextMeshProUGUI ItemCountText;
        #endregion

        #region Protected.
        /// <summary>
        /// How long it should take to fade in the CanvasGroup.
        /// </summary>
        protected override float FadeInDuration => 0f;
        /// <summary>
        /// How long it should take to fade out the CanvasGroup.
        /// </summary>
        override protected float FadeOutDuration => 0f;
        #endregion

        /// <summary>
        /// Sets which sprite to use.
        /// </summary>
        /// <param name="sprite">Sprite to use.</param>
        /// <param name="sizeOverride">When has value the renderer will be set to this size. Otherwise, the size of the sprite will be used. This value assumes the sprite anchors are set to center.</param>
        public virtual void Initialize(Sprite sprite, Vector3? sizeOverride, int itemCount)
        {
            base.SetSprite(sprite, sizeOverride);
            ItemCountText.text = (itemCount > 1) ? itemCount.ToString() : string.Empty;
        }

        //Do not modify interactable state of canvasgroup.
        protected override void SetCanvasGroupBlockingType(CanvasGroupBlockingType blockingType) { }

    }

}
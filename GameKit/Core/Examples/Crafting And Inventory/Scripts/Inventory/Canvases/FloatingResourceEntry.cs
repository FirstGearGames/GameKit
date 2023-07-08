using GameKit.Utilities;
using GameKit.Utilities.Types.OverlayContainers;
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

        public override void ShowImmediately()
        {
            base.CanvasGroup.SetActive(CanvasGroupBlockingTypes.DoNotBlock, 1f);
        }
        public override void HideImmediately()
        {
            base.CanvasGroup.SetActive(CanvasGroupBlockingTypes.DoNotBlock, 0f);
        }
        public override void Show()
        {
            base.CanvasGroup.SetActive(CanvasGroupBlockingTypes.DoNotBlock, 1f);
        }
        public override void Hide()
        {
            base.CanvasGroup.SetActive(CanvasGroupBlockingTypes.DoNotBlock, 0f);
        }

    }

}
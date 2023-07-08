using GameKit.Utilities.Types.OverlayContainers;
using TMPro;
using UnityEngine;

namespace GameKit.Examples.Inventories.Canvases
{

    public class FloatingResourceEntry : FloatingImage
    {
        /// <summary>
        /// Text used to show the items count.
        /// </summary>
        [Tooltip("Text used to show the items count.")]
        [SerializeField]
        protected TextMeshProUGUI _itemCountText;

        /// <summary>
        /// Sets which sprite to use.
        /// </summary>
        /// <param name="sprite">Sprite to use.</param>
        /// <param name="sizeOverride">When has value the renderer will be set to this size. Otherwise, the size of the sprite will be used. This value assumes the sprite anchors are set to center.</param>
        public virtual void Initialize(Sprite sprite, Vector3? sizeOverride, int itemCount)
        {
            base.SetSprite(sprite, sizeOverride);
            _itemCountText.text = (itemCount > 1) ? itemCount.ToString() : string.Empty;
        }

    }

}
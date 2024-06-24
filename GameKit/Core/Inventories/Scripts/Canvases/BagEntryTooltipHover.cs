using UnityEngine;
using GameKit.Core.FloatingContainers.Tooltips;
using GameKit.Core.Inventories.Bags;
using UnityEngine.EventSystems;

namespace GameKit.Core.Inventories.Canvases
{
    public class BagEntryTooltipHover : PointerMonoBehaviour
    {
        #region Private.
        /// <summary>
        /// Bag to show tooltip information for.
        /// </summary>
        private BagData _bag;
        /// <summary>
        /// TooltipCanvas to use.
        /// </summary>
        private FloatingTooltipCanvas _tooltipCanvas;
        /// <summary>
        /// Where to anchor tooltips.
        /// </summary>
        private readonly Vector2 _tooltipPivot = new Vector2(0.0f, 1f);
        #endregion

        public void InitializeOnce(BagData bag, FloatingTooltipCanvas tooltipCanvas)
        {
            _bag = bag;
            _tooltipCanvas = tooltipCanvas;
        }
        /// <summary>
        /// Called when entry hovered state changes.
        /// </summary>
        public override void OnHovered(bool hovered, PointerEventData eventData)
        {
            bool show = (hovered && (_bag != null));
            if (show)
            {
                Vector2 position = new Vector2(transform.position.x, transform.position.y);
                string text = $"{_bag.Name}:\r\n{_bag.Description}";
                _tooltipCanvas.Show(this, position, text, _tooltipPivot, FloatingTooltipCanvas.TextAlignmentStyle.TopLeft);
            }
            else
            {
                _tooltipCanvas.Hide(this);
            }
        }


    }


}
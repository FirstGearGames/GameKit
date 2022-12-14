using GameKit.Resources;
using GameKit.Resources.Managers;
using FishNet;
using TMPro;
using UnityEngine;
using UnityEngine.UI;
using TriInspector;
using GameKit.Examples.Resources;
using UnityEngine.EventSystems;
using GameKit.Examples.Tooltips.Canvases;

namespace GameKit.Examples.Inventories.Canvases
{

    public class ResourceEntry : MonoBehaviour, IPointerEnterHandler, IPointerExitHandler, IPointerDownHandler, IPointerUpHandler
    {
        #region Public.
        /// <summary>
        /// IResourceData for this entry.
        /// </summary>
        public IResourceData IResourceData;
        /// <summary>
        /// Custom ResourceData for IResourceData.
        /// </summary>
        public ResourceData ResourceData => (ResourceData)IResourceData;
        #endregion

        #region Serialized.
        /// <summary>
        /// Button on the entry.
        /// </summary>
        [SerializeField]
        private Button _button;
        /// <summary>
        /// Icon for resource.
        /// </summary>
        [PropertyTooltip("Icon for resource.")]
        [SerializeField]
        private Image _icon;
        /// <summary>
        /// Text for stack size.
        /// </summary>
        [PropertyTooltip("Text for stack size.")]
        [SerializeField]
        private TextMeshProUGUI _stackText;
        #endregion

        #region Private.
        /// <summary>
        /// InventoryCanvas for this entry.
        /// </summary>
        private InventoryCanvas _inventoryCanvas;
        /// <summary>
        /// TooltipCanvas to use.
        /// </summary>
        private TooltipCanvas _tooltipCanvas;
        /// <summary>
        /// True if the pointer is pressing this object.
        /// </summary>
        private bool _pressed;
        /// <summary>
        /// True if the pointer is hovering over this object.
        /// </summary>
        private bool _hovered;
        /// <summary>
        /// Where to anchor tooltips.
        /// </summary>
        private readonly Vector2 _tooltipPivot = new Vector2(0.5f, 1f);
        /// <summary>
        /// Offset to apply for tooltip position.
        /// </summary>
        private readonly Vector2 _tooltipOffset = new Vector2(0f, 64f);
        #endregion

        private void Awake()
        {
            _button.onClick.AddListener(OnClick_Button);
        }

        private void OnDestroy()
        {
            _button.onClick.RemoveListener(OnClick_Button);
        }

        /// <summary>
        /// Initializes this entry.
        /// </summary>
        public void Initialize(InventoryCanvas inventoryCanvas, TooltipCanvas tooltipCanvas, ResourceQuantity rq)
        {
            //If no data then initialize empty.
            if (rq.IsUnset)
            {
                Initialize(inventoryCanvas, tooltipCanvas);
                return;
            }

            _inventoryCanvas = inventoryCanvas;
            _tooltipCanvas = tooltipCanvas;
            IResourceData = InstanceFinder.NetworkManager.GetInstance<ResourceManager>().GetIResourceData(rq.ResourceId);
            _icon.sprite = ResourceData.GetIcon();
            _stackText.text = (rq.Quantity > 1) ? $"{rq.Quantity}" : string.Empty;

            UpdateComponentStates();
        }

        /// <summary>
        /// Initializes this with no data, resetting values.
        /// </summary>
        public void Initialize(InventoryCanvas inventoryCanvas, TooltipCanvas tooltipCanvas)
        {
            _inventoryCanvas = inventoryCanvas;
            _tooltipCanvas = tooltipCanvas;
            IResourceData = null;
            _stackText.text = string.Empty;
            UpdateComponentStates();
        }

        /// <summary>
        /// Updates component states based on if resource data is available.
        /// </summary>
        private void UpdateComponentStates()
        {
            bool hasData = (IResourceData != null);

            _icon.enabled = hasData;
            _button.enabled = hasData;
            SetSelectable(hasData);

        }
        /// <summary>
        /// Modifies this entries selectability. 
        /// </summary>
        /// <param name="matches"></param>
        public void SetSelectable(bool selectable)
        {
            Color c = (selectable) ?
                Color.white : (Color.white * 0.4f);

            _icon.color = c;
            _stackText.color = c;
        }

        /// <summary>
        /// Called when the entry is pressed.
        /// </summary>
        public void OnClick_Button()
        {
            _inventoryCanvas.SelectResourceEntry(this);
        }

        /// <summary>
        /// Called when the pointer enters this objects rect transform.
        /// </summary>
        public void OnPointerEnter(PointerEventData eventData) => SetHovered(true);
        /// <summary>
        /// Called when the pointer exits this objects rect transform.
        /// </summary>
        public void OnPointerExit(PointerEventData eventData) => SetHovered(false);
        /// <summary>
        /// Called when the pointer presses this objects rect transform.
        /// </summary>
        public void OnPointerDown(PointerEventData eventData) => SetPressed(true);
        /// <summary>
        /// Called when the pointer releases this objects rect transform.
        /// </summary>
        public void OnPointerUp(PointerEventData eventData) => SetPressed(false);

        /// <summary>
        /// Sets pressed and updates tooltip if needed.
        /// </summary>
        private void SetPressed(bool pressed)
        {
            _pressed = pressed;
            SetTooltip();
        }
        /// <summary>
        /// Sets hovered and updates tooltip if needed.
        /// </summary>
        private void SetHovered(bool hovered)
        {
            _hovered = hovered;
            SetTooltip();
        }
        /// <summary>
        /// Shows or hides the tooltip for this entry.
        /// </summary>
        private void SetTooltip()
        {
            bool show = (ResourceData != null) && (!_pressed && _hovered);
            if (show)
            {
                Vector2 position = new Vector2(transform.position.x, transform.position.y);
                string description = ResourceData.Description;
                _tooltipCanvas.Show(this, position - _tooltipOffset, description, _tooltipPivot);
            }
            else
            {
                _tooltipCanvas.Hide(this);
            }
        }
    }


}
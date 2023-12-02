using FishNet;
using TMPro;
using UnityEngine;
using UnityEngine.UI;
using UnityEngine.EventSystems;
using GameKit.Core.FloatingContainers.Tooltips;
using GameKit.Core.Resources;
using GameKit.Core.Inventories;
using GameKit.Core.CraftingAndInventories.Resources;

namespace GameKit.Core.CraftingAndInventories.Inventories.Canvases
{

    public class ResourceEntry : PointerMonoBehaviour
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
        /// <summary>
        /// Bag and slot index where this resource sits.
        /// </summary>
        public ActiveBagResource BagSlot { get; private set; }
        /// <summary>
        /// Current number of items on the stack.
        /// </summary>
        public int StackCount { get; private set; }
        #endregion

        #region Serialized.
        /// <summary>
        /// CanvasGroup for this resource entry.
        /// </summary>
        public CanvasGroup CanvasGroup => _canvasGroup;
        [SerializeField]
        private CanvasGroup _canvasGroup;
        /// <summary>
        /// Button on the entry.
        /// </summary>
        [SerializeField]
        private Button _button;
        /// <summary>
        /// Icon for resource.
        /// </summary>
        [Tooltip("Icon for resource.")]
        [SerializeField]
        private Image _icon;
        /// <summary>
        /// Text for stack size.
        /// </summary>
        [Tooltip("Text for stack size.")]
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
        private FloatingTooltipCanvas _tooltipCanvas;
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
        public void Initialize(InventoryCanvas inventoryCanvas, FloatingTooltipCanvas tooltipCanvas, ResourceQuantity rq, ActiveBagResource bagSlot)
        {
            //If no data then initialize empty.
            if (rq.IsUnset)
            {
                Initialize(inventoryCanvas, tooltipCanvas, bagSlot);
                return;
            }

            SetBagSlot(bagSlot);
            _inventoryCanvas = inventoryCanvas;
            _tooltipCanvas = tooltipCanvas;
            IResourceData = InstanceFinder.NetworkManager.GetInstance<ResourceManager>().GetIResourceData(rq.ResourceId);
            _icon.sprite = ResourceData.GetIcon();
            StackCount = rq.Quantity;
            _stackText.text = (StackCount > 1) ? $"{rq.Quantity}" : string.Empty;

            UpdateComponentStates();
        }

        /// <summary>
        /// Initializes this with no data, resetting values.
        /// </summary>
        public void Initialize(InventoryCanvas inventoryCanvas, FloatingTooltipCanvas tooltipCanvas, ActiveBagResource bagSlot)
        {
            SetBagSlot(bagSlot);
            _inventoryCanvas = inventoryCanvas;
            _tooltipCanvas = tooltipCanvas;
            IResourceData = null;
            _stackText.text = string.Empty;
            UpdateComponentStates();
        }

        /// <summary>
        /// Sets which bag and slot index this entry is within.
        /// </summary>
        /// <param name="bagIndex">Bag index of this entry.</param>
        /// <param name="slotIndex">Slot index of this entry.</param>
        public void SetBagSlot(ActiveBagResource bagSlot)
        {
            BagSlot = bagSlot;
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
        /// Sets pressed and updates tooltip if needed.
        /// </summary>
        public override void OnPressed(bool pressed, PointerEventData eventData)
        {
            //If changed update inventory canvas.
            if (pressed != _pressed)
            {
                if (pressed)
                    _inventoryCanvas.OnHeld_ResourceEntry(this);
                else
                    _inventoryCanvas.OnRelease_ResourceEntry(this);
            }

            _pressed = pressed;
            SetTooltip();
        }
        /// <summary>
        /// Sets hovered and updates tooltip if needed.
        /// </summary>
        public override void OnHovered(bool hovered, PointerEventData data)
        {
            //If changed update inventory canvas.
            if (_hovered != hovered)
            {
                if (hovered)
                    _inventoryCanvas.OnEnter_ResourceEntry(this);
                else
                    _inventoryCanvas.OnExit_ResourceEntry(this);
            }

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
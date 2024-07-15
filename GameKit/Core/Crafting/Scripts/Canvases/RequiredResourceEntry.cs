using GameKit.Core.Inventories;
using GameKit.Core.Resources;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace GameKit.Core.Crafting.Canvases
{

    public class RequiredResourceEntry : MonoBehaviour
    {
        /// <summary>
        /// Icon for the resource.
        /// </summary>
        [Tooltip("Icon for the resource.")]
        [SerializeField]
        private Image _icon;
        /// <summary>
        /// Text to show name of the resource.
        /// </summary>
        [Tooltip("Text to show name of the resource.")]
        [SerializeField]
        private TextMeshProUGUI _nameText;
        /// <summary>
        /// Text to show quantity needed.
        /// </summary>
        [Tooltip("Text to show quantity needed.")]
        [SerializeField]
        private TextMeshProUGUI _quantityText;
        /// <summary>
        /// Quantity of resource required.
        /// </summary>
        private SerializableResourceQuantity _resourceQuantity;
        /// <summary>
        /// Inventory to pull information from.
        /// </summary>
        private InventoryBase _inventory;

        /// <summary>
        /// Initializes with values.
        /// </summary>
        /// <param name="rm">ResourceManager reference.</param>
        /// <param name="rq">Resource information to display.</param>
        public void Initialize(ResourceManager rm, SerializableResourceQuantity rq, InventoryBase inventoryBase)
        {
            _resourceQuantity = rq;
            _inventory = inventoryBase;
            ResourceData rd = rm.GetResourceData(rq.UniqueId);
            _nameText.text = rd.DisplayName;
            _icon.sprite = rd.Icon;
            UpdateAvailable();
        }

        /// <summary>
        /// Updates how many resources are available in the quantity.
        /// </summary>
        public void UpdateAvailable()
        {
            if (_inventory == null)
                return;

            int current = _inventory.GetResourceQuantity(_resourceQuantity.UniqueId);
            _quantityText.text = $"{current} / {_resourceQuantity.Quantity}";
        }

        /// <summary>
        /// Resets as if unused.
        /// </summary>
        public void ResetValues()
        {
            _nameText.text = string.Empty;
            _quantityText.text = string.Empty;
            _icon.sprite = null;
        }
    }


}
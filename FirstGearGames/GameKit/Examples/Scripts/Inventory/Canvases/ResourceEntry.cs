using GameKit.Resources;
using GameKit.Resources.Managers;
using FishNet;
using TMPro;
using UnityEngine;
using UnityEngine.UI;
using TriInspector;
using GameKit.Examples.Resources;

namespace GameKit.Examples.Inventories.Canvases
{

    public class ResourceEntry : MonoBehaviour
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
        private InventoryCanvas _canvas;
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
        public void Initialize(InventoryCanvas canvas, ResourceQuantity rq)
        {
            //If no data then initialize empty.
            if (rq.IsUnset)
            {
                Initialize(canvas);
                return;
            }

            _canvas = canvas;
            IResourceData = InstanceFinder.NetworkManager.GetInstance<ResourceManager>().GetIResourceData(rq.ResourceId);
            _icon.sprite = ResourceData.GetIcon();
            _stackText.text = (rq.Quantity > 1) ? $"{rq.Quantity}" : string.Empty;

            UpdateComponentStates();
        }

        /// <summary>
        /// Initializes this with no data, resetting values.
        /// </summary>
        public void Initialize(InventoryCanvas canvas)
        {
            _canvas = canvas;
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
            _canvas.SelectResourceEntry(this);
        }
    }


}
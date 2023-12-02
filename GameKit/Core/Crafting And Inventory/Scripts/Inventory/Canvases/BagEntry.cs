using FishNet;
using System.Collections.Generic;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

using GameKit.Dependencies.Utilities;
using GameKit.Core.FloatingContainers.Tooltips;
using GameKit.Dependencies.Inspectors;
using GameKit.Core.Inventories;
using GameKit.Core.Resources;
using GameKit.Core.Inventories.Bags;
using UnityEngine.EventSystems;

namespace GameKit.Core.CraftingAndInventories.Inventories.Canvases
{
    public class BagEntry : PointerMonoBehaviour
    {
        #region Public.
        /// <summary>
        /// ActiveBag this entry is for.
        /// </summary>
        public ActiveBag ActiveBag { get; private set; }
        /// <summary>
        /// ResourceEntrys within Bag.
        /// </summary>
        public List<ResourceEntry> ResourceEntries { get; private set; } = new();
        #endregion

        #region Serialized.
        /// <summary>
        /// RectTransform for this script. Used to resize based on number of resource entries and bag size.
        /// </summary>
        [Tooltip("RectTransform for this script. Used to resize based on number of resource entries and bag size.")]
        [SerializeField, Group("Sizing")]
        private RectTransform _rectTransform;
        /// <summary>
        /// RectTransform for the bag header. This is used to add additional size to this bags RectTransform.
        /// </summary>
        [Tooltip("RectTransform for the bag header.")]
        [SerializeField, Group("Sizing")]
        private RectTransform _bagHeader;
        /// <summary>
        /// LayoutGroup for resource entries.
        /// </summary>
        public GridLayoutGroup GridLayoutGroup => _gridLayoutGroup;
        [Tooltip("LayoutGroup for resource entries.")]
        [SerializeField, Group("Sizing")]
        private GridLayoutGroup _gridLayoutGroup;        
        /// <summary>
        /// Text to show bag information.
        /// </summary>
        [Tooltip("Text to show bag information.")]
        [SerializeField, Group("Header")]
        private TextMeshProUGUI _bagTitleText;
        /// <summary>
        /// TooltipHover to show hovered bag information.
        /// </summary>
        [Tooltip("TooltipHover to show hovered bag information.")]
        [SerializeField, Group("Header")]
        private BagEntrytooltipHover _tooltipHover;

        /// <summary>
        /// Content where each resource entry is instantiated.
        /// </summary>
        [Tooltip("Content where each resource entry is instantiated.")]
        [SerializeField, Group("Misc")]
        private Transform _content;
        /// <summary>
        /// Prefab for resource entries.
        /// </summary>
        [Tooltip("Prefab for resource entries.")]
        [SerializeField, Group("Misc")]
        private ResourceEntry _resourceEntryPrefab;
        #endregion

        #region Private.
        /// <summary>
        /// Inventory canvas this entry is for.
        /// </summary>
        private InventoryCanvas _inventoryCanvas;
        /// <summary>
        /// True if RectTransform needs to be resized.
        /// </summary>
        private bool _resizeRequired;
        #endregion

        private void Update()
        {
            Resize();
        }

        /// <summary>
        /// Initializes this script for use.
        /// </summary>
        public void Initialize(InventoryCanvas inventoryCanvas, FloatingTooltipCanvas tooltipCanvas, ActiveBag activeBag)
        {
            //Destroy any content which may have been placed for testing.
            _content.DestroyChildren<ResourceEntry>(false);
            _inventoryCanvas = inventoryCanvas;
            ActiveBag = activeBag;
            _tooltipHover.InitializeOnce(activeBag.Bag, tooltipCanvas);


            int slots = activeBag.Slots.Length;
            //Initialize empty slots. Don't bother pooling since bag slots will rarely change.
            for (int i = 0; i < slots; i++)
            {
                ResourceEntry re = Instantiate(_resourceEntryPrefab, _content);
                IResourceData ird = InstanceFinder.NetworkManager.GetInstance<ResourceManager>().GetIResourceData(activeBag.Slots[i].ResourceId);
                //todo
                /*
                * Add public int BagIndex {get;private} and set
                * when bag is added to inventory. Pass this into
                * init with slot index. */
                ActiveBagResource baggedResource = new ActiveBagResource(ActiveBag.Index, i);
                if (ird != null)
                    re.Initialize(_inventoryCanvas, tooltipCanvas, activeBag.Slots[i], baggedResource);
                else
                    re.Initialize(_inventoryCanvas, tooltipCanvas, baggedResource);

                ResourceEntries.Add(re);
            }

            SetUsedInventorySpaceText();
            QueueResize();
        }

        /// <summary>
        /// Indicates that a resize must occur.
        /// </summary>
        public void QueueResize()
        {
            _resizeRequired = true;
        }

        /// <summary>
        /// Sets text for used bag space.
        /// </summary>
        public void SetUsedInventorySpaceText()
        {
            string name = "Unset";
            int used = 0;
            int max = 0;
            if (ActiveBag != null)
            {
                name = ActiveBag.Bag.name;
                used = ActiveBag.UsedSlots;
                max = ActiveBag.MaximumSlots;
            }

            _bagTitleText.text = $"{name} ({used} / {max})";
        }

        /// <summary>
        /// Resizes transform based on bag slots.
        /// </summary>
        private void Resize()
        {
            if (!_resizeRequired)
                return;
            _resizeRequired = false;

            float headerHeight = _bagHeader.sizeDelta.y;
            float layoutSpacing = GridLayoutGroup.spacing.y;
            int cellCount = _content.childCount;
            float cellSizeY = GridLayoutGroup.cellSize.y;
            int rows = Mathf.CeilToInt((float)cellCount / (float)GridLayoutGroup.constraintCount);
            float result = headerHeight + layoutSpacing + (rows * cellSizeY) + (rows * layoutSpacing);

            _rectTransform.sizeDelta = new Vector2(_rectTransform.sizeDelta.x, result);
        }

    }


}
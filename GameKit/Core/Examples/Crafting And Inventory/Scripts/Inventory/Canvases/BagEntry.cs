using GameKit.Inventories;
using GameKit.Resources;
using GameKit.Resources.Managers;
using FishNet;
using System.Collections.Generic;
using TMPro;
using UnityEngine;
using UnityEngine.UI;
using TriInspector;
using GameKit.Utilities;
using GameKit.Examples.FloatingContainers.Tooltips;

namespace GameKit.Examples.Inventories.Canvases
{

    [DeclareFoldoutGroup("Sizing")]
    [DeclareFoldoutGroup("Header")]
    [DeclareFoldoutGroup("Misc")]
    public class BagEntry : MonoBehaviour
    {
        #region Public.
        /// <summary>
        /// Bag this entry is for.
        /// </summary>
        public Bag Bag { get; private set; }
        /// <summary>
        /// ResourceEntrys within Bag.
        /// </summary>
        public List<ResourceEntry> ResourceEntries { get; private set; } = new();
        #endregion

        #region Serialized.
        /// <summary>
        /// RectTransform for this script. Used to resize based on number of resource entries and bag size.
        /// </summary>
        [PropertyTooltip("RectTransform for this script. Used to resize based on number of resource entries and bag size.")]
        [SerializeField, Group("Sizing")]
        private RectTransform _rectTransform;
        /// <summary>
        /// RectTransform for the bag header. This is used to add additional size to this bags RectTransform.
        /// </summary>
        [PropertyTooltip("RectTransform for the bag header.")]
        [SerializeField, Group("Sizing")]
        private RectTransform _bagHeader;
        /// <summary>
        /// LayoutGroup for resource entries.
        /// </summary>
        public GridLayoutGroup GridLayoutGroup => _gridLayoutGroup;
        [PropertyTooltip("LayoutGroup for resource entries.")]
        [SerializeField, Group("Sizing")]
        private GridLayoutGroup _gridLayoutGroup;        
        /// <summary>
        /// Text to show bag information.
        /// </summary>
        [PropertyTooltip("Text to show bag information.")]
        [SerializeField, Group("Header")]
        private TextMeshProUGUI _bagTitleText;

        /// <summary>
        /// Content where each resource entry is instantiated.
        /// </summary>
        [PropertyTooltip("Content where each resource entry is instantiated.")]
        [SerializeField, Group("Misc")]
        private Transform _content;
        /// <summary>
        /// Prefab for resource entries.
        /// </summary>
        [PropertyTooltip("Prefab for resource entries.")]
        [SerializeField, Group("Misc")]
        private ResourceEntry _resourceEntryPrefab;
        #endregion

        #region Private.
        /// <summary>
        /// Bag this entry is for.
        /// </summary>
        private Bag _bag;
        /// <summary>
        /// Inventory canvas this entry is for.
        /// </summary>
        private InventoryCanvas _inventoryCanvas;
        /// <summary>
        /// TooltipCanvas to use.
        /// </summary>
        private FloatingTooltipCanvas _tooltipCanvas;
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
        public void Initialize(InventoryCanvas inventoryCanvas, FloatingTooltipCanvas tooltipCanvas, Bag bag)
        {
            //Destroy any content which may have been placed for testing.
            _content.DestroyChildren<ResourceEntry>(false);

            _inventoryCanvas = inventoryCanvas;
            _tooltipCanvas = tooltipCanvas;
            _bag = bag;

            int slots = bag.Slots.Length;
            //Initialize empty slots. Don't bother pooling since bag slots will rarely change.
            for (int i = 0; i < slots; i++)
            {
                ResourceEntry re = Instantiate(_resourceEntryPrefab, _content);
                IResourceData ird = InstanceFinder.NetworkManager.GetInstance<ResourceManager>().GetIResourceData(bag.Slots[i].ResourceId);
                //todo
                /*
                * Add public int BagIndex {get;private} and set
                * when bag is added to inventory. Pass this into
                * init with slot index. */
                BaggedResource baggedResource = new BaggedResource(_bag.Index, i);
                if (ird != null)
                    re.Initialize(_inventoryCanvas, tooltipCanvas, bag.Slots[i], baggedResource);
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
            int used = 0;
            int max = 0;
            if (_bag != null)
            {
                used = _bag.UsedSlots;
                max = _bag.MaximumSlots;
            }

            _bagTitleText.text = $"Bag {used} / {max}";
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
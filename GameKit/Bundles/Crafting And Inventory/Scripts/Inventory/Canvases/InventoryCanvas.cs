using UnityEngine;
using System.Collections.Generic;
using TMPro;
using System.Runtime.CompilerServices;
using System;
using UnityEngine.UI;
using GameKit.Dependencies.Utilities;
using GameKit.Bundles.FloatingContainers.Tooltips;
using GameKit.Dependencies.Inspectors;
using GameKit.Core.Inventories;
using GameKit.Core.Resources;
using GameKit.Bundles.CraftingAndInventories.Resources;
using GameKit.Bundles.Dependencies;

namespace GameKit.Bundles.CraftingAndInventories.Inventories.Canvases
{

    public class InventoryCanvas : MonoBehaviour
    {
        #region Serialized.
        /// <summary>
        /// TextMeshPro to show which category is selected.
        /// </summary>
        [Tooltip("TextMeshPro to show which category is selected.")]
        [SerializeField, Group("Header")]
        private TextMeshProUGUI _categoryText;
        /// <summary>
        /// Input used to search the current category.
        /// </summary>
        [Tooltip("Input used to search the current category.")]
        [SerializeField, Group("Header")]
        private TMP_InputField _searchInput;

        /// <summary>
        /// ScrollRect to disable when dragging entries.
        /// </summary>
        [Tooltip("ScrollRect to disable when dragging entries.")]
        [SerializeField, Group("Collection")]
        private ScrollRect _scrollRect;
        /// <summary>
        /// Prefab to use for resource entries.
        /// </summary>
        [Tooltip("Prefab to use for resource entries.")]
        [SerializeField, Group("Collection")]
        private BagEntry _bagEntryPrefab;
        /// <summary>
        /// Transform to place instantiated bags.
        /// </summary>
        [Tooltip("Transform to place instantiated bags.")]
        [SerializeField, Group("Collection")]
        private Transform _bagContent;
        /// <summary>
        /// FloatingImage prefab to use to show moving of item entries.
        /// </summary>
        [Tooltip("FloatingImage prefab to use to show moving of item entries.")]
        [SerializeField, Group("Collection")]
        private FloatingResourceEntry _floatingInventoryItemPrefab;

        /// <summary>
        /// Text to show amount of space used in the inventory.
        /// </summary>
        [Tooltip("Text to show amount of space used in the inventory.")]
        [SerializeField, Group("Footer")]
        private TextMeshProUGUI _inventorySpaceText;
        #endregion

        #region Private.
        /// <summary>
        /// Entries for resources.
        /// </summary>
        private List<BagEntry> _bagEntries = new List<BagEntry>();
        /// <summary>
        /// True if subscribed to events.
        /// </summary>
        private bool _subscribed;
        /// <summary>
        /// True if this canvas is visible.
        /// </summary>
        private bool _visible;
        /// <summary>
        /// True if collection should be updated when shown.
        /// </summary>
        private bool _updateOnShow;
        /// <summary>
        /// Next time a search may occur. When -1f no search is queued.
        /// </summary>
        private float _nextSearchUnscaledTime;
        /// <summary>
        /// Inventory being used.
        /// </summary>
        private Inventory _inventory;
        /// <summary>
        /// TooltipCanvas to use.
        /// </summary>
        private FloatingTooltipCanvas _tooltipCanvas;
        /// <summary>
        /// Entry currently being held.
        /// </summary>
        private ResourceEntry _heldEntry;
        /// <summary>
        /// Last entry to be hovered over.
        /// </summary>
        private ResourceEntry _hoveredEntry;
        /// <summary>
        /// Currently instantiated floating inventory item.
        /// </summary>
        private FloatingResourceEntry _floatingInventoryItem;
        #endregion

        #region Const.
        /// <summary>
        /// How often searches may occur.
        /// </summary>
        private float SEARCH_INTERVAL = 0.15f;
        #endregion

        private void Awake()
        {
            _floatingInventoryItem = Instantiate(_floatingInventoryItemPrefab);
            _floatingInventoryItem.Hide();
            //Attach floating to this canvas so the rect transforms on it works.
            _floatingInventoryItem.transform.SetParentAndKeepTransform(transform);

            //Destroy content children. There may be some present from testing.
            _bagContent.DestroyChildren<BagEntry>(true);
            _searchInput.onValueChanged.AddListener(_searchInput_OnValueChanged);

            ClientInstance.OnClientChangeInvoke(new ClientInstance.ClientChangeDel(ClientInstance_OnClientChange));
        }

        private void Start()
        {
            Show();
        }

        private void OnDestroy()
        {
            ChangeSubscription(ClientInstance.Instance, false);
            ClientInstance.OnClientChange -= ClientInstance_OnClientChange;
            _searchInput.onValueChanged.AddListener(_searchInput_OnValueChanged);
        }

        private void Update()
        {
            TrySearch();
            MoveFloatingInventoryItem();
        }

        /// <summary>
        /// Moves the current floating inventory item to mouse position.
        /// </summary>
        private void MoveFloatingInventoryItem()
        {
            if (_floatingInventoryItem.IsHiding)
                return;

            _floatingInventoryItem.UpdatePosition(Input.mousePosition);
        }

        /// <summary>
        /// Changes subscription status to events.
        /// </summary>
        private void ChangeSubscription(ClientInstance ci, bool subscribe)
        {
            if (subscribe == _subscribed)
                return;
            _subscribed = subscribe;
            if (ci == null)
                return;

            if (subscribe)
                ci.Inventory.OnBagSlotUpdated += Inventory_OnBagSlotUpdated;
            else
                ci.Inventory.OnBagSlotUpdated -= Inventory_OnBagSlotUpdated;
        }

        /// <summary>
        /// Tries to search using current search inputs value.
        /// </summary>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        private void TrySearch()
        {
            //No search or cannot yet.
            if (_nextSearchUnscaledTime == -1f || (Time.unscaledTime < _nextSearchUnscaledTime))
                return;
            //Unset.
            _nextSearchUnscaledTime = -1f;

            string value = _searchInput.text;

            foreach (BagEntry be in _bagEntries)
            {
                foreach (ResourceEntry re in be.ResourceEntries)
                    UpdateSearch(re, value);
            }
        }

        /// <summary>
        /// Updates search result for a ResourceEntry.
        /// </summary>
        /// <param name="re">ResourceEntry to update.</param>
        /// <param name="value">String to search.</param>
        private void UpdateSearch(ResourceEntry re, string value)
        {
            //If nothing to search then just make sure all entries are enabled.
            if (string.IsNullOrWhiteSpace(value))
            {
                re.SetSelectable(true);
            }
            //Something to search for.
            else
            {
                //Default.
                bool contains = false;
                if (re.IResourceData != null)
                {
                    ResourceData rd = (ResourceData)re.IResourceData;
                    contains = rd.GetDisplayName().Contains(value, System.StringComparison.OrdinalIgnoreCase);
                }

                re.SetSelectable(contains);
            }
        }

        /// <summary>
        /// Called when the search field changes.
        /// </summary>
        /// <param name="value"></param>
        private void _searchInput_OnValueChanged(string value)
        {
            //Set when next search can occur. Throttle searches for perf.
            if (_nextSearchUnscaledTime == -1f)
                _nextSearchUnscaledTime = Time.unscaledTime + SEARCH_INTERVAL;
        }

        /// <summary>
        /// Called when inventory space is updated.
        /// </summary>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        private void Inventory_OnBagSlotUpdated(int bagIndex, int slotIndex, ResourceQuantity rq)
        {

            ResourceEntry re = _bagEntries[bagIndex].ResourceEntries[slotIndex];

            /* If the new resource is not the same as existing then
             * try to hide the tooltip using existing reference. If the
             * modified bag is the one showing the tooltip then the tooltip
             * will reset, which we want due to item change. 
             * Keep in mind that calling hide from the object which
             * did not call Show will result in the tooltip ignoring
             * the command. */
            if (re.IResourceData != null && re.IResourceData.GetResourceId() != rq.ResourceId)
                _tooltipCanvas.Hide(re);

            re.Initialize(this, _tooltipCanvas, rq, new BaggedResource(bagIndex, slotIndex));
            SetUsedInventorySpaceText();
            _bagEntries[bagIndex].SetUsedInventorySpaceText();
            UpdateSearch(re, _searchInput.text);
        }

        /// <summary>
        /// Sets text for used inventory space.
        /// </summary>
        private void SetUsedInventorySpaceText()
        {
            int used = 0;
            int max = 0;
            if (_inventory != null)
            {
                used = _inventory.UsedSlots;
                max = _inventory.MaximumSlots;
            }

            _inventorySpaceText.text = $"Used {used} / {max} Space";
        }

        /// <summary>
        /// Called when OnStartClient occurs on this local clients ClientInstance.
        /// </summary>
        private void ClientInstance_OnClientChange(ClientInstance instance, ClientInstanceState state)
        {
            if (state.IsPreState())
                return;

            bool started = (state == ClientInstanceState.PostInitialize);
            ChangeSubscription(instance, started);
            if (started)
                InitializeBags();
        }

        /// <summary>
        /// Shows this canvas.
        /// </summary>
        public void Show()
        {
            _visible = true;

            if (_updateOnShow)
            {
                InitializeBags();
                //Force an immediate search as well.
                _nextSearchUnscaledTime = 0f;
            }
            _updateOnShow = false;
        }

        /// <summary>
        /// Hides this canvas.
        /// </summary>
        public void Hide()
        {
            _visible = false;
            _heldEntry = null;
            _hoveredEntry = null;
            _scrollRect.enabled = true;
        }

        /// <summary>
        /// Selects a resource entry.
        /// </summary>
        public void SelectResourceEntry(ResourceEntry re)
        {
            throw new NotImplementedException();
        }

        /// <summary>
        /// Initializes bags with data from inventory.
        /// </summary>
        public void InitializeBags()
        {
            if (!_visible)
            {
                _updateOnShow = true;
                return;
            }

            //Cannot update without the inventory.
            _inventory = ClientInstance.Instance?.Inventory;
            if (_inventory == null)
                return;

            _bagContent.DestroyChildren<BagEntry>(true);
            _bagEntries.Clear();

            foreach (Bag b in _inventory.Bags)
            {
                BagEntry be = Instantiate(_bagEntryPrefab, _bagContent);
                be.Initialize(this, _tooltipCanvas, b);
                _bagEntries.Add(be);
            }

            SetUsedInventorySpaceText();
        }

        /// <summary>
        /// Called when a bag entry is pressed.
        /// </summary>
        /// <param name="entry">Entry being held.</param>
        public void OnHeld_ResourceEntry(ResourceEntry entry)
        {
            if (entry.ResourceData == null)
                return;

            TryInitializeFloatingInventoryItem();
            _heldEntry = entry;
            _scrollRect.enabled = false;

            /* Tries to initialize the floating inventory item
             * if it has not already been done so. */
            void TryInitializeFloatingInventoryItem()
            {
                if (_floatingInventoryItem.IsHiding)
                {
                    _floatingInventoryItem.Initialize(entry.ResourceData.GetIcon(), _bagEntryPrefab.GridLayoutGroup.cellSize, entry.StackCount);
                    _floatingInventoryItem.Show(entry.transform);
                    entry.CanvasGroup.SetActive(false, true);
                }
            }
        }


        /// <summary>
        /// Called when a bag entry is no longer held.
        /// </summary>
        /// <param name="entry">The entry which the pointer was released over.</param>
        public void OnRelease_ResourceEntry(ResourceEntry entry)
        {
            /* Tell inventory to try and move, swap, or
             * stack items. Inventory performs error checking
             * so no need to here. */
            if (_heldEntry != null && _hoveredEntry != null)
                _inventory.MoveResource(_heldEntry.BagSlot, _hoveredEntry.BagSlot);

            _floatingInventoryItem.Hide();

            _heldEntry?.CanvasGroup.SetActive(true, true);
            _heldEntry = null;
            _scrollRect.enabled = true;
        }

        /// <summary>
        /// Called when a resource is hovered over.
        /// </summary>
        public void OnEnter_ResourceEntry(ResourceEntry entry)
        {
            _hoveredEntry = entry;
        }

        /// <summary>
        /// Called when a resource is hovered over.
        /// </summary>
        public void OnExit_ResourceEntry(ResourceEntry entry)
        {
            _hoveredEntry = null;
        }
    }


}
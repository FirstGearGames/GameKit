using UnityEngine;
using System.Collections.Generic;
using TMPro;
using System.Runtime.CompilerServices;
using GameKit.Resources;
using FishNet;
using GameKit.Inventories;
using GameKit.Examples.Managers;
using FirstGearGames.Utilities.Objects;
using TriInspector;
using System;
using GameKit.Examples.Resources;
using GameKit.Examples.Tooltips.Canvases;

namespace GameKit.Examples.Inventories.Canvases
{

    [DeclareFoldoutGroup("Header")]
    [DeclareFoldoutGroup("Collection")]
    [DeclareFoldoutGroup("Footer")]
    public class InventoryCanvas : MonoBehaviour
    {
        #region Types.
        private class BaggedResource
        {
            /// <summary>
            /// Bag the resource is in.
            /// </summary>
            public readonly Bag Bag;
            /// <summary>
            /// Index of resource in the bag.
            /// </summary>
            public readonly int Index;
            /// <summary>
            /// ResourceEntry for this bagged resource.
            /// </summary>
            public ResourceEntry Entry;

            public BaggedResource(Bag bag, int index, ResourceEntry entry)
            {
                Bag = bag;
                Index = index;
                Entry = entry;
            }
        }
        #endregion

        #region Serialized.
        /// <summary>
        /// TextMeshPro to show which category is selected.
        /// </summary>
        [PropertyTooltip("TextMeshPro to show which category is selected.")]
        [SerializeField, Group("Header")]
        private TextMeshProUGUI _categoryText;
        /// <summary>
        /// Input used to search the current category.
        /// </summary>
        [PropertyTooltip("Input used to search the current category.")]
        [SerializeField, Group("Header")]
        private TMP_InputField _searchInput;

        /// <summary>
        /// Prefab to use for resource entries.
        /// </summary>
        [PropertyTooltip("Prefab to use for resource entries.")]
        [SerializeField, Group("Collection")]
        private BagEntry _bagEntryPrefab;
        /// <summary>
        /// Transform to place instantiated bags.
        /// </summary>
        [PropertyTooltip("Transform to place instantiated bags.")]
        [SerializeField, Group("Collection")]
        private Transform _bagContent;

        /// <summary>
        /// Text to show amount of space used in the inventory.
        /// </summary>
        [PropertyTooltip("Text to show amount of space used in the inventory.")]
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
        private TooltipCanvas _tooltipCanvas;
        #endregion

        #region Const.
        /// <summary>
        /// How often searches may occur.
        /// </summary>
        private float SEARCH_INTERVAL = 0.15f;
        #endregion

        private void Start()
        {
            InitializeOnce();
            Show();
        }

        private void Update()
        {
            TrySearch();
        }

        private void OnDestroy()
        {
            ChangeSubscription(ClientInstance.Instance, false);
            ClientInstance.OnClientChange -= ClientInstance_OnClientStarted;
            _searchInput.onValueChanged.AddListener(_searchInput_OnValueChanged);
        }

        private void InitializeOnce()
        {
            CanvasManager cm = InstanceFinder.NetworkManager.GetInstance<CanvasManager>();
            if (cm != null)
            {
                cm.InventoryCanvas = this;
                _tooltipCanvas = cm.TooltipCanvas;
            }

            //Destroy content children. There may be some present from testing.
            _bagContent.DestroyChildren<BagEntry>(true);

            ClientInstance selfCi = ClientInstance.Instance;
            //If client instance exist then get inventory, otherwise listen for instantiation.
            if (selfCi != null)
                ClientInstance_OnClientStarted(selfCi, true);

            //Listen for changes to the client instance.
            ClientInstance.OnClientChange += ClientInstance_OnClientStarted;
            _searchInput.onValueChanged.AddListener(_searchInput_OnValueChanged);
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
            re.Initialize(this, _tooltipCanvas, rq);
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
        private void ClientInstance_OnClientStarted(ClientInstance instance, bool started)
        {
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

    }


}
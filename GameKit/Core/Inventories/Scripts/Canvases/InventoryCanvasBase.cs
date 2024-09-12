using UnityEngine;
using System.Collections.Generic;
using TMPro;
using System.Runtime.CompilerServices;
using UnityEngine.UI;
using GameKit.Dependencies.Utilities;
using GameKit.Core.FloatingContainers.Tooltips;
using GameKit.Core.Resources;
using GameKit.Core.Dependencies;
using GameKit.Core.FloatingContainers.OptionMenuButtons;
using GameKit.Core.Inventories.Bags;
using GameKit.Core.Utilities;
using GameKit.Dependencies.Utilities.Types;
using Sirenix.OdinInspector;

namespace GameKit.Core.Inventories.Canvases
{
    public class InventoryCanvasBase : MonoBehaviour
    {
        #region Types.
        /// <summary>
        /// Reason a resource is being held.
        /// </summary>
        protected enum HeldResourceReason
        {
            Unset = 0,
            Move = 1,
            Split = 2,
        }
        #endregion

        #region Serialized.
        /// <summary>
        /// TextMeshPro to show which category is selected.
        /// </summary>
        [Tooltip("TextMeshPro to show which category is selected.")]
        [SerializeField, BoxGroup("Header")]
        private TextMeshProUGUI _categoryText;
        /// <summary>
        /// Input used to search the current category.
        /// </summary>
        [Tooltip("Input used to search the current category.")]
        [SerializeField, BoxGroup("Header")]
        private TMP_InputField _searchInput;

        /// <summary>
        /// ScrollRect to disable when dragging entries.
        /// </summary>
        [Tooltip("ScrollRect to disable when dragging entries.")]
        [SerializeField, BoxGroup("Collection")]
        protected ScrollRect ScrollRect;
        /// <summary>
        /// Prefab to use for resource entries.
        /// </summary>
        [Tooltip("Prefab to use for resource entries.")]
        [SerializeField, BoxGroup("Collection")]
        private BagEntry _bagEntryPrefab;
        /// <summary>
        /// Transform to place instantiated bags.
        /// </summary>
        [Tooltip("Transform to place instantiated bags.")]
        [SerializeField, BoxGroup("Collection")]
        private Transform _bagContent;
        /// <summary>
        /// FloatingImage prefab to use to show moving of item entries.
        /// </summary>
        [Tooltip("FloatingImage prefab to use to show moving of item entries.")]
        [SerializeField, BoxGroup("Collection")]
        private FloatingResourceEntry _floatingInventoryItemPrefab;

        /// <summary>
        /// Text to show amount of space used in the inventory.
        /// </summary>
        [Tooltip("Text to show amount of space used in the inventory.")]
        [SerializeField, BoxGroup("Footer")]
        private TextMeshProUGUI _inventorySpaceText;
        #endregion

        #region Protected.
        /// <summary>
        /// Inventory being used.
        /// </summary>
        protected InventoryBase Inventory;
        /// <summary>
        /// TooltipCanvas to use.
        /// </summary>
        protected FloatingTooltipCanvas TooltipCanvas;
        /// <summary>
        /// ClientInstance for the local client.
        /// </summary>
        protected ClientInstance ClientInstance;
        #endregion

        #region Private.
        /// <summary>
        /// Canvas used to show stack splitting options.
        /// </summary>
        private SplittingCanvas _splittingCanvas;
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
        /// Entry currently being held.
        /// </summary>
        private ResourceEntry _heldEntry;
        /// <summary>
        /// Last entry to be hovered over.
        /// </summary>
        private ResourceEntry _hoveredEntry;
        /// <summary>
        /// How many resources to move within an inventory.
        /// </summary>
        /// <remarks>When value is unset all quantities should be moved.</remarks>
        protected int EntryMoveQuantity = UNSET_ENTRY_MOVE_QUANTITY;
        /// <summary>
        /// Currently instantiated floating inventory item.
        /// </summary>
        private FloatingResourceEntry _floatingResourceEntry;
        /// <summary>
        /// Reason for heldResource value.
        /// </summary>
        private HeldResourceReason _heldResourceReason;
        #endregion

        #region Const.
        /// <summary>
        /// How often searches may occur.
        /// </summary>
        private float SEARCH_INTERVAL = 0.15f;
        /// <summary>
        /// Indicates that the amount to move is unset.
        /// </summary>
        public const int UNSET_ENTRY_MOVE_QUANTITY = -1;
        #endregion

        private void Awake()
        {
            _floatingResourceEntry = Instantiate(_floatingInventoryItemPrefab);
            _floatingResourceEntry.Hide();
            //Attach floating to this canvas so the rect transforms on it works.
            _floatingResourceEntry.transform.SetParentAndKeepTransform(transform);

            //Destroy content children. There may be some present from testing.
            _bagContent.DestroyChildren<BagEntry>(true);
            _searchInput.onValueChanged.AddListener(_searchInput_OnValueChanged);

            ClientInstance.OnClientInstanceChangeInvoke(new ClientInstance.ClientInstanceChangeDel(ClientInstance_OnClientInstanceChange), false);
        }

        private void Start()
        {
            Show();
        }

        private void OnDestroy()
        {
            ChangeSubscription(false);
            ClientInstance.OnClientInstanceChange -= ClientInstance_OnClientInstanceChange;
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
            if (_floatingResourceEntry.IsHiding)
                return;

            _floatingResourceEntry.UpdatePosition(Input.mousePosition);
        }

        /// <summary>
        /// Changes subscription status to events.
        /// </summary>
        protected void ChangeSubscription(bool subscribe)
        {
            if (subscribe == _subscribed)
                return;
            _subscribed = subscribe;

            if (Inventory == null)
                return;

            if (subscribe)
            {
                Inventory.OnBagsChanged += Inventory_OnBagsChanged;
                Inventory.OnBagSlotUpdated += Inventory_OnBagSlotUpdated;
            }
            else
            {
                Inventory.OnBagsChanged -= Inventory_OnBagsChanged;
                Inventory.OnBagSlotUpdated -= Inventory_OnBagSlotUpdated;
            }
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
                if (re.ResourceData != null)
                    contains = re.ResourceData.DisplayName.Contains(value, System.StringComparison.OrdinalIgnoreCase);

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
        /// Called when inventory bags are updated.
        /// </summary>
        private void Inventory_OnBagsChanged(bool added, ActiveBag bag)
        {
            InitializeBags();
        }

        /// <summary>
        /// Called when inventory space is updated.
        /// </summary>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        private void Inventory_OnBagSlotUpdated(ActiveBag activeBag, int slotIndex, SerializableResourceQuantity srq)
        {
            if (UpdateOnShow())
                return;

            ResourceEntry re = _bagEntries[activeBag.LayoutIndex].ResourceEntries[slotIndex];

            /* If the new resource is not the same as existing then
             * try to hide the tooltip using existing reference. If the
             * modified bag is the one showing the tooltip then the tooltip
             * will reset, which we want due to item change.
             * Keep in mind that calling hide from the object which
             * did not call Show will result in the tooltip ignoring
             * the command. */
            if (re.ResourceData != null && re.ResourceData.UniqueId != srq.UniqueId)
                TooltipCanvas.Hide(re);

            re.Initialize(ClientInstance.Instance, this, TooltipCanvas, srq, new BagSlot(activeBag, slotIndex));

            SetUsedInventorySpaceText();
            _bagEntries[activeBag.LayoutIndex].SetUsedInventorySpaceText();
            UpdateSearch(re, _searchInput.text);
        }

        /// <summary>
        /// Will queue an update the next time this canvas is shown if currently hidden.
        /// </summary>
        /// <returns>True if an update was queued.</returns>
        private bool UpdateOnShow()
        {
            if (!_visible)
                _updateOnShow = true;

            return !_visible;
        }

        /// <summary>
        /// Sets text for used inventory space.
        /// </summary>
        private void SetUsedInventorySpaceText()
        {
            int used = 0;
            int max = 0;
            if (Inventory != null)
            {
                used = Inventory.UsedSlots;
                max = Inventory.MaximumSlots;
            }

            _inventorySpaceText.text = $"Used {used} / {max} Space";
        }

        /// <summary>
        /// Called when OnStartClient occurs on this local clients ClientInstance.
        /// </summary>
        protected virtual void ClientInstance_OnClientInstanceChange(ClientInstance instance, ClientInstanceState state, bool asServer)
        {
            if (asServer)
                return;

            if (state.IsPreState())
            {
                ClientInstance = instance;
                instance.NetworkManager.RegisterInstance(this);
                return;
            }

            bool started = (state == ClientInstanceState.PostInitialize);
            //If started then get the character inventory and initialize bags.
            if (started)
            {
                TooltipCanvas = instance.NetworkManager.GetInstance<FloatingTooltipCanvas>();
                _splittingCanvas = instance.NetworkManager.GetInstance<SplittingCanvas>();
            }
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
            ScrollRect.enabled = true;
        }

        /// <summary>
        /// Initializes bags with data from inventory.
        /// </summary>
        /// <param name="force">True to update immediately even if this canvas is not shown.</param>
        public void InitializeBags()
        {
            if (!_visible)
            {
                _updateOnShow = true;
                return;
            }

            if (Inventory == null)
                return;

            _bagContent.DestroyChildren<BagEntry>(true);
            _bagEntries.Clear();

            foreach (ActiveBag b in Inventory.ActiveBags.Values)
            {
                BagEntry be = Instantiate(_bagEntryPrefab, _bagContent);
                be.Initialize(this, ClientInstance, TooltipCanvas, b);
                _bagEntries.Add(be);
            }

            SetUsedInventorySpaceText();
        }

        /// <summary>
        /// Called when a bag entry is pressed.
        /// </summary>
        /// <param name="entry">Entry being held.</param>
        public virtual void OnPressed_ResourceEntry(ResourceEntry entry)
        {
            Debug.LogError($"make it so split move shows count if split were to succeed. This behavior only happens when a split is confirmed. The update should be temporary until the move is canceled. If full stack is moved then hide the hide the item per usual. this all might be possible to do in the BeginMoveEntry(entry, quantity)");
            /* Pressed callbacks can be for a variety of things.
             * - Creating a hovering icon to move an entire stack.
             *      This happens when an entry with a quantity > 0 is pressed
             *      and split key is not held.
             * - Begin a split to prompt split stack canvas.
             *      This happens when an entry with a quantity > 1 is pressed
             *      and split key is held.
             * - Complete split movement.
             *      This happens when split canvas was already shown and
             *      the resource is attempted ot be moved onto another stack. */

            bool pressedHasData = (entry.StackCount > 0);
            //If entry has data and there is no current held reason, check to set one.
            if (pressedHasData && _heldResourceReason == HeldResourceReason.Unset)
            {
                //Show split.
                if (Keybinds.IsShiftHeld)
                {
                    //Splitting requires at least 2 quantity.
                    if (entry.StackCount > 1)
                    {
                        /* Do not set to split here. The resource is not considered held until
                         * the user completes the split canvas. */
                        SplittingCanvasConfig config = new()
                        {
                            CanceledCallback = new(OnSplitCanceled),
                            ConfirmedCallback = new(OnSplitConfirmed),
                            ResourceEntry = entry,
                            SplitValues = new IntRange(1, entry.StackCount),
                        };

                        _splittingCanvas.Show(entry.transform, config);
                    }
                }
                //Split key is not held, move entire stack.
                else
                {
                    _heldResourceReason = HeldResourceReason.Move;
                    BeginMoveResource(entry);
                }
            }
            //Split held reason.
            else if (_heldResourceReason == HeldResourceReason.Split)
            {
                OnRelease_ResourceEntry(_heldEntry);
            }
            //Do not need to check Move as the action completes in OnRelease.
        }

        /// <summary>
        /// Called when a bag entry is no longer held.
        /// </summary>
        /// <param name="entry">The entry which the pointer was released over.</param>
        public void OnRelease_ResourceEntry(ResourceEntry entry)
        {
            EndMoveResource();
        }

        /// <summary>
        /// Ends moving of a resource.
        /// </summary>
        private void EndMoveResource()
        {
            //There is no action to take when theres no held reason.
            if (_heldResourceReason == HeldResourceReason.Unset)
                return;

            /* Tell inventory to try and move, swap, or
             * stack items. Inventory performs error checking
             * so no need to here. */
            if (_heldEntry != null && _hoveredEntry != null)
                Inventory.MoveResource(_heldEntry.BagSlot, _hoveredEntry.BagSlot, EntryMoveQuantity);

            _floatingResourceEntry.Hide();

            if (_heldEntry != null)
                _heldEntry.CanvasGroup.SetActive(true, true);
            _heldEntry = null;
            ScrollRect.enabled = true;

            _heldResourceReason = HeldResourceReason.Unset;
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

        /// <summary>
        /// Shows a moving resource canvas with the full quantity of entry.
        /// </summary>
        protected virtual void BeginMoveResource(ResourceEntry entry)
        {
            BeginMoveResource(entry, entry.StackCount);
            entry.CanvasGroup.SetActive(false, true);
        }

        /// <summary>
        /// Shows a moving resource canvas with a specified quantity of entry.
        /// </summary>
        /// <remarks>>This is typically used to split resources.</remarks>
        protected virtual void BeginMoveResource(ResourceEntry entry, int quantity)
        {
            EntryMoveQuantity = quantity;
            InitializeFloatingInventoryItem(entry, quantity);
            _heldEntry = entry;
            ScrollRect.enabled = false;
        }

        /// <summary>
        /// Initializes a floating item canvas for an entry.
        /// </summary>
        /// <param name="entry"></param>
        protected virtual void InitializeFloatingInventoryItem(ResourceEntry entry, int moveQuantity)
        {
            _floatingResourceEntry.Initialize(entry.ResourceData.Icon, _bagEntryPrefab.GridLayoutGroup.cellSize, moveQuantity);
            _floatingResourceEntry.Show(entry.transform);
        }

        /// <summary>
        /// Called when cancel is pressed on an item split.
        /// </summary>
        private void OnSplitCanceled()
        {
            EntryMoveQuantity = UNSET_ENTRY_MOVE_QUANTITY;
            _heldEntry = null;
        }

        /// <summary>
        /// Called when confirmed is pressed on an item split.
        /// </summary>
        private void OnSplitConfirmed(ResourceEntry resourceEntry, int moveCount)
        {
            //Splitter starts minimum at 1 so this shouldn't be possible.
            if (moveCount == 0)
            {
                OnSplitCanceled();
            }
            //Partial quantity move.
            else
            {
                _heldResourceReason = HeldResourceReason.Split;
                _heldEntry = resourceEntry;
                BeginMoveResource(resourceEntry, moveCount);
            }
        }
    }
}
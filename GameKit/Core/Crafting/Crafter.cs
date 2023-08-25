using FishNet.Object;
using System.Runtime.CompilerServices;
using UnityEngine;
using GameKit.Dependencies.Utilities.Types;
using GameKit.Core.Inventories;
using GameKit.Core.Resources;

namespace GameKit.Core.Crafting
{

    public partial class Crafter : NetworkBehaviour
    {
        #region Types.
        /// <summary>
        /// Information about a recipe being crafted.
        /// </summary>
        private class CraftingProgress
        {
            /// <summary>
            /// True if crafting is in progress.
            /// </summary>
            public bool Active => (Recipe != null);
            /// <summary>
            /// Percentage complete.
            /// </summary>
            public float Percent
            {
                get
                {
                    if (Recipe == null)
                        return 0f;

                    return Mathf.Min(1f, _timePassed / Recipe.GetCraftTime());
                }
            }

            /// <summary>
            /// Craft time of the recipe after having SpeedMultiplier applied.
            /// </summary>
            public float MultipliedCraftTime => ((1f / _speedMultiplier) * Recipe.GetCraftTime());
            /// <summary>
            /// Recipe being crafted.
            /// </summary>
            public IRecipe Recipe;
            /// <summary>
            /// Time passed since crafting this recipe started.
            /// </summary>
            private float _timePassed;
            /// <summary>
            /// Multiplier applied to every time passed update.
            /// </summary>
            private float _speedMultiplier;

            public void Reset()
            {
                Recipe = null;
            }

            /// <summary>
            /// Initializes new crafting.
            /// </summary>
            /// <param name="r"></param>
            /// <param name="timeRemaining"></param>
            public void Initialize(IRecipe r, float multiplier)
            {
                Recipe = r;
                _timePassed = 0f;
                _speedMultiplier = multiplier;
            }

            /// <summary>
            /// Subtract time from TimeRemaining.
            /// </summary>
            public void AddTimePassed(float delta)
            {
                delta *= _speedMultiplier;
                _timePassed += delta;
            }
        }
        #endregion

        #region Public.
        /// <summary>
        /// Called when crafting starts.
        /// </summary>
        public event CraftingStartedDel OnCraftingStarted;
        /// <summary>
        /// sfdsd
        /// </summary>
        /// <param name="recipe"></param>
        /// <param name="asServer">zzdfgdgd</param>
        public delegate void CraftingStartedDel(IRecipe recipe, bool asServer);
        /// <summary>
        /// Called when crafting progresses.
        /// </summary>
        public event CraftingProgressedDel OnCraftingProgressed;
        public delegate void CraftingProgressedDel(IRecipe recipe, float percent, float delta);
        /// <summary>
        /// Called when a crafting result occurs. This could be completed, failed, ect.
        /// </summary>
        public event CraftingResultDel OnCraftingResult;
        public delegate void CraftingResultDel(IRecipe recipe, CraftingResult result, bool asServer);
        #endregion

        #region Private.
        /// <summary>
        /// CraftingManager to use.
        /// </summary>
        private CraftingManager _craftingManager;
        /// <summary>
        /// ResourceManager to use.
        /// </summary>
        private ResourceManager _resourceManager;
        /// <summary>
        /// Inventory for this client.
        /// </summary>
        private Inventory _inventory;
        /// <summary>
        /// Multiplier gradually applied to crafting speed when crafting many objects at once.
        /// </summary>
        private FloatRange _sequentialSpeedMultiplier = new FloatRange(1f, 10f);
        /// <summary>
        /// The last recipe which crafting had completed.
        /// </summary>
        private IRecipe _lastCraftedRecipe;
        /// <summary>
        /// Last unscaled time a completed recipe was sent to the client.
        /// </summary>
        private float _lastCompletedCraftTime;
        #endregion

        #region Const.
        /// <summary>
        /// How many resources in a row must be crafted to achieve the highest speed multiplier.
        /// </summary>
        private const int MAXIMUM_SEQUENTIAL_REQUIREMENT = 10;
        #endregion
        private void Awake()
        {
            _inventory = GetComponent<Inventory>();
        }

        public override void OnStartNetwork()
        {
            base.OnStartNetwork();
            _resourceManager = base.NetworkManager.GetInstance<ResourceManager>();
            _craftingManager = base.NetworkManager.GetInstance<CraftingManager>();
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        private void Update()
        {
            ProgressCrafting();
        }


        /// <summary>
        /// Returns craft time including speed multipliers for the current crafting progress.
        /// </summary>
        /// <param name="asServer"></param>
        /// <returns></returns>
        public float GetMultipliedCraftTime(bool asServer)
        {
            CraftingProgress cp = (asServer) ? _serverCraftingProgress : _clientCraftingProgress;
            if (!cp.Active)
                return -1f;

            return cp.MultipliedCraftTime;
        }

        /// <summary>
        /// Returns if a recipe can be crafted.
        /// </summary>
        /// <param name="r"></param>
        /// <returns></returns>
        private bool HasCraftingResources(IRecipe r, int totalCrafts = 1)
        {
            foreach (ResourceQuantity rq in r.GetRequiredResources())
            {
                int count = _inventory.GetResourceQuantity(rq.ResourceId);
                //Resource count not met.
                if (count < (rq.Quantity * totalCrafts))
                    return false;
            }

            //If here then can craft.
            return true;
        }

        /// <summary>
        /// Crafts a recipe on the server.
        /// </summary>
        private void BeginCraftingRecipe(IRecipe r, CraftingProgress cp, ref int sequentialCount)
        {
            int seqCount = SetSequentialCount(r, ref sequentialCount);
            //Calculate multiplier.
            float percentage = (float)seqCount / (float)MAXIMUM_SEQUENTIAL_REQUIREMENT;
            float multiplier = _sequentialSpeedMultiplier.Lerp(percentage);

            cp.Initialize(r, multiplier);
        }

        /// <summary>
        /// Progresses crafting on current recipe.
        /// </summary>
        private void ProgressCrafting()
        {
            //Server only needs to progress if this frame will tick.
            if (base.IsServer && base.TimeManager.FrameTicked)
                Progress(true);
            //Client always progresses to show real-time updates.
            if (base.IsClient)
                Progress(false);

            //Progresses crafting.
            void Progress(bool asServer)
            {
                CraftingProgress cp = (asServer) ? _serverCraftingProgress : _clientCraftingProgress;
                if (cp == null || !cp.Active)
                    return;
                if (cp.Percent >= 1f)
                    return;

                IRecipe recipe = cp.Recipe;
                float delta = (asServer) ? (float)base.TimeManager.TickDelta : Time.unscaledDeltaTime;
                cp.AddTimePassed(delta);
                float percentComplete = cp.Percent;
                /* Only client needs frame by frame
                 * progress updates. Server only cares about completion. */
                if (!asServer)
                    OnCraftingProgressed?.Invoke(recipe, percentComplete, delta);

                //If not yet complete.
                if (percentComplete < 1f)
                    return;

                /* Double check inventory space before
                 * completing the recipe. It's possible the player
                 * tried to be sneaky and put things in their
                * bag during the crafting process. */
                bool hasSpace = HasInventorySpace(recipe);
                if (!hasSpace)
                {
                    InvokeCraftingResult(recipe, CraftingResult.NoSpace, asServer);
                    return;
                }

                if (asServer)
                    CraftingCompleted_Server(recipe);
            }
        }

        /// <summary>
        /// Resets CraftingProgress depending on the crafting result.
        /// </summary>
        /// <param name="cr">CraftingResult to use.</param>
        /// <param name="asServer">True if running as server.</param>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public void TryResetCraftingProgress(CraftingResult cr, bool asServer)
        {
            if (cr == CraftingResult.Canceled || cr == CraftingResult.NoSpace || cr == CraftingResult.Failed)
                ResetCraftingProgress(asServer);
        }

        /// <summary>
        /// Resets current CraftingProgress for client or server.
        /// </summary>
        /// <param name="asServer">True if running as server.</param>
        public void ResetCraftingProgress(bool asServer)
        {
            if (asServer)
                _serverCraftingProgress.Reset();
            else
                _clientCraftingProgress.Reset();
        }

        /// <summary>
        /// Invokes a crafting result.
        /// </summary>
        /// <param name="cr"></param>
        private void InvokeCraftingResult(IRecipe recipe, CraftingResult cr, bool asServer)
        {
            TryResetCraftsRemaining(cr);
            TryResetCraftingProgress(cr, asServer);
            OnCraftingResult?.Invoke(recipe, cr, asServer);
        }

        /// <summary>
        /// Invokes a crafting result of no space if the recipe would consume space beyond the inventory size.
        /// </summary>
        /// <param name="recipe">Recipe to check if there is enough space for.</param>
        /// <returns>True if a no space response was invoked.</returns>
        private bool TryInvokeNoSpace(IRecipe recipe, bool asServer)
        {
            if (!HasInventorySpace(recipe))
            {
                OnCraftingResult?.Invoke(recipe, CraftingResult.NoSpace, asServer);
                return true;
            }
            else
            {
                return false;
            }
        }

        /// <summary>
        /// Returns the inventory has enough space to accomodate a recipe.
        /// </summary>
        private bool HasInventorySpace(IRecipe recipe)
        {
            ResourceQuantity receivedResourceQuantity = recipe.GetResult();
            IResourceData iResourceData = _resourceManager.GetIResourceData(receivedResourceQuantity.ResourceId);
            int stackLimit = iResourceData.GetStackLimit();
            /* Quantiy remaining to be placed in bags.
            * This value will change as more options are ruled
            * out, such as filling stacks. */
            int quantityRemaining = receivedResourceQuantity.Quantity;
            int slotsRequired = GetSlotsRequired(quantityRemaining, stackLimit);

            /* Check if there are enough empty spaces first.
             * If so, removed resources don't even need to be
             * considered. */
            int availableSpace = _inventory.AvailableSlots;
            if (availableSpace >= slotsRequired)
                return true;

            /* If stack size is larger than 1 see if can be stacked
             * into same type entries. */
            if (iResourceData.GetStackLimit() > 1)
            {
                foreach (Bag b in _inventory.Bags)
                {
                    foreach (ResourceQuantity rq in b.Slots)
                    {
                        //Not the same resource to add.
                        if (rq.ResourceId != receivedResourceQuantity.ResourceId)
                            continue;

                        //How much can be put into this stack.
                        int available = (stackLimit - rq.Quantity);
                        quantityRemaining -= available;
                        //None remaining to be placed, everything can be fit into stacks.
                        if (quantityRemaining <= 0)
                            return true;
                    }
                }
            }

            //Update slots resourced based on quantity remaining.
            slotsRequired = GetSlotsRequired(quantityRemaining, stackLimit);
            //Return if potentially new slots required is less than available space.
            return (availableSpace >= slotsRequired);

            /* We could go further and iterate all bags to see if
             * removing resources would open up the needed slots
             * but at this time that will be considered an advanced
             * feature, and will come at a later date. */

            //Slots required to accomodate quantity.
            int GetSlotsRequired(int pQuantity, int pStack) => Mathf.CeilToInt((float)pQuantity / (float)pStack);
        }

    }


}
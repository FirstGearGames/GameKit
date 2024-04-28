using FishNet.Connection;
using FishNet.Managing.Logging;
using FishNet.Object;
using GameKit.Core.Resources;
using System.Collections.Generic;
using UnityEngine;

namespace GameKit.Core.Crafting
{

    public partial class Crafter : NetworkBehaviour
    {
        #region Public.
        /// <summary>
        /// Number of sequential crafts remaining.
        /// </summary>
        public int CraftsRemaining { get; private set; }
        #endregion

        #region Private.
        /// <summary>
        /// Cache for how many times each recipe can be crafted.
        /// </summary>
        private List<CraftableRecipeQuantity> _craftableRecipeQuantityCache = new List<CraftableRecipeQuantity>();
        /// <summary>
        /// Current recipe being crafted on the client, and it's progress.
        /// Only one CraftingProgress would be needed for dedicated servers, but for clientHost testing both are required.
        /// </summary>
        private CraftingProgress _clientCraftingProgress;
        /// <summary>
        /// Number of objects crafted in sequence on the client.
        /// Only one of these would be needed for dedicated servers, but for clientHost testing both are required.
        /// </summary>
        private int _clientSequentialCount;
        #endregion

        public override void OnStartClient()
        {
            base.OnStartClient();
            _clientCraftingProgress = new CraftingProgress();
        }

        /// <summary>
        /// Cancels the current crafting progress.
        /// </summary>
        /// <returns>True if a cancel was sent.</returns>
        [Client(Logging = LoggingType.Off)]
        public bool CancelCrafting_Client()
        {
            if (!_clientCraftingProgress.Active)
                return false;

            ServerCancelCrafting(_clientCraftingProgress.RecipeData);
            return true;
        }

        /// <summary>
        /// Sends a request to the server to craft a recipe.
        /// Returns if was able to send craft request to the server.
        /// </summary>
        [Client(Logging = LoggingType.Off)]
        public bool CraftRecipe_Client(RecipeData r, int count)
        {
            if (count == 0)
                return ReturnFailedResponse(CraftingResult.Failed);
            if (_clientCraftingProgress.Active)
                return ReturnFailedResponse(CraftingResult.FullQueue);
            if (!HasCraftingResources(r, count))
                return ReturnFailedResponse(CraftingResult.NoResources);
            if (!HasInventorySpace(r))
                return ReturnFailedResponse(CraftingResult.NoSpace);

            CraftsRemaining = count;
            /* Only begin crafting locally if the client is not host.
             * If they are, then the server side will start crafting. */
            BeginCraftingRecipe(r, _clientCraftingProgress, ref _clientSequentialCount);
            OnCraftingStarted?.Invoke(r, false);
            ServerCraftRecipe(r);
            return true;

            //Returns a failed response and unsets values.
            bool ReturnFailedResponse(CraftingResult cr)
            {
                InvokeCraftingResult(r, cr, false);
                return false;
            }
        }

        /// <summary>
        /// Resets CraftsRemaining depending on the crafting result.
        /// </summary>
        /// <param name="cr">Crafting result to use.</param>
        [Client(Logging = LoggingType.Off)]
        private void TryResetCraftsRemaining(CraftingResult cr)
        {
            if (cr == CraftingResult.Canceled || cr == CraftingResult.NoSpace || cr == CraftingResult.Failed)
                CraftsRemaining = 0;
        }

        /// <summary>
        /// Returns a craftable quantity for a recipe.
        /// </summary>
        /// <returns></returns>
        [Client(Logging = LoggingType.Off)]
        public CraftableRecipeQuantity GetCraftableQuantiy(RecipeData recipe)
        {
            //Lowest amount of quantity which can be made.
            int lowestQuantity = int.MaxValue;
            foreach (ResourceQuantity rq in recipe.GetRequiredResources())
            {
                int availableCount = _inventory.GetResourceQuantity(rq.UniqueId);
                //No resources of required type are available. This recipe cannot be completed.
                if (availableCount == 0)
                {
                    lowestQuantity = 0;
                    break;
                }
                //Number which can be crafted.
                int craftableCount = (availableCount / rq.Quantity);
                //Not enough for a single craft, recipe cannot be completed.
                if (craftableCount == 0)
                {
                    lowestQuantity = 0;
                    break;
                }
                /* Check if number craftable is less than lowest count.
                 * If so, this is the new lowest count. Recipe can only
                 * be made the number of times as the lowest craftable count. */
                if (craftableCount < lowestQuantity)
                    lowestQuantity = craftableCount;
            }

            //If was not changed then something is wrong with the recipe. Return 0 as craftable count.
            if (lowestQuantity == int.MaxValue)
                lowestQuantity = 0;
            //If craftable lowest quantity will be more than 0.
            return new CraftableRecipeQuantity(lowestQuantity, recipe);
        }


        /// <summary>
        /// Returns recipes which can be crafted with resources.
        /// </summary>
        /// <returns></returns>
        [Client(Logging = LoggingType.Off)]
        public List<CraftableRecipeQuantity> GetCraftableQuantities()
        {
            _craftableRecipeQuantityCache.Clear();
            //After all counts have been gathered build results.
            foreach (RecipeData r in _craftingManager.RecipeDatas)
            {
                //Lowest amount of quantity which can be made.
                int lowestQuantity = int.MaxValue;
                foreach (ResourceQuantity rq in r.GetRequiredResources())
                {
                    int availableCount = _inventory.GetResourceQuantity(rq.UniqueId);
                    //No resources of required type are available. This recipe cannot be completed.
                    if (availableCount == 0)
                    {
                        lowestQuantity = 0;
                        break;
                    }
                    //Number which can be crafted.
                    int craftableCount = (availableCount / rq.Quantity);
                    //Not enough for a single craft, recipe cannot be completed.
                    if (craftableCount == 0)
                    {
                        lowestQuantity = 0;
                        break;
                    }
                    /* Check if number craftable is less than lowest count.
                     * If so, this is the new lowest count. Recipe can only
                     * be made the number of times as the lowest craftable count. */
                    if (craftableCount < lowestQuantity)
                        lowestQuantity = craftableCount;
                }

                //If craftable lowest quantity will be more than 0.
                if (lowestQuantity > 0)
                    _craftableRecipeQuantityCache.Add(new CraftableRecipeQuantity(lowestQuantity, r));
            }

            return _craftableRecipeQuantityCache;
        }


        /// <summary>
        /// Updates client with a recipe progress state.
        /// This is entirely for visuals; the server sends different messages for removing and adding resources via crafting.
        /// </summary>
        [TargetRpc]
        private void TargetCraftingResult(NetworkConnection c, RecipeData r, CraftingResult result)
        {
            //No crafting is active, nothing to validate against.
            if (!_clientCraftingProgress.Active)
                return;
            //Different recipe being crafted. Should not be possible.
            if (r != null && _clientCraftingProgress.RecipeData != r)
                return;

            bool completed = (result == CraftingResult.Completed);
            ResetCraftingProgress(false);
            /* If successfully completed then remove one
             * from remaining. Otherwise set to 0 since crafting
             * failed. */
            if (completed)
                CraftsRemaining = Mathf.Max(0, CraftsRemaining - 1);
            else
                CraftsRemaining = 0;

            //Invoke new state
            InvokeCraftingResult(r, result, false);

            if (completed)
            {
                _lastCraftedRecipe = r;
                _lastCompletedCraftTime = Time.unscaledTime;
                //Start over if there are sequential crafts.
                if (CraftsRemaining > 0)
                    CraftRecipe_Client(r, CraftsRemaining);
            }
        }

    }


}
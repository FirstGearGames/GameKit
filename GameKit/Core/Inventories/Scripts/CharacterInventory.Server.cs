using GameKit.Core.Crafting;

namespace GameKit.Core.Inventories
{

    public partial class CharacterInventory : InventoryBase
    {

        public override void OnStartServer()
        {
            Crafter crafter = GetComponentInParent<Crafter>();
            crafter.OnCraftingResult += Crafter_OnCraftingResult;
        }


        /// <summary>
        /// Called after receiving a crafting result.
        /// </summary>
        /// <param name="r">Recipe the result is for.</param>
        /// <param name="result">The crafting result.</param>
        /// <param name="asServer">True if callback is for server.</param>
        private void Crafter_OnCraftingResult(RecipeData r, CraftingResult result, bool asServer)
        {
            if (result == CraftingResult.Completed)
                base.UpdateResourcesFromRecipe(r, asServer);
        }

    }

}
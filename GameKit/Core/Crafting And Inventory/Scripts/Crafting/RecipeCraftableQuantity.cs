namespace GameKit.Core.Crafting
{

    /// <summary>
    /// Contains how many times a recipe may be performed with current resources.
    /// </summary>
    public struct CraftableRecipeQuantity
    {
        /// <summary>
        /// How many of recipe which can be made.
        /// </summary>
        public int Quantity;
        /// <summary>
        /// Recipe quantity is for.
        /// </summary>
        public IRecipe Recipe;

        public CraftableRecipeQuantity(int quantity, IRecipe recipe)
        {
            Quantity = quantity;
            Recipe = recipe;
        }
    }


}
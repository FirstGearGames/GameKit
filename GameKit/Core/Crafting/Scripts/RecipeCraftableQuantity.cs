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
        public RecipeData Recipe;

        public CraftableRecipeQuantity(int quantity, RecipeData recipe)
        {
            Quantity = quantity;
            Recipe = recipe;
        }
    }


}
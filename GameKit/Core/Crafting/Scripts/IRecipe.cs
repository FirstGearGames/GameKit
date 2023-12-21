using GameKit.Core.Resources;
using System.Collections.Generic;

namespace GameKit.Core.Crafting
{

    public interface IRecipe
    {
        public int GetIndex();
        public void SetIndex(int value);

        /// <summary>
        /// Gets the time it takes this recipe to be crafted.
        /// </summary>
        /// <returns></returns>
        public float GetCraftTime();
        /// <summary>
        /// Resource quantity supplied as a result of the recipe.
        /// </summary>
        public ResourceQuantity GetResult();
        /// <summary>
        /// Resources required to get the result.
        /// </summary>
        public List<ResourceQuantity> GetRequiredResources();
    }

}
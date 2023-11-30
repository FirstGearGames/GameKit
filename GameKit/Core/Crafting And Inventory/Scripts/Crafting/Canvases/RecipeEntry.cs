using System.Collections.Generic;
using TMPro;

using UnityEngine;
using GameKit.Core.Resources;
using GameKit.Core.Crafting;
using GameKit.Core.CraftingAndInventories.Resources;

namespace GameKit.Core.CraftingAndInventories.Crafting.Canvases
{

    public class RecipeEntry : MonoBehaviour
    {
        /// <summary>
        /// Text to show recipe information.
        /// </summary>
        [Tooltip("Text to show recipe information.")]
        [SerializeField]
        private TextMeshProUGUI _titleTMP;

        /// <summary>
        /// Recipe this entry is for.
        /// </summary>
        public IRecipe Recipe;
        /// <summary>
        /// CraftingCanvas for this entry.
        /// </summary>
        private CraftingCanvas _craftingCanvas;
        /// <summary>
        /// ResourceManager manager for the NetworkManager.
        /// </summary>
        private ResourceManager _resourceManager;
        /// <summary>
        /// How many times this recipe can be crafted with current resources.
        /// </summary>
        public int CraftableCount { get; private set; }

        /// <summary>
        /// Initializes this entry.
        /// </summary>
        public void Initialize(CraftingCanvas canvas, ResourceManager rm, IRecipe r)
        {
            _craftingCanvas = canvas;
            _resourceManager = rm;
            Recipe = r;
            UpdateAvailableCrafts(0);
        }

        public void Initialize(string text)
        {
            _titleTMP.text = text;
        }

        /// <summary>
        /// Updates the number of crafts possible using a RecipeCraftableQuantity.
        /// </summary>
        public void UpdateAvailableCrafts(CraftableRecipeQuantity rcq)
        {
            if (rcq.Recipe != Recipe)
            {
                Debug.LogError($"Recipe does not match");
                return;
            }

            UpdateAvailableCrafts(rcq.Quantity);
        }


        /// <summary>
        /// Updates the number of crafts possible by searching a RecipeCraftableQuantity collection.
        /// </summary>
        public void UpdateAvailableCrafts(List<CraftableRecipeQuantity> craftableQuantities)
        {
            foreach (CraftableRecipeQuantity rcq in craftableQuantities)
            {
                if (rcq.Recipe != Recipe)
                    continue;

                UpdateAvailableCrafts(rcq.Quantity);
                return;
            }

            //Fall through, quantity not found.
            UpdateAvailableCrafts(0);
        }

        /// <summary>
        /// Updates available crafts with a set count.
        /// </summary>
        private void UpdateAvailableCrafts(int count)
        {
            CraftableCount = count;
            string countText = (count > 0) ? $" ({count})" : string.Empty;
            IResourceData ird = _resourceManager.GetIResourceData(Recipe.GetResult().ResourceId);
            ResourceData rd = (ResourceData)ird;
            _titleTMP.text = $"{rd.GetDisplayName()}{countText}";
        }


        public void OnClick_PreviewRecipe()
        {
            if (_craftingCanvas == null || Recipe == null)
                return;

            _craftingCanvas.SelectRecipe(Recipe);
        }

    }


}
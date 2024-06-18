using GameKit.Dependencies.Utilities;
using System.Collections.Generic;

using UnityEngine;
using UnityEngine.UI;
using GameKit.Core.Resources;
using GameKit.Core.Inventories;
using GameKit.Core.Dependencies;
using Sirenix.OdinInspector;

namespace GameKit.Core.Crafting.Canvases
{
    public class CraftingCanvas : MonoBehaviour
    {
        /// <summary>
        /// Prefab for each recipe listing.
        /// </summary>
        [Tooltip("Prefab for each recipe listing.")]
        [SerializeField, BoxGroup("Recipes")]
        private RecipeEntry _entryPrefab;
        /// <summary>
        /// Content transform that holds recipes.
        /// </summary>
        [Tooltip("Content transform that holds recipes.")]
        [SerializeField, BoxGroup("Recipes")]
        private Transform _recipesContent;

        /// <summary>
        /// Prefab for requires recipe resources.
        /// </summary>
        [Tooltip("Prefab for requires recipe resources.")]
        [SerializeField, BoxGroup("Preview")]
        private RequiredResourceEntry _requiredResourceEntryPrefab;
        /// <summary>
        /// Content transform that holds recipe previews.
        /// </summary>
        [Tooltip("Content transform that holds recipe previews.")]
        [SerializeField, BoxGroup("Preview")]
        private Transform _previewRecipeContent;
        /// <summary>
        /// Script that holds recipe result.
        /// </summary>
        [Tooltip("Script that holds recipe result.")]
        [SerializeField, BoxGroup("Preview")]
        private RequiredResourceEntry _previewResult;

        /// <summary>
        /// Image for the crafting progress bar.
        /// </summary>
        [Tooltip("Image for the crafting progress bar.")]
        [SerializeField, BoxGroup("Crafting")]
        private Image _progressImage;
        /// <summary>
        /// Button to craft one item for hte current recipe.
        /// </summary>
        [Tooltip("Button to craft one item.")]
        [SerializeField, BoxGroup("Crafting")]
        private Button _craftOneButton;
        /// <summary>
        /// Button to craft all possible items for the current recipe.
        /// </summary>
        [Tooltip("Button to craft all possible items for the current recipe.")]
        [SerializeField, BoxGroup("Crafting")]
        private Button _craftAllButton;
        /// <summary>
        /// Button to cancel crafting progress.
        /// </summary>
        [Tooltip("Button to cancel crafting progress.")]
        [SerializeField, BoxGroup("Crafting")]
        private Button _cancelCraftingButton;

        /// <summary>
        /// Currently selected recipe.
        /// </summary>
        private RecipeData _selectedRecipe;
        /// <summary>
        /// Number of crafts to perform.
        /// </summary>
        private int _craftableCount;
        /// <summary>
        /// CraftingManager for the NetworkManager.
        /// </summary>
        private CraftingManager _craftingManager;
        /// <summary>
        /// ResourceManager for the NetworkManager.
        /// </summary>
        private ResourceManager _resourceManager;
        /// <summary>
        /// Crafter for the local client.
        /// </summary>
        private Crafter _crafter => _clientInstance.Crafter;
        /// <summary>
        /// Inventory for the local client.
        /// </summary>
        private Inventory _inventory => _clientInstance.Inventory;
        /// <summary>
        /// ClientInstance for the local client.
        /// </summary>
        private ClientInstance _clientInstance;
        /// <summary>
        /// Added selectable recipes.
        /// </summary>
        private List<RecipeEntry> _recipeEntries = new List<RecipeEntry>();
        /// <summary>
        /// Added resources which preview requirements for a recipe.
        /// </summary>
        private List<RequiredResourceEntry> _requiredResourceEntries = new List<RequiredResourceEntry>();

        /// <summary>
        /// How quickly to fade the progress bar in and out. This is just for eye candy.
        /// </summary>
        private const float FADE_RATE = 2f;

        private void Awake()
        {
            _recipesContent.DestroyChildren<RecipeEntry>();
            _previewRecipeContent.DestroyChildren<RequiredResourceEntry>();
            _previewResult.ResetValues();

            _cancelCraftingButton.onClick.AddListener(OnClick_Cancel);
            _craftOneButton.onClick.AddListener(OnClick_CraftOne);
            _craftAllButton.onClick.AddListener(OnClick_CraftAll);
            EnableButtons(false);

            ClientInstance.OnClientInstanceChangeInvoke(new ClientInstance.ClientInstanceChangeDel(ClientInstance_OnClientChange), false);
        }

        private void OnDestroy()
        {
            ClientInstance.OnClientInstanceChange -= ClientInstance_OnClientChange;
        }

        private void Update()
        {
            FadeProgressImage();
        }

        /// <summary>
        /// Fades the progress image in and out based on fill amount.
        /// </summary>
        private void FadeProgressImage()
        {
            float rate;
            float target;
            if (_progressImage.fillAmount == 1f)
            {
                rate = FADE_RATE;
                target = 0f;
            }
            else
            {
                rate = (FADE_RATE * 2f);
                target = 1f;
            }
            Color c = _progressImage.color;
            c.a = Mathf.MoveTowards(c.a, target, rate * Time.deltaTime);
            _progressImage.color = c;
        }

        /// <summary>
        /// Called when a ClientInstance runs OnStop or OnStartClient.
        /// </summary>
        private void ClientInstance_OnClientChange(ClientInstance instance, ClientInstanceState state, bool asServer)
        {
            if (asServer)
                return;
            if (instance == null)
                return;
            //Do not do anything if this is not the instance owned by local client.
            if (!instance.IsOwner)
                return;

            if (state == ClientInstanceState.PostInitialize)
            {
                _clientInstance = instance;
                _craftingManager = instance.NetworkManager.GetInstance<CraftingManager>();
                _resourceManager = instance.NetworkManager.GetInstance<ResourceManager>();
                Initialize();
            }
        }

        /// <summary>
        /// Initializes this for use.
        /// </summary>
        private void Initialize()
        {
            _recipesContent.DestroyChildren<RecipeEntry>();

            foreach (RecipeData r in _craftingManager.RecipeDatas)
            {
                RecipeEntry re = Instantiate(_entryPrefab, _recipesContent);
                re.Initialize(this, _resourceManager, r);
                _recipeEntries.Add(re);
            }

            //Select the first entry.
            if (_recipeEntries.Count > 0)
                SelectRecipe(_recipeEntries[0].Recipe);
        }

        /// <summary>
        /// Refreshes which recipes can be crafted with current resources.
        /// </summary>
        public void RefreshAvailableRecipes()
        {
            List<CraftableRecipeQuantity> availableRecipes = _crafter.GetCraftableQuantities();

            if (availableRecipes == null)
                return;

            foreach (RecipeEntry item in _recipeEntries)
                item.UpdateAvailableCrafts(availableRecipes);

            if (_selectedRecipe != null)
                SelectRecipe(_selectedRecipe);
        }


        /// <summary>
        /// Selects a recipe to preview.
        /// </summary>
        /// <param name="r"></param>
        public void SelectRecipe(RecipeData r)
        {
            if (_resourceManager == null || r == null)
            {
                _selectedRecipe = null;
                _previewRecipeContent.DestroyChildren<RequiredResourceEntry>();
                _previewResult.ResetValues();
                return;
            }

            if (_selectedRecipe != r)
                PreviewRecipe(r);
            else
                UpdatePreview();

            _selectedRecipe = r;
        }

        /// <summary>
        /// Updates the current preview with latest values.
        /// </summary>
        private void UpdatePreview()
        {
            foreach (RequiredResourceEntry rre in _requiredResourceEntries)
                rre.UpdateAvailable();

            _previewResult.UpdateAvailable();
        }

        /// <summary>
        /// Previews a recipe.
        /// </summary>
        private void PreviewRecipe(RecipeData r)
        {
            _previewRecipeContent.DestroyChildren<RequiredResourceEntry>();
            _requiredResourceEntries.Clear();

            foreach (SerializableResourceQuantity rq in r.GetRequiredResources())
            {
                RequiredResourceEntry rre = Instantiate(_requiredResourceEntryPrefab, _previewRecipeContent);
                rre.Initialize(_resourceManager, rq, _inventory);
                _requiredResourceEntries.Add(rre);
            }

            _previewResult.Initialize(_resourceManager, r.GetResult(), _inventory);
        }

        /// <summary>
        /// Cancels current crafting.
        /// </summary>
        public void OnClick_Cancel()
        {
            bool cancelSent = _crafter.CancelCrafting_Client();
            if (cancelSent)
                _cancelCraftingButton.interactable = false;
        }

        /// <summary>
        /// Changes enable state on buttons based on if crafting.
        /// </summary>
        /// <param name="crafting"></param>
        private void EnableButtons(bool crafting)
        {
            _craftAllButton.interactable = !crafting;
            _craftOneButton.interactable = !crafting;
            _cancelCraftingButton.interactable = crafting;
        }

        /// <summary>
        /// Gets the craftable quantity for the selected recipe.
        /// </summary>
        /// <returns></returns>
        private int GetCraftableCount()
        {
            if (_selectedRecipe == null)
                return 0;

            return _crafter.GetCraftableQuantiy(_selectedRecipe).Quantity;
        }

        /// <summary>
        /// Crafts the recipe once.
        /// </summary>
        public void OnClick_CraftOne()
        {
            if (GetCraftableCount() == 0)
                return;

            _craftableCount = 1;
            BeginCrafting();
        }

        /// <summary>
        /// Crafts as many recipes as possible.
        /// </summary>
        public void OnClick_CraftAll()
        {
            if (_selectedRecipe == null)
                return;

            _craftableCount = GetCraftableCount();
            if (_craftableCount == 0)
                return;

            BeginCrafting();
        }

        /// <summary>
        /// Begins crafting with the server.
        /// </summary>
        private void BeginCrafting()
        {
            EnableButtons(true);

            _crafter.OnCraftingProgressed -= Crafter_OnCraftingProgressed;
            _crafter.OnCraftingResult -= Crafter_OnCraftingResult;
            _crafter.OnCraftingProgressed += Crafter_OnCraftingProgressed;
            _crafter.OnCraftingResult += Crafter_OnCraftingResult;

            _inventory.OnBulkResourcesUpdated -= Inventory_OnBulkResourcesUpdated;
            _inventory.OnBulkResourcesUpdated += Inventory_OnBulkResourcesUpdated;
            _crafter.CraftRecipe_Client(_selectedRecipe, _craftableCount);
        }

        /// <summary>
        /// Called after inventory comples bulk changes.
        /// </summary>
        private void Inventory_OnBulkResourcesUpdated()
        {
            RefreshAvailableRecipes();
        }

        /// <summary>
        /// Called after receiving a craft result.
        /// </summary>
        /// <param name="r"></param>
        /// <param name="result"></param>
        /// <param name="asServer"></param>
        private void Crafter_OnCraftingResult(RecipeData r, CraftingResult result, bool asServer)
        {
            if (!asServer)
            {
                EnableButtons(_crafter.CraftsRemaining > 0);
                Debug.Log($"Crafting result: {r.GetResult().UniqueId}, {result}");

                //If canceled or failed.
                if (result == CraftingResult.Canceled || result == CraftingResult.Failed)
                    Crafter_OnCraftingProgressed(r, 0f, 0f);
            }
        }

        /// <summary>
        /// Called when crafting progress is updated.
        /// </summary>
        /// <param name="r"></param>
        /// <param name="percent"></param>
        private void Crafter_OnCraftingProgressed(RecipeData r, float percent, float delta)
        {
            _progressImage.fillAmount = percent;
        }

    }
}

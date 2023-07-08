using GameKit.Crafting;
using GameKit.Crafting.Managers;
using GameKit.Inventories;
using GameKit.Resources;
using GameKit.Resources.Managers;
using GameKit.Utilities;
using System.Collections.Generic;
using TriInspector;
using UnityEngine;
using UnityEngine.UI;

namespace GameKit.Examples.Crafting.Canvases
{

    [DeclareFoldoutGroup("Recipes")]
    [DeclareFoldoutGroup("Preview")]
    [DeclareFoldoutGroup("Crafting")]
    public class CraftingCanvas : MonoBehaviour
    {
        /// <summary>
        /// Prefab for each recipe listing.
        /// </summary>
        [PropertyTooltip("Prefab for each recipe listing.")]
        [SerializeField, Group("Recipes")]
        private RecipeEntry _entryPrefab;
        /// <summary>
        /// Content transform that holds recipes.
        /// </summary>
        [PropertyTooltip("Content transform that holds recipes.")]
        [SerializeField, Group("Recipes")]
        private Transform _recipesContent;

        /// <summary>
        /// Prefab for requires recipe resources.
        /// </summary>
        [PropertyTooltip("Prefab for requires recipe resources.")]
        [SerializeField, Group("Preview")]
        private RequiredResourceEntry _requiredResourceEntryPrefab;
        /// <summary>
        /// Content transform that holds recipe previews.
        /// </summary>
        [PropertyTooltip("Content transform that holds recipe previews.")]
        [SerializeField, Group("Preview")]
        private Transform _previewRecipeContent;
        /// <summary>
        /// Script that holds recipe result.
        /// </summary>
        [PropertyTooltip("Script that holds recipe result.")]
        [SerializeField, Group("Preview")]
        private RequiredResourceEntry _previewResult;

        /// <summary>
        /// Image for the crafting progress bar.
        /// </summary>
        [PropertyTooltip("Image for the crafting progress bar.")]
        [SerializeField, Group("Crafting")]
        private Image _progressImg;
        /// <summary>
        /// Button to craft one item for hte current recipe.
        /// </summary>
        [PropertyTooltip("Button to craft one item.")]
        [SerializeField, Group("Crafting")]
        private Button _craftOneButton;
        /// <summary>
        /// Button to craft all possible items for the current recipe.
        /// </summary>
        [PropertyTooltip("Button to craft all possible items for the current recipe.")]
        [SerializeField, Group("Crafting")]
        private Button _craftAllButton;
        /// <summary>
        /// Button to cancel crafting progress.
        /// </summary>
        [PropertyTooltip("Button to cancel crafting progress.")]
        [SerializeField, Group("Crafting")]
        private Button _cancelCraftingButton;

        /// <summary>
        /// Currently selected recipe.
        /// </summary>
        private IRecipe _selectedRecipe;
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

            ClientInstance.OnClientChange += ClientInstance_OnClientChange;
            ClientInstance_OnClientChange(ClientInstance.Instance, true);
        }

        private void OnDestroy()
        {
            ClientInstance.OnClientChange -= ClientInstance_OnClientChange;
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
            if (_progressImg.fillAmount == 1f)
            {
                rate = FADE_RATE;
                target = 0f;
            }
            else
            {
                rate = (FADE_RATE * 2f);
                target = 1f;
            }
            Color c = _progressImg.color;
            c.a = Mathf.MoveTowards(c.a, target, rate * Time.deltaTime);
            _progressImg.color = c;
        }

        /// <summary>
        /// Called when a ClientInstance runs OnStop or OnStartClient.
        /// </summary>
        private void ClientInstance_OnClientChange(ClientInstance instance, bool started)
        {
            if (instance == null)
                return;
            //Do not do anything if this is not the instance owned by local client.
            if (!instance.IsOwner)
                return;

            if (started)
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

            foreach (IRecipe r in _craftingManager.Recipes)
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
        public void SelectRecipe(IRecipe r)
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
        private void PreviewRecipe(IRecipe r)
        {
            _previewRecipeContent.DestroyChildren<RequiredResourceEntry>();
            _requiredResourceEntries.Clear();

            foreach (ResourceQuantity rq in r.GetRequiredResources())
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
        private void Crafter_OnCraftingResult(IRecipe r, CraftingResult result, bool asServer)
        {
            if (!asServer)
            {
                EnableButtons(_crafter.CraftsRemaining > 0);
                Debug.Log($"Crafting result: {r.GetResult().ResourceId}, {result}");
            }
        }

        /// <summary>
        /// Called when crafting progress is updated.
        /// </summary>
        /// <param name="r"></param>
        /// <param name="percent"></param>
        private void Crafter_OnCraftingProgressed(IRecipe r, float percent, float delta)
        {
            _progressImg.fillAmount = percent;
        }

    }
}

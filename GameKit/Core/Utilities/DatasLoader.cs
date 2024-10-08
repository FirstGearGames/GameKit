using UnityEngine;
using GameKit.Core.Resources;
using GameKit.Core.Resources.Droppables;
using System.Collections.Generic;
using Sirenix.OdinInspector;
using GameKit.Core.Crafting;
using GameKit.Dependencies.Utilities;
using GameKit.Core.Inventories.Bags;
#if UNITY_EDITOR
using UnityEditor;
#endif

namespace GameKit.Core.Utilities
{

    /// <summary>
    /// Loads datas into their managers.
    /// </summary>
    public class DatasLoader : MonoBehaviour
    {
        #region Serialized.
        /// <summary>
        /// All resource datas for this game.
        /// </summary>
        [Tooltip("All resource datas for this game.")]
        [SerializeField]
        private List<ResourceData> _resourceDatas = new();
        /// <summary>
        /// All resource category datas for this game.
        /// </summary>
        [Tooltip("All resource category datas for this game.")]
        [SerializeField]
        private List<ResourceCategoryData> _resourceCategoryDatas = new();
        /// <summary>
        /// All recipes for this game.
        /// </summary>
        [Tooltip("All recipes for this game.")]
        [SerializeField]
        private List<RecipeData> _recipeDatas = new();
        /// <summary>
        /// All droppables for this game.
        /// </summary>
        [Tooltip("All droppables for this game.")]
        [SerializeField]
        private List<DroppableData> _droppableDatas = new();
        /// <summary>
        /// All bag datas for this game.
        /// </summary>
        [Tooltip("All bag datas for this game.")]
        [SerializeField]
        private List<BagData> _bagDatas = new();
        #endregion

        private void Awake()
        {
            AddDatasToManagers();
        }

        /// <summary>
        /// Adds all datas to each appropriate manager.
        /// </summary>
        private void AddDatasToManagers()
        {
            ResourceManager rm = GetComponentInParent<ResourceManager>();
            rm.AddResourceData(_resourceDatas, true);
            rm.AddResourceCategoryData(_resourceCategoryDatas);

            CraftingManager cm = GetComponentInParent<CraftingManager>();
            cm.AddRecipeData(_recipeDatas, true);

            DroppableManager dm = GetComponentInParent<DroppableManager>();
            dm.AddDroppableData(_droppableDatas, true);

            BagManager bm = GetComponentInParent<BagManager>();
            bm.AddBagData(_bagDatas, true);
        }

        #region Editor.
#if UNITY_EDITOR

        /// <summary>
        /// Finds and sets all loadables.
        /// </summary>
        [PropertySpace]
        [Button("Find Loadables.", ButtonSizes.Large)]
        private void FindLoadables()
        {
            Debug.Log($"Please wait ...");

            _resourceDatas.Clear();
            _resourceCategoryDatas.Clear();
            _recipeDatas.Clear();
            _droppableDatas.Clear();

            foreach (string path in IOs.GetDirectoryFiles("Assets", new HashSet<string>(), true, "*.asset"))
            {
                System.Type t = AssetDatabase.GetMainAssetTypeAtPath(path);
                if (t == typeof(ResourceData))
                {
                    ResourceData data = AssetDatabase.LoadAssetAtPath<ResourceData>(path);
                    _resourceDatas.Add(data);
                }    
                else if (t == typeof(ResourceCategoryData))
                {
                    ResourceCategoryData data = AssetDatabase.LoadAssetAtPath<ResourceCategoryData>(path);
                    _resourceCategoryDatas.Add(data);
                }
                else if (t == typeof(RecipeData))
                {
                    RecipeData data = AssetDatabase.LoadAssetAtPath<RecipeData>(path);
                    _recipeDatas.Add(data);
                }
                else if (t == typeof(DroppableData))
                {
                    DroppableData data = AssetDatabase.LoadAssetAtPath<DroppableData>(path);
                    _droppableDatas.Add(data);
                }
            }

            EditorUtility.SetDirty(this);
            AssetDatabase.SaveAssets();

            Debug.Log($"... Loading complete. Please save your scene.");

        }
#endif
        #endregion
    }
}
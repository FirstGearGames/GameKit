using GameKit.Crafting;
using GameKit.Examples.Resources;
using GameKit.Resources;
using System.Collections.Generic;
using UnityEngine;

namespace GameKit.Examples.Crafting
{

    [CreateAssetMenu(fileName = "Recipe", menuName = "Game/New Recipe", order = 1)]
    public class Recipe : ScriptableObject, IRecipe, IEqualityComparer<IRecipe>
    {
        #region Types.
        [System.Serializable]
        public struct TypedResourceQuantity
        {
            public ResourceType ResourceType;
            public int Quantity;

            public TypedResourceQuantity(ResourceType resourceType, int quantity)
            {
                ResourceType = resourceType;
                Quantity = quantity;
            }
        }
        #endregion

        #region Public.
        /// <summary>
        /// Index of the recipe. This value is set at runtime automatically.
        /// </summary>
        [System.NonSerialized, HideInInspector]
        public int Index;
        /// <summary>
        /// The time it takes this recipe to be crafted.
        /// </summary>
        public float CraftTime = 1.5f;
        /// <summary>
        /// Resource quantity supplied as a result of the recipe.
        /// </summary>
        public TypedResourceQuantity Result;
        /// <summary>
        /// Resources required to get the result.
        /// </summary>
        public List<TypedResourceQuantity> RequiredResources = new List<TypedResourceQuantity>();
        #endregion

        #region Private.
        /// <summary>
        /// Cached for required resources.
        /// </summary>
        private List<ResourceQuantity> _requiredResourcesCache = new List<ResourceQuantity>();
        #endregion

        public int GetIndex() => Index;
        public void SetIndex(int value) => Index = value;
        public ResourceQuantity GetResult() => new ResourceQuantity((int)Result.ResourceType, Result.Quantity);
        public List<ResourceQuantity> GetRequiredResources()
        {
            //If needs to be cached.
            if (RequiredResources.Count > 0 && _requiredResourcesCache.Count == 0)
            {
                foreach (TypedResourceQuantity trq in RequiredResources)
                    _requiredResourcesCache.Add(new ResourceQuantity((int)trq.ResourceType, trq.Quantity));
            }

            return _requiredResourcesCache;
        }

        public float GetCraftTime() => CraftTime;

        #region Comparers.
        public bool Equals(IRecipe other)
        {
            if (other == null)
                return false;
            return (Index == other.GetIndex());
        }
        public override bool Equals(object obj)
        {
            if (obj == null)
                return false;
            if (obj.GetType() != typeof(IRecipe))
                return false;

            return Equals((IRecipe)obj);
        }
        public int GetHashCode(IRecipe obj)
        {
            return obj.GetHashCode();
        }
        public override int GetHashCode()
        {
            return base.GetHashCode();
        }
        public bool Equals(IRecipe r1, IRecipe r2)
        {
            bool r1Null = (r1 is null);
            bool r2Null = (r2 is null);
            //Both null, it's a match.
            if (r1Null && r2Null)
                return true;
            //One is null but the other is not.
            if (r1Null != r2Null)
                return false;

            return (r1.GetIndex() == r2.GetIndex());

        }
        #endregion

    }

}
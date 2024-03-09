using System.Collections.Generic;
using UnityEngine;
using GameKit.Core.Resources;

namespace GameKit.Core.Crafting
{

    [CreateAssetMenu(fileName = "Recipe", menuName = "Game/New Recipe", order = 1)]
    public class RecipeData : ScriptableObject, IEqualityComparer<RecipeData>
    {
        #region Types.
        [System.Serializable]
        public struct TypedResourceQuantity
        {
            public ResourceData ResourceData;
            public int Quantity;

            public TypedResourceQuantity(ResourceData resourceData, int quantity)
            {
                ResourceData = resourceData;
                Quantity = quantity;
            }
        }
        #endregion

        #region Public.
        /// <summary>
        /// True if should be recognized and used. False to remove from the game.
        /// </summary>
        public bool Enabled = true;
        /// <summary>
        /// UniqueId of the resource.   
        /// </summary>
        [HideInInspector, System.NonSerialized]
        public uint UniqueId = ResourceConsts.UNSET_RESOURCE_ID;
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

        public ResourceQuantity GetResult() => new ResourceQuantity(Result.ResourceData.UniqueId, Result.Quantity);
        public List<ResourceQuantity> GetRequiredResources()
        {
            //If needs to be cached.
            if (RequiredResources.Count > 0 && _requiredResourcesCache.Count == 0)
            {
                foreach (TypedResourceQuantity trq in RequiredResources)
                    _requiredResourcesCache.Add(new ResourceQuantity(trq.ResourceData.UniqueId, trq.Quantity));
            }

            return _requiredResourcesCache;
        }


        #region Comparers.
        public bool Equals(RecipeData other)
        {
            if (other == null)
                return false;
            return (UniqueId  == other.UniqueId);
        }
        public override bool Equals(object obj)
        {
            if (obj == null)
                return false;
            if (obj.GetType() != typeof(RecipeData))
                return false;

            return Equals((RecipeData)obj);
        }
        public int GetHashCode(RecipeData obj)
        {
            return obj.GetHashCode();
        }
        public override int GetHashCode()
        {
            return base.GetHashCode();
        }
        public bool Equals(RecipeData r1, RecipeData r2)
        {
            bool r1Null = (r1 is null);
            bool r2Null = (r2 is null);
            //Both null, it's a match.
            if (r1Null && r2Null)
                return true;
            //One is null but the other is not.
            if (r1Null != r2Null)
                return false;

            return (r1.UniqueId == r2.UniqueId);

        }
        #endregion

    }

}
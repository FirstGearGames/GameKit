using UnityEngine;

namespace GameKit.Core.Resources
{
    public interface IResourceCategoryData
    {
        public int GetResourceCategoryId();
        public string GetDisplayName();
        public Sprite GetIcon();
    }


}
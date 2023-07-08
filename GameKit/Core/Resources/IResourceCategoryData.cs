using UnityEngine;

namespace GameKit.Resources
{
    public interface IResourceCategoryData
    {
        public int GetResourceCategoryId();
        public string GetDisplayName();
        public Sprite GetIcon();
    }


}
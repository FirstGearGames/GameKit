using FishNet.Serializing;
using GameKit.Core.Resources;

namespace GameKit.Core.Crafting
{


    public static class Recipe_Serializers
    {
        public static void WriteRecipeData(this Writer w, RecipeData value)
        {
            if (value == null)
                w.WriteUInt32(ResourceConsts.UNSET_RESOURCE_ID);
            else
                w.WriteUInt32(value.UniqueId);
        }
        public static RecipeData ReadRecipeData(this Reader r)
        {
            uint index = r.ReadUInt32();
            if (index == ResourceConsts.UNSET_RESOURCE_ID)
                return null;

            CraftingManager cm = r.NetworkManager.GetInstance<CraftingManager>();
            if (cm != null)
                return cm.GetRecipe(index);

            //Fall through.
            return null;
        }
    }

}

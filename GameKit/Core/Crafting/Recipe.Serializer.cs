using GameKit.Crafting.Managers;
using FishNet.Managing;
using FishNet.Serializing;

namespace GameKit.Crafting
{


    public static class Recipe_Serializers
    {
        public static void WriteIRecipe(this Writer w, IRecipe value)
        {
            if (value == null)
                w.WriteInt32(-1);
            else
                w.WriteInt32(value.GetIndex());
        }
        public static IRecipe ReadIRecipe(this Reader r)
        {
            int index = r.ReadInt32();
            if (index == -1)
                return null;

            CraftingManager cm = r.NetworkManager.GetInstance<CraftingManager>();
            if (cm != null)
                return cm.GetRecipe(index);

            //Fall through.
            return null;
        }
    }

}

using GameKit.Examples.Crafting.Canvases;
using GameKit.Examples.Resources;
using GameKit.Inventories;
using System.Collections.Generic;
using UnityEngine;

namespace GameKit.Crafting.Testing
{

    public class ChangeResourcesCanvas : MonoBehaviour
    {

        public void AddRandomResources()
        {
            Inventory inv = GameObject.FindObjectOfType<Inventory>();
            if (inv == null)
                return;

            List<ResourceType> resources = new List<ResourceType>();
            System.Array pidValues = System.Enum.GetValues(typeof(ResourceType));
            foreach (ResourceType rt in pidValues)
                resources.Add(rt);

            for (int i = 0; i < 5; i++)
            {
                int count = Random.Range(1, 2);
                int index = Random.Range(0, (resources.Count - 1));
                ResourceType rt = resources[index];
                if (rt == ResourceType.Rope || rt == ResourceType.Crossbow || rt == ResourceType.Unset)
                {
                    i--;
                    continue;
                }
                inv.ModifiyResourceQuantity((int)rt, count);
            }

            CraftingCanvas cmt = GameObject.FindObjectOfType<CraftingCanvas>();
            cmt.RefreshAvailableRecipes();
        }


        public void RemoveRandomResources()
        {
            Inventory inv = GameObject.FindObjectOfType<Inventory>();
            if (inv == null)
                return;

            List<ResourceType> resources = new List<ResourceType>();
            foreach (int rId in inv.ResourceQuantities.Keys)
                resources.Add((ResourceType)rId);

            if (resources.Count == 0)
                return;

            for (int i = 0; i < 5; i++)
            {
                int count = Random.Range(1, 2);
                int index = Random.Range(0, (resources.Count - 1));

                inv.ModifiyResourceQuantity((int)resources[index], -count);
            }

            CraftingCanvas cmt = GameObject.FindObjectOfType<CraftingCanvas>();
            cmt.RefreshAvailableRecipes();
        }



    }


}
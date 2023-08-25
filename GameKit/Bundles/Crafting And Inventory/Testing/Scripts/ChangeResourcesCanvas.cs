using FishNet.Connection;
using FishNet.Object;
using GameKit.Bundles.CraftingAndInventories.Crafting.Canvases;
using GameKit.Bundles.CraftingAndInventories.Resources;
using GameKit.Core.Inventories;
using System.Collections.Generic;
using UnityEngine;

namespace GameKit.Crafting.Testing
{

    public class ChangeResourcesCanvas : NetworkBehaviour
    {

        public void AddRandomResources()
        {
            //Client has to ask server to add.
            if (base.IsClientOnly)
            {
                ServerAdd();
                return;
            }
            else if (!base.IsServer)
            {
                return;
            }

            Inventory inv = GameObject.FindObjectOfType<Inventory>();
            if (inv == null)
            {
                Debug.Log("Player inventory was not found.");
                return;
            }

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
            cmt?.RefreshAvailableRecipes();
        }



        public void RemoveRandomResources()
        {
            //Client has to ask server to remove.
            if (base.IsClientOnly)
            {
                ServerRemove();
                return;
            }
            else if (!base.IsServer)
            {
                return;
            }

            Inventory inv = GameObject.FindObjectOfType<Inventory>();
            if (inv == null)
            {
                Debug.Log("Player inventory was not found.");
                return;
            }

            List<ResourceType> resources = new List<ResourceType>();
            foreach (int rId in inv.ResourceQuantities.Keys)
                resources.Add((ResourceType)rId);

            if (resources.Count == 0)
            {
                Debug.Log("No resources to remove.");
                return;
            }

            for (int i = 0; i < 5; i++)
            {
                int count = Random.Range(1, 2);
                int index = Random.Range(0, (resources.Count - 1));

                inv.ModifiyResourceQuantity((int)resources[index], -count);
            }

            RefreshAvailableRecipes();
        }

        [ServerRpc(RequireOwnership = false)]
        private void ServerAdd(NetworkConnection caller = null)
        {
            AddRandomResources();
            TargetRefreshAvailableRecipes(caller);
        }
        [ServerRpc(RequireOwnership = false)]
        private void ServerRemove(NetworkConnection caller = null)
        {
            RemoveRandomResources();
            TargetRefreshAvailableRecipes(caller);
        }

        [TargetRpc]
        private void TargetRefreshAvailableRecipes(NetworkConnection c)
        {
            RefreshAvailableRecipes();
        }


        private void RefreshAvailableRecipes()
        {
            CraftingCanvas cmt = GameObject.FindObjectOfType<CraftingCanvas>();
            cmt?.RefreshAvailableRecipes();
        }

    }


}
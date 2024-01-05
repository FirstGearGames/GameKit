using FishNet.Connection;
using FishNet.Object;
using GameKit.Core.Crafting.Canvases;
using GameKit.Core.Inventories;
using GameKit.Core.Resources;
using System.Collections.Generic;
using UnityEngine;

namespace GameKit.Crafting.Testing
{

    public class ChangeResourcesCanvas : NetworkBehaviour
    {

        public void AddRandomResources()
        {
            //Client has to ask server to add.
            if (base.IsClientOnlyStarted)
            {
                ServerAdd();
                return;
            }
            else if (!base.IsServerStarted)
            {
                return;
            }

            Inventory inv = GameObject.FindObjectOfType<Inventory>();
            if (inv == null)
            {
                Debug.Log("Player inventory was not found.");
                return;
            }

            Debug.LogError($"Get all uniqueIds in managers and add them here.");
            List<uint> resources = new List<uint>();

            for (int i = 0; i < 5; i++)
            {
                int count = Random.Range(1, 2);
                int index = Random.Range(0, (resources.Count - 1));
                inv.ModifiyResourceQuantity(resources[index], count);
            }

            inv.InventorySortedChanged();
            CraftingCanvas cmt = GameObject.FindObjectOfType<CraftingCanvas>();
            cmt?.RefreshAvailableRecipes();
        }



        public void RemoveRandomResources()
        {
            //Client has to ask server to remove.
            if (base.IsClientOnlyStarted)
            {
                ServerRemove();
                return;
            }
            else if (!base.IsServerStarted)
            {
                return;
            }

            Inventory inv = GameObject.FindObjectOfType<Inventory>();
            if (inv == null)
            {
                Debug.Log("Player inventory was not found.");
                return;
            }

            Debug.LogError($"Get all uniqueIds in managers and add them here.");
            List<uint> resources = new List<uint>();

            if (resources.Count == 0)
            {
                Debug.Log("No resources to remove.");
                return;
            }

            for (int i = 0; i < 5; i++)
            {
                int count = Random.Range(1, 2);
                int index = Random.Range(0, (resources.Count - 1));
                inv.ModifiyResourceQuantity(resources[index], -count);
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
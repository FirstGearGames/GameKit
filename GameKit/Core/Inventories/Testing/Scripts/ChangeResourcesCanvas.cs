using FishNet;
using FishNet.Connection;
using FishNet.Object;
using GameKit.Core.Crafting.Canvases;
using GameKit.Core.Dependencies;
using GameKit.Core.Inventories;
using GameKit.Core.Resources;
using GameKit.Dependencies.Utilities;
using System.Collections.Generic;
using UnityEngine;

namespace GameKit.Crafting.Testing
{

    /// <summary>
    /// THIS IS FOR TESTING ONLY>
    /// </summary>
    public class ChangeResourcesCanvas : NetworkBehaviour
    {

        public void AddRandomResources()
        {
            if (!base.IsServerStarted && !base.IsClientStarted)
            {
                Debug.LogError($"This action requires server or client to be running.");
                return;
            }

            //Get the resouceManager to gather what resources can be added.
            if (!InstanceFinder.TryGetInstance<ResourceManager>(out ResourceManager rm))
            {
                Debug.LogError($"ResourceManager not found. Cannot continue.");
                return;
            }
            List<ResourceData> addableResources = rm.ResourceDatas;
            if (addableResources.Count == 0)
            {
                Debug.LogError($"There are no resources added to the ResourceManager.");
                return;
            }
            if (ClientInstance.Instances.Count == 0)
            {
                Debug.LogError($"There are no clients to add resources for.");
                return;
            }

            //Client has to ask server to add.
            if (base.IsClientOnlyStarted)
            {
                ServerAdd();
                return;
            }

            foreach (ClientInstance ci in ClientInstance.Instances)
            {
                Inventory inv = ci.Inventory;
                int tryAdded = 0;
                int notAdded = 0;
                const int maxIterations = 1;
                for (int i = 0; i < maxIterations; i++)
                {
                    int count = Ints.RandomInclusiveRange(1, 2);
                    int index = Ints.RandomExclusiveRange(0, addableResources.Count);
                    notAdded += inv.ModifiyResourceQuantity(addableResources[index].UniqueId, count);
                    tryAdded += count;
                }

                Debug.Log($"Added {tryAdded - notAdded} of {tryAdded} items.");

                inv.InventorySortedChanged();
                CraftingCanvas cmt = GameObject.FindObjectOfType<CraftingCanvas>();
                cmt.RefreshAvailableRecipes();
            }

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
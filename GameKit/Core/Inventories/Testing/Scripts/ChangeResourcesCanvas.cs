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

        public void AddOrRemoveRandomResources(bool add)
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
            List<ResourceData> rds = rm.ResourceDatas;
            if (rds.Count == 0)
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
                if (add)
                    ServerAdd();
                else
                    ServerRemove();
                return;
            }

            foreach (ClientInstance ci in ClientInstance.Instances)
            {
                Inventory inv = ci.Inventory;
                InventoryBase invBase = inv.GetInventoryBase(InventoryCategory.Character);
                int tryModify = 0;
                int notModified = 0;
                const int maxIterations = 1;
                for (int i = 0; i < maxIterations; i++)
                {
                    int count = Ints.RandomInclusiveRange(1, 2);
                    if (!add)
                        count *= -1;
                    int index = Ints.RandomExclusiveRange(0, rds.Count);
                    notModified += invBase.ModifyResourceQuantity(rds[index].UniqueId, count);
                    tryModify += Mathf.Abs(count);
                }

                string addedRemovedText = (add) ? "Added " : "Removed ";
                Debug.Log($"{addedRemovedText} {tryModify - notModified} of {tryModify} items.");

                if (ci.Owner.IsLocalClient)
                {
                    CraftingCanvas cmt = GameObject.FindObjectOfType<CraftingCanvas>();
                    cmt.RefreshAvailableRecipes();
                }
            }


        }

        [ServerRpc(RequireOwnership = false)]
        private void ServerAdd(NetworkConnection caller = null)
        {
            AddOrRemoveRandomResources(true);
            TargetRefreshAvailableRecipes(caller);
        }
        [ServerRpc(RequireOwnership = false)]
        private void ServerRemove(NetworkConnection caller = null)
        {
            AddOrRemoveRandomResources(false);
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
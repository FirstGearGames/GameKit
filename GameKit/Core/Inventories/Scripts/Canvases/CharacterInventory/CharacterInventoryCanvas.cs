using GameKit.Core.Dependencies;
using GameKit.Core.Utilities;

namespace GameKit.Core.Inventories.Canvases.Characters
{

    public class CharacterInventoryCanvas : InventoryCanvasBase
    {
        protected override void ClientInstance_OnClientInstanceChange(ClientInstance instance, ClientInstanceState state, bool asServer)
        {
            base.ClientInstance_OnClientInstanceChange(instance, state, asServer);

            bool started = (state == ClientInstanceState.PostInitialize);
            //If started then get the character inventory and initialize bags.
            if (started)
            {
                Inventory inv = instance.Inventory;
                Inventory = inv.GetInventoryBase(InventoryCategory.Character, true);

                InitializeBags();
            }

            base.ChangeSubscription(started);
        }

        public override void OnPressed_ResourceEntry(ResourceEntry entry)
        {
            if (entry.ResourceData == null || entry.StackCount == 1 || !Keybinds.IsShiftHeld)
            {
                base.OnPressed_ResourceEntry(entry);
                return;
            }

            //base.OnPressed_ResourceEntry(entry);
        }
    }


}
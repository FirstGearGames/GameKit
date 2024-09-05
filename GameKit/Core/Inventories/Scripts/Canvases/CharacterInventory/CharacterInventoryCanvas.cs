using GameKit.Core.Dependencies;
using GameKit.Core.FloatingContainers.OptionMenuButtons;
using GameKit.Core.Resources;
using GameKit.Core.Utilities;
using GameKit.Dependencies.Utilities;
using GameKit.Dependencies.Utilities.Types;
using GameKit.Dependencies.Utilities.Types.CanvasContainers;

namespace GameKit.Core.Inventories.Canvases.Characters
{
    public class CharacterInventoryCanvas : InventoryCanvasBase
    {
        /// <summary>
        /// Canvas used to show stack splitting options.
        /// </summary>
        private SplittingCanvas _splittingCanvas;

        protected override void ClientInstance_OnClientInstanceChange(ClientInstance instance,
            ClientInstanceState state, bool asServer)
        {
            base.ClientInstance_OnClientInstanceChange(instance, state, asServer);

            bool started = (state == ClientInstanceState.PostInitialize);
            //If started then get the character inventory and initialize bags.
            if (started)
            {
                _splittingCanvas = instance.NetworkManager.GetInstance<SplittingCanvas>();

                Inventory inv = instance.Inventory;
                Inventory = inv.GetInventoryBase(InventoryCategory.Character, true);

                InitializeBags();
            }

            base.ChangeSubscription(started);
        }

        public override void OnPressed_ResourceEntry(ResourceEntry entry)
        {
            ResourceData data = entry.ResourceData;
            if (data == null || entry.StackCount == 1 || !Keybinds.IsShiftHeld)
            {
                base.OnPressed_ResourceEntry(entry);
                return;
            }

            SplittingCanvasConfig config = new()
            {
                Item = data,
                ConfirmCallback = new PressedDelegateData(new(OnSplitConfirmed)),
                SplitValues = new IntRange(1, entry.StackCount),
            };
            _splittingCanvas.Show(entry.transform, config);

            base.ScrollRect.enabled = false;
        }

        private void OnSplitConfirmed(string key)
        {
        }
    }
}
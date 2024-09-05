using GameKit.Dependencies.Utilities.Types.CanvasContainers;
using GameKit.Core.Resources;
using GameKit.Dependencies.Utilities.Types;

namespace GameKit.Core.FloatingContainers.OptionMenuButtons
{
    public struct SplittingCanvasConfig
    {
        #region Public.

        /// <summary>
        /// Item being split.
        /// </summary>
        public ResourceData Item;

        /// <summary>
        /// Range of the split.
        /// </summary>
        public IntRange SplitValues;

        /// <summary>
        /// Callback to invoke when a split is canceled.
        /// </summary>
        public PressedDelegateData CancelCallback;

        /// <summary>
        /// Callback to invoke when a split is confirmed.
        /// </summary>
        public PressedDelegateData ConfirmCallback;

        #endregion

        public SplittingCanvasConfig(ResourceData item, IntRange splitValues, PressedDelegateData cancelCallback, PressedDelegateData confirmCallback)
        {
            Item = item;
            SplitValues = splitValues;
            CancelCallback = cancelCallback;
            ConfirmCallback = confirmCallback;
        }

    }
}
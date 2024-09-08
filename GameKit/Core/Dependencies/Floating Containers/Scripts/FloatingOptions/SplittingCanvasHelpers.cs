using GameKit.Core.Inventories;
using GameKit.Core.Inventories.Canvases;
using GameKit.Core.Resources;
using GameKit.Dependencies.Utilities.Types;

namespace GameKit.Core.FloatingContainers.OptionMenuButtons
{
    /// <summary>
    /// Delegate for when a split is canceled.
    /// </summary>
    public delegate void CanceledDel();

    /// <summary>
    /// Delegate for when a split is confirmed.
    /// </summary>
    public delegate void ConfirmedDel(ResourceEntry resourceEntry, int moveQuantity);

    public struct SplittingCanvasConfig
    {
        #region Public.
        /// <summary>
        /// ResourceEntry being split.
        /// </summary>
        public ResourceEntry ResourceEntry;
        /// <summary>
        /// Range of the split.
        /// </summary>
        public IntRange SplitValues;
        /// <summary>
        /// Callback to invoke when a split is canceled.
        /// </summary>
        public CanceledDel CanceledCallback;
        /// <summary>
        /// Callback to invoke when a split is confirmed.
        /// </summary>
        public ConfirmedDel ConfirmedCallback;
        #endregion

        public SplittingCanvasConfig(ResourceEntry resourceEntry, IntRange splitValues, CanceledDel cancelCallback, ConfirmedDel confirmCallback)
        {
            ResourceEntry = resourceEntry;
            SplitValues = splitValues;
            CanceledCallback = cancelCallback;
            ConfirmedCallback = confirmCallback;
        }
    }
}
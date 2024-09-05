using GameKit.Dependencies.Utilities.Types.CanvasContainers;

namespace GameKit.Core.FloatingContainers.OptionMenuButtons
{
    public struct FloatingOptionsConfig
    {
        /// <summary>
        /// True to allow cancel / close without selecting an option.
        /// </summary>
        public bool AllowCancel;

        /// <summary>
        /// Callback to perform when canceling.
        /// </summary>
        public PressedDelegateData CancelCallback;
    }

}
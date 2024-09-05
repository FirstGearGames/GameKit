namespace GameKit.Dependencies.Utilities.Types.CanvasContainers
{
    public class ButtonData : IResettable
    {
        #region Public.

        /// <summary>
        /// Text to place on the button.
        /// </summary>
        public string Text { get; protected set; } = string.Empty;

        /// <summary>
        /// Callback to invoke when pressed.
        /// </summary>
        public PressedDelegateData PressedCallback;

        #endregion

        /// <summary>
        /// Initializes this for use.
        /// </summary>
        /// <param name="text">Text to display on the button.</param>
        /// <param name="callback">Callback when OnPressed is called.</param>
        /// <param name="key">Optional key to include within the callback.</param>
        public void Initialize(string text, PressedDelegateData callback)
        {
            Text = text;
            PressedCallback = callback;
        }

        /// <summary>
        /// Called whewn the button for this data is pressed.
        /// </summary>
        public virtual void OnPressed()
        {
            PressedCallback.Invoke();
        }

        public virtual void ResetState()
        {
            Text = string.Empty;
            PressedCallback = default;
        }

        public void InitializeState()
        {
        }
    }
}
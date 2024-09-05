namespace GameKit.Dependencies.Utilities.Types.CanvasContainers
{
    public struct PressedDelegateData
    {
        #region Public.

        /// <summary>
        /// Callback to use.
        /// </summary>
        public PressedDelegateDel Callback;

        /// <summary>
        /// Key for the callback.
        /// </summary>
        public string Key;

        #endregion

        public PressedDelegateData(PressedDelegateDel callback)
        {
            Callback = callback;
            Key = string.Empty;
        }
        public PressedDelegateData(PressedDelegateDel callback, string key)
        {
            Callback = callback;
            Key = key;
        }

        /// <summary>
        /// Invokes this if Callback is set.
        /// </summary>
        public void Invoke()
        {
            if (Callback != null)
                Callback.Invoke(Key);
        }
    }
}
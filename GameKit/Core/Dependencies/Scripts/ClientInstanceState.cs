namespace GameKit.Core.Dependencies
{
    /// <summary>
    /// State of ClientInstance during event invokes.
    /// </summary>
    public enum ClientInstanceState
    {
        /// <summary>
        /// ClientInstance is initializing internal features.
        /// Use this order to register objects or perform actions on the ClientInstance to complete initialization.
        /// </summary>
        PreInitialize,
        /// <summary>
        /// ClientInstance has initialized internal features.
        /// Use this order to setup objects dependent on ClientInstance settings.
        /// </summary>
        PostInitialize,
        /// <summary>
        /// ClientInstance is deinitializing internal features.
        /// Use this to read values or clean-up scripts while ClientInstance is still initialized.
        /// </summary>
        PreDeinitialize,
        /// <summary>
        /// ClientInstance has deinitialized internal features.
        /// Use this to remove any use of ClientInstance.
        /// </summary>
        PostDeinitialize,
    }

    /// <summary>
    /// Extensions for ClientInstanceState.
    /// </summary>
    public static class ClientInstanceStateExtensions
    {
        /// <summary>
        /// Returns true if state is PreInitialize or PreDeinitialize.
        /// </summary>
        public static bool IsPreState(this ClientInstanceState state)
        {
            return (state == ClientInstanceState.PreInitialize || state == ClientInstanceState.PreDeinitialize);
        }
        /// <summary>
        /// Returns true if state is PostInitialize or PostDeinitialize.
        /// </summary>
        public static bool IsPostState(this ClientInstanceState state)
        {
            return (state == ClientInstanceState.PostInitialize || state == ClientInstanceState.PostDeinitialize);
        }
        /// <summary>
        /// Returns true if state is any initialize.
        /// </summary>
        public static bool IsInitializeState(this ClientInstanceState state)
        {
            return (state == ClientInstanceState.PreInitialize || state == ClientInstanceState.PostInitialize);
        }
        /// <summary>
        /// Returns true if state is any deinitialize.
        /// </summary>
        public static bool IsDeinitializeState(this ClientInstanceState state)
        {
            return (state == ClientInstanceState.PreDeinitialize || state == ClientInstanceState.PostDeinitialize);
        }
    }

}
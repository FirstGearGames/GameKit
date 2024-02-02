using UnityEngine;

namespace GameKit.Core.Providers
{
    [CreateAssetMenu(fileName = "ProviderData", menuName = "Game/New ProviderData", order = 1)]
    public class ProviderData : ScriptableObject
    {
        /// <summary>
        /// UniqueId for the provider.
        /// </summary>
        public uint UniqueId;
    }


}
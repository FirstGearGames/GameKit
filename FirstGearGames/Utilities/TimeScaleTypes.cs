
using UnityEngine;

namespace FirstGearGames.Configurations
{

    /// <summary>
    /// Types of scaling for camera movement.
    /// </summary>
    public enum TimeScaleTypes
    {
        /// <summary>
        /// Move using unscaled time.
        /// </summary>
        Unscaled,
        /// <summary>
        /// Move using scaled time.
        /// </summary>
        Scaled,
        /// <summary>
        /// Move using Unscaled with scaled blocking conditions.
        /// </summary>
        RestrictedUnscaled
    }

}
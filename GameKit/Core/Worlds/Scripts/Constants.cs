using GameKit.Dependencies.Utilities.Types;
using System;
using System.Collections.Generic;

namespace GameKit.Core.Worlds
{
    public enum WorldObjectType
    {
        Unset = 0,
        Obtainable = 1,
        NonObtainable = 2,
    }

    public static class WorldObject
    {
        /// <summary>
        /// Multiplier to get the end value of each WorldObjectType index.
        /// </summary>
        private const uint END_INDEX_MULTIPLIER = 100000000;
        /// <summary>
        /// Minimum added to each WorldObjectType start.
        /// </summary>
        private const uint MINIMUM_VALUE = 1;
        /// <summary>
        /// Ranges for each WorldObjectType.
        /// </summary>
        private static Dictionary<WorldObjectType, UIntRange> _objectTypeRanges;

        /// <summary>
        /// Builds ObjectTypeRanges if needed.
        /// </summary>
        private static void TryBuildObjectTypeRanges()
        {
            if (_objectTypeRanges != null)
                return;

            _objectTypeRanges = new Dictionary<WorldObjectType, UIntRange>();
            foreach (WorldObjectType wot in Enum.GetValues(typeof(WorldObjectType)))
            {
                //Skip unset.
                if (wot == WorldObjectType.Unset)
                    continue;

                uint minimum = (MINIMUM_VALUE + ((byte)wot * END_INDEX_MULTIPLIER));
                uint maximum = (minimum + END_INDEX_MULTIPLIER);
                _objectTypeRanges[wot] = new UIntRange(minimum, maximum);
            }
        }

        /// <summary>
        /// Returns the WorldObjectType for a value.
        /// </summary>
        public static WorldObjectType GetWorldObjectType(uint value)
        {
            TryBuildObjectTypeRanges();

            foreach (KeyValuePair<WorldObjectType, UIntRange> item in _objectTypeRanges)
            {
                if (value >= item.Value.Minimum && value <= item.Value.Maximum)
                    return item.Key;
            }

            //Fallthrough.
            return WorldObjectType.Unset;
        }

        /// <summary>
        /// Gets the end value to use for a WorldObjectType.
        /// </summary>
        public static uint GetEndValue(WorldObjectType wot)
        {
            TryBuildObjectTypeRanges();

            if (_objectTypeRanges.TryGetValue(wot, out UIntRange range))
                return range.Maximum;
            else
                return (byte)WorldObjectType.Unset;
        }
        /// <summary>
        /// Gets the start value to use for a WorldObjectType.
        /// </summary>
        public static uint GetStartValue(WorldObjectType wot)
        {
            TryBuildObjectTypeRanges();

            if (_objectTypeRanges.TryGetValue(wot, out UIntRange range))
                return range.Minimum;
            else
                return (byte)WorldObjectType.Unset;
        }
    }


}
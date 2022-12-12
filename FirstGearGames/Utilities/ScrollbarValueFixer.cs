using UnityEngine;
using UnityEngine.UI;

namespace FirstGearGames.Utilities.Objects
{



    public class ScrollbarValueFixer
    {
        /// <summary>
        /// Scrollbar to fix.
        /// </summary>
        private Scrollbar _scrollBar;
        /// <summary>
        /// Value to set scrollbar at.
        /// </summary>
        private float _value;
        /// <summary>
        /// Frame when value was updated.
        /// </summary>
        private int _updatedFrame = -1;
        /// <summary>
        /// Number of frames to wait before fixing.
        /// </summary>
        private int _fixFrames;

        public ScrollbarValueFixer(Scrollbar sb, int fixFrames = 2)
        {
            _scrollBar = sb;
            _fixFrames = fixFrames;
        }

        /// <summary>
        /// Sets value of the scrollbar.
        /// </summary>
        /// <param name="value"></param>
        public void SetValue(float value)
        {
            _scrollBar.value = value;
            _value = value;
            _updatedFrame = Time.frameCount;
        }

        /// <summary>
        /// Checks to fix scrollbar value.
        /// </summary>
        public void Fix()
        {
            if (_updatedFrame == -1)
                return;
            if ((Time.frameCount - _updatedFrame) < _fixFrames)
                return;

            _updatedFrame = -1;
            _scrollBar.value = _value;
        }
    }


}

using System;
using UnityEngine.UI;

namespace GameKit.Utilities.FloatingOptionMenus
{
    public class ImageButtonData : ButtonData, IDisposable
    {
        #region Public.
        /// <summary>
        /// Image to display.
        /// </summary>
        public Image DisplayImage { get; protected set; } = null;
        #endregion

        /// <summary>
        /// Initializes this for use.
        /// </summary>
        /// <param name="image">Image to use on the button.</param>
        /// <param name="text">Text to display on the button.</param>
        /// <param name="callback">Callback when OnPressed is called.</param>
        /// <param name="key">Optional key to include within the callback.</param>
        public void Initialize(Image image, string text, PressedDelegate callback, string key = "")
        {
            base.Initialize(text, callback, key);
            DisplayImage = image;
        }

        public override void Dispose()
        {
            base.Dispose();
            DisplayImage = null;
        }

    }


}
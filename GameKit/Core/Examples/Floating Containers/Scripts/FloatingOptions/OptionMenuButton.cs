using GameKit.Utilities.Types.CanvasContainers;
using TMPro;
using UnityEngine;

namespace GameKit.Utilities.Types.OptionMenuButtons
{
    public class OptionMenuButton : MonoBehaviour
    {
        #region Public.
        /// <summary>
        /// ButtonData for this button.
        /// </summary>
        public ButtonData ButtonData { get; private set; }
        #endregion

        #region Serialized.
        /// <summary>
        /// Textbox to show buttonData text.
        /// </summary>
        [Tooltip("Textbox to show buttonData text.")]
        [SerializeField]
        private TextMeshProUGUI _text;
        #endregion

        public virtual void Initialize(ButtonData bd)
        {
            ButtonData = bd;
            _text.text = bd.Text;
        }

        /// <summary>
        /// To be called when the button is pressed.
        /// </summary>
        public virtual void OnPressed()
        {
            ButtonData?.OnPressed();
        }
    }

}
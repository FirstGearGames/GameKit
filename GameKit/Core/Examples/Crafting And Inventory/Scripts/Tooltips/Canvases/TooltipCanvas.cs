using UnityEngine;
using TMPro;
using FishNet;
using GameKit.Examples.Managers;
using TriInspector;
using GameKit.Utilities.Types;
using GameKit.Utilities;
using GameKit.Utilities.Types.CanvasContainers;

namespace GameKit.Examples.Tooltips.Canvases
{

    [DeclareFoldoutGroup("Components")]
    [DeclareFoldoutGroup("Sizing")]
    public class TooltipCanvas : MonoBehaviour
    {
        #region Serialized.
        /// <summary>
        /// Container to show the tooltip.
        /// </summary>
        [Tooltip("Container to show the tooltip.")]
        [SerializeField, Group("Components")]
        ResizableContainer _container;
        /// <summary>
        /// TextMeshPro to show tooltip text.
        /// </summary>
        [PropertyTooltip("TextMeshPro to show tooltip text.")]
        [SerializeField, Group("Components")]
        private TextMeshProUGUI _text;
        #endregion

        #region Private.
        /// <summary>
        /// Object calling Show.
        /// </summary>
        private object _caller;
        /// <summary>
        /// CanvasManager to use.
        /// </summary>
        private CanvasManager _canvasManager;
        #endregion

        private void Awake()
        {
            InitializeOnce();
        }

        private void InitializeOnce()
        {
            _canvasManager = InstanceFinder.NetworkManager.GetInstance<CanvasManager>();
            _canvasManager.TooltipCanvas = this;
        }

        /// <summary>
        /// Shows this canvas.
        /// </summary>
        /// <param name="text">Text to use.</param>
        public void Show(object caller, Vector2 position, string text, Vector2 pivot)
        {
            if (string.IsNullOrWhiteSpace(text))
                return;

            _caller = caller;
            _text.text = text;


            _container.UpdatePosition(position, true);
            _container.UpdatePivot(pivot, false);

            FloatRange2D sizeLimits = _container.SizeLimits;
            /* Set the rect of the text to maximum size and change anchoring. This will ensure it will
             * always be one size regardless of parent transforms. This is required because
             * Text.GetPreferredValues() returns differently depending on the last size of the Text
             * component, even if the containing string value is the same. This is surely a Unity bug
             * but I've found no other way around it then what is being done below. */
            Vector2 anchorOverride = new Vector2(0.5f, 0.5f);
            _text.rectTransform.anchorMin = anchorOverride;
            _text.rectTransform.anchorMax = anchorOverride;
            _text.rectTransform.sizeDelta = new Vector2(sizeLimits.X.Maximum, sizeLimits.Y.Maximum);
            //Always use word wrap otherwise text will overflow.
            _text.enableWordWrapping = true;

            _container.SetSizeAndShow(_text.GetPreferredValues());
        }

        /// <summary>
        /// Hides this canvas.
        /// </summary>
        public void Hide()
        {
            _caller = null;
            _container.Hide();
        }

        /// <summary>
        /// Hides this canvas if caller is the current one showing the canvas.
        /// </summary>
        /// <param name="caller">Object calling hide.</param>
        public void Hide(object caller)
        {
            if (_caller != caller)
                return;
            Hide();
        }

    }


}
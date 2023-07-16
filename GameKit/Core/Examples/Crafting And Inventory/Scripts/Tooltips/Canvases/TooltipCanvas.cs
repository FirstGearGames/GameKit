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
            //Calculate needed size.
            float widthRequired = _text.GetPreferredValues().x;
            //Set wrapping based on if width exceeds maximum width.
            _text.enableWordWrapping = (widthRequired > sizeLimits.X.Maximum);

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

        ///// <summary>
        ///// Resizes transform based on bag slots.
        ///// </summary>
        //private void ResizeAndShow(bool complete, Vector3 position)
        //{
        //    Vector2 preferredValues = _text.GetPreferredValues();
        //    float widthRequired = preferredValues.x;
        //    //Set wrapping based on if width exceeds maximum width.
        //    _text.enableWordWrapping = (widthRequired > _size.X.Maximum);
        //    float heightRequired = preferredValues.y;
        //    //Clamp width and height.
        //    widthRequired = Mathf.Clamp(widthRequired, _size.X.Minimum, _size.X.Maximum);
        //    heightRequired = Mathf.Clamp(heightRequired, _size.Y.Minimum, _size.Y.Maximum);
        //    _rectTransform.sizeDelta = new Vector2(widthRequired, heightRequired);

        //    _rectTransform.position = _rectTransform.GetOnScreenPosition(position, Constants.FLOATING_CANVAS_EDGE_PADDING);

        //    if (complete)
        //        _canvasGroup.SetActive(CanvasGroupBlockingType.Unchanged, 0.9f);
        //}

    }


}
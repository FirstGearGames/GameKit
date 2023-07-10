using UnityEngine;
using TMPro;
using FishNet;
using GameKit.Examples.Managers;
using TriInspector;
using GameKit.Utilities.Types;
using GameKit.Utilities;

namespace GameKit.Examples.Tooltips.Canvases
{

    [DeclareFoldoutGroup("Components")]
    [DeclareFoldoutGroup("Sizing")]
    public class TooltipCanvas : MonoBehaviour
    {
        #region Serialized.
        /// <summary>
        /// CanvasGroup to show and hide this canvas.
        /// </summary>
        [Tooltip("CanvasGroup to show and hide this canvas.")]
        [SerializeField, Group("Components")]
        private CanvasGroup _canvasGroup;
        /// <summary>
        /// RectTransform to resize with tooltip.
        /// </summary>
        [Tooltip("RectTransform to resize with tooltip.")]
        [SerializeField, Group("Components")]
        private RectTransform _rectTransform;
        /// <summary>
        /// TextMeshPro to show tooltip text.
        /// </summary>
        [PropertyTooltip("TextMeshPro to show tooltip text.")]
        [SerializeField, Group("Components")]
        private TextMeshProUGUI _text;

        /// <summary>
        /// Minimum and maximum range for the width of RectTransform.
        /// </summary>
        [PropertyTooltip("Minimum and maximum range for the width of RectTransform.")]
        [SerializeField, Group("Sizing")]
        private FloatRange _width = new FloatRange(300, 1400);
        /// <summary>
        /// Minimum and maximum range for the height of RectTransform.
        /// </summary>
        [PropertyTooltip("Minimum and maximum range for the height of RectTransform.")]
        [SerializeField, Group("Sizing")]
        private FloatRange _height = new FloatRange(100, 800);
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

            _rectTransform.pivot = pivot;
            _caller = caller;
            _text.text = text;
            CanvasManager.ResizeDelegate del = new CanvasManager.ResizeDelegate(() => ResizeAndShow(position));
            _canvasManager.Resize(del);
        }

        /// <summary>
        /// Hides this canvas.
        /// </summary>
        public void Hide()
        {
            _caller = null;
            _canvasGroup.SetActive(CanvasGroupBlockingType.Unchanged, 0f);
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

        /// <summary>
        /// Resizes transform based on bag slots.
        /// </summary>
        private void ResizeAndShow(Vector3 position)
        {
            Vector2 preferredValues = _text.GetPreferredValues();
            float widthRequired = preferredValues.x;
            //Set wrapping based on if width exceeds maximum width.
            _text.enableWordWrapping = (widthRequired > _width.Maximum);
            float heightRequired = preferredValues.y;
            //Clamp width and height.
            widthRequired = Mathf.Clamp(widthRequired, _width.Minimum, _width.Maximum);
            heightRequired = Mathf.Clamp(heightRequired, _height.Minimum, _height.Maximum);
            _rectTransform.sizeDelta = new Vector2(widthRequired, heightRequired);

            _rectTransform.position = _rectTransform.GetOnScreenPosition(position, Constants.FLOATING_CANVAS_EDGE_PADDING);
            _canvasGroup.SetActive(CanvasGroupBlockingType.Unchanged, 0.9f);
        }

    }


}
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
        /// Ideal position for tooltip.
        /// </summary>
        private Vector2 _desiredPosition;
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
            _canvasManager.Resize(new CanvasManager.ResizeDelegate(Resize));
            _desiredPosition = position;
            _canvasGroup.SetActive(true, 0.9f);
        }

        /// <summary>
        /// Hides this canvas.
        /// </summary>
        public void Hide()
        {
            _caller = null;
            _canvasGroup.SetActive(false, true);
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
        private void Resize()
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

            //Value of which the tooltip would exceed screen bounds.
            //If there would be overshoot then adjust to be just on the edge of the overshooting side.
            float overshoot;

            float halfWidthRequired = (widthRequired / 2f);
            overshoot = (Screen.width - (_desiredPosition.x + halfWidthRequired));
            //If overshooting on the right.
            if (overshoot < 0f)
                _desiredPosition.x += overshoot;
            overshoot = (_desiredPosition.x - halfWidthRequired);
            //If overshooting on the left.
            if (overshoot < 0f)
                _desiredPosition.x = halfWidthRequired;

            float halfHeightRequired = (heightRequired / 2f);
            overshoot = (Screen.height - (_desiredPosition.y + halfHeightRequired));
            //If overshooting on the right.
            if (overshoot < 0f)
                _desiredPosition.y += overshoot;
            overshoot = (_desiredPosition.y - halfHeightRequired);
            //If overshooting on the left.
            if (overshoot < 0f)
                _desiredPosition.y = halfHeightRequired;

            _rectTransform.position = _desiredPosition;


            //Debug.Log(screenX + ",  " + _desiredPosition.x + ",  " + widthRequired);
        }

    }


}
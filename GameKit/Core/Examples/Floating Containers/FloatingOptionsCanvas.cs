using FishNet;
using GameKit.Examples;
using GameKit.Examples.Managers;
using GameKit.Utilities.Types.CanvasContainers;
using System.Collections.Generic;
using System.Runtime.CompilerServices;
using TriInspector;
using UnityEngine;


namespace GameKit.Utilities.Types.OptionMenuButtons
{

    public class FloatingOptionsCanvas : FloatingOptions
    {
        #region Serialized.
        /// <summary>
        /// RectTransform to resize to fit buttons.
        /// </summary>
        [Tooltip("RectTransform to resize to fit buttons.")]
        [SerializeField, Group("Components")]
        private RectTransform _rectTransform;
        /// <summary>
        /// If not null the size of padding will be considered when resizing.
        /// </summary>
        [Tooltip("If not null the size of padding will be considered when resizing.")]
        [SerializeField, Group("Components")]
        private RectTransform _paddingTransform;
        /// <summary>
        /// Transform to add buttons to.
        /// </summary>
        [Tooltip("Transform to add buttons to.")]
        [SerializeField, Group("Components")]
        private RectTransform _content;

        /// <summary>
        /// Default prefab to use for each button.
        /// </summary>
        [Tooltip("Default prefab to use for each button.")]
        [SerializeField, Group("Buttons")]
        private OptionMenuButton _buttonPrefab;

        /// <summary>
        /// Maximum width and height of the RectTransform. Resizing will occur based on the number of buttons and their sizes but will stay within this range.
        /// </summary>
        [Tooltip("Maximum width and height of the RectTransform. Resizing will occur based on the number of buttons and their sizes but will stay within this range.")]
        [SerializeField, Group("Sizing")]
        private Vector2 _size = new Vector2(1400f, 800f);
        #endregion

        #region Private.
        /// <summary>
        /// CanvasManager for instance.
        /// </summary>
        private CanvasManager _canvasManager;
        /// <summary>
        /// Preferred position of the canvas.
        /// </summary>
        private Vector3 _desiredPosition;
        /// <summary>
        /// Button prefab to use. If not null this will be used instead of the default button prefab.
        /// </summary>
        private OptionMenuButton _desiredButtonPrefab;
        #endregion

        private void Awake()
        {
            ClientInstance.OnClientChange += ClientInstance_OnClientChange;
            ClientInstance_OnClientChange(ClientInstance.Instance, true);
        }

        protected override void Update()
        {
            base.Update();
            if (Time.frameCount % 3 == 0)
                RectTransformResizer.Resize(new RectTransformResizer.ResizeDelegate(ResizeAndShow));

        }
        /// <summary>
        /// Called when a ClientInstance runs OnStop or OnStartClient.
        /// </summary>
        private void ClientInstance_OnClientChange(ClientInstance instance, bool started)
        {
            if (instance == null)
                return;
            //Do not do anything if this is not the instance owned by local client.
            if (!instance.IsOwner)
                return;

            if (started)
                _canvasManager = instance.NetworkManager.GetInstance<CanvasManager>();

            RectTransformResizer.Resize(new RectTransformResizer.ResizeDelegate(ResizeAndShow));
        }

        /// <summary>
        /// Shows the canvas.
        /// </summary>
        /// <param name="clearExisting">True to clear existing buttons first.</param>
        /// <param name="position">Position of canvas.</param>
        /// <param name="buttonPrefab">Button prefab to use. If null DefaultButtonPrefab will be used.</param>
        /// <param name="buttonDatas">Datas to use.</param>
        public virtual void Show(bool clearExisting, Vector2 position, IEnumerable<ButtonData> buttonDatas, GameObject buttonPrefab = null)
        {
            _desiredPosition = position;
            //Remove all current buttons then add new ones.
            RemoveButtons();
            AddButtons(true, buttonDatas);
            //Begin resize.
            RectTransformResizer.Resize(new RectTransformResizer.ResizeDelegate(ResizeAndShow));
        }

        /// <param name="clearExisting">True to clear existing buttons first.</param>
        /// <param name="startingPoint">Transform to use as the position.</param>
        /// <param name="buttonPrefab">Button prefab to use. If null DefaultButtonPrefab will be used.</param>
        /// <param name="buttonDatas">Datas to use.</param>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public virtual void Show(bool clearExisting, Transform startingPoint, IEnumerable<ButtonData> buttonDatas, GameObject buttonPrefab = null)
        {
            if (startingPoint == null)
            {
                InstanceFinder.NetworkManager.LogError($"A null Transform cannot be used as the starting point.");
                return;
            }

            Show(clearExisting, startingPoint.position, buttonDatas, buttonPrefab);
        }

        /// <summary>
        /// Adds buttons.
        /// </summary>
        /// <param name="clearExisting">True to clear existing buttons first.</param>
        /// <param name="buttonDatas">Buttons to add.</param>
        protected override void AddButtons(bool clearExisting, IEnumerable<ButtonData> buttonDatas)
        {
            base.AddButtons(clearExisting, buttonDatas);
            RectTransformResizer.Resize(new RectTransformResizer.ResizeDelegate(ResizeAndShow));
        }

        /// <summary>
        /// Resizes based on button and header.
        /// </summary>
        private void ResizeAndShow(bool complete)
        {
            Vector2 buttonSize;
            GameObject button = (_desiredButtonPrefab == null) ? _buttonPrefab.gameObject : _desiredButtonPrefab.gameObject;
            RectTransform buttonRt = _desiredButtonPrefab.GetComponent<RectTransform>();
            if (buttonRt == null)
            {
                _canvasManager.NetworkManager.LogWarning($"Button prefab {button.name} does not contain a rectTransform on it's root object. Resizing cannot occur.");
                return;
            }
            buttonSize = buttonRt.sizeDelta;
            //Start minimum size to that of the button.
            Vector2 minimumSize = Vector2.zero;

            Vector2 padding = Vector2.zero;
            //If to add on padding.
            if (_paddingTransform != null)
            {
                padding = new Vector2(
                    Mathf.Abs(_paddingTransform.offsetMax.x) + Mathf.Abs(_paddingTransform.offsetMin.x),
                    Mathf.Abs(_paddingTransform.offsetMax.y) + Mathf.Abs(_paddingTransform.offsetMin.y)
                    );
            }
            //Add padding onto minimum size.
            minimumSize += padding;

            /* Maximum size available for buttons. This excludes
             * the padding if padding is used. */
            Vector2 maximumSizeWithoutPadding = (_size - padding);
            if (maximumSizeWithoutPadding.x <= 0f || maximumSizeWithoutPadding.y <= 0f)
            {
                _canvasManager.NetworkManager.LogError($"Maximum size is less than 0f on at least one axes. Resize cannot occur.");
                return;
            }

            /* Check the content transform for the layout group type.
             * EG: if vertical layout group ...
             * 
             * --- Padding needed to fit all buttons...
             * float layoutPaddingRequired = (buttonCount - 1) * layoutPadding.
             * --- Number of buttons which can fit into maximumSize while including the layout group padding.
             * int possibleButtons = Mathf.FloorToInt((maximumSizeWithoutPadding - layoutPaddingRequired) / buttonCount);
             * --- Vertical size needed to include possible buttons and their layout group padding.
             * Needed vertical size would then be (possibleButtons * buttonHeight) + ((possibleButtons - 1) * layoutPadding.
             *
             * Once this is known the rectTransform can be resized to the calculated value and position set. */

            _rectTransform.position = _rectTransform.GetOnScreenPosition(_desiredPosition, Constants.FLOATING_CANVAS_EDGE_PADDING);

            if (complete)
                base.Show();
        }

    }


}
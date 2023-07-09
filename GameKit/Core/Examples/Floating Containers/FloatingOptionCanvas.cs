using FishNet;
using GameKit.Examples;
using GameKit.Examples.Managers;
using GameKit.Utilities.Types.CanvasContainers;
using System.Runtime.CompilerServices;
using UnityEngine;


namespace GameKit.Utilities.Types.OptionMenuButtons
{

    public class FloatingOptionCanvas : FloatingOptions
    {
        #region Serialized.
        /// <summary>
        /// True to limit the maximum resize height of the canvas to starting values.
        /// </summary>
        [Tooltip("True to limit the maximum resize height of the canvas to starting values.")]
        [SerializeField]
        private bool _limitMaximumResizeHeight = true;
        /// <summary>
        /// RectTransform to resize to fit buttons.
        /// </summary>
        [Tooltip("RectTransform to resize to fit buttons.")]
        [SerializeField]
        private RectTransform _rectTransform;
        /// <summary>
        /// If not null the size of padding will be considered when resizing.
        /// </summary>
        [Tooltip("If not null the size of padding will be considered when resizing.")]
        [SerializeField]
        private RectTransform _paddingTransform;
        /// <summary>
        /// Prefab to use for each button.
        /// </summary>
        [Tooltip("Prefab to use for each button.")]
        [SerializeField]
        private OptionMenuButton _buttonPrefab;
        /// <summary>
        /// Transform to add buttons to.
        /// </summary>
        [Tooltip("Transform to add buttons to.")]
        [SerializeField]
        private RectTransform _content;
        #endregion

        #region Private.
        /// <summary>
        /// CanvasManager for instance.
        /// </summary>
        private CanvasManager _canvasManager;
        /// <summary>
        /// Starting height of RectTransform.
        /// </summary>
        private float _startingHeight;
        #endregion

        private void Awake()
        {
            _startingHeight = _rectTransform.sizeDelta.y;
            ClientInstance.OnClientChange += ClientInstance_OnClientChange;
            ClientInstance_OnClientChange(ClientInstance.Instance, true);
        }

        protected override void Update()
        {
            base.Update();
            if (Time.frameCount % 3 == 0)
                _canvasManager?.Resize(new CanvasManager.ResizeDelegate(Resize));

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

            _canvasManager?.Resize(new CanvasManager.ResizeDelegate(Resize));
        }

        /// <summary>
        /// Shows the canvas.
        /// </summary>
        /// <param name="clearExisting">True to clear existing buttons first.</param>
        /// <param name="position">Position of canvas.</param>
        /// <param name="rotation">Rotation of canvas.</param>
        /// <param name="scale">Scale of canvas.</param>
        /// <param name="buttonDatas">Datas to use.</param>
        public virtual void Show(bool clearExisting, Vector3 position, Quaternion rotation, Vector3 scale, params ButtonData[] buttonDatas)
        {
            base.Show();
            transform.SetPositionAndRotation(position, rotation);
            transform.localScale = scale;
            //Remove all current buttons then add new ones.
            RemoveButtons();
            AddButtons(true, buttonDatas);
        }
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public virtual void Show(bool clearExisting, Vector3 position, params ButtonData[] buttonDatas)
        {
            Show(clearExisting, position, Quaternion.identity, Vector3.one, buttonDatas);
        }
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public virtual void Show(bool clearExisting, Vector3 position, Quaternion rotation, params ButtonData[] buttonDatas)
        {
            Show(clearExisting, position, rotation, Vector3.one, buttonDatas);
        }
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public virtual void Show(bool clearExisting, Transform startingPoint, params ButtonData[] buttonDatas)
        {
            if (startingPoint == null)
            {
                InstanceFinder.NetworkManager.LogError($"A null Transform cannot be used as the starting point.");
                return;
            }

            Show(clearExisting, startingPoint.position, startingPoint.rotation, startingPoint.localScale, buttonDatas);
        }

        /// <summary>
        /// Adds buttons.
        /// </summary>
        /// <param name="clearExisting">True to clear existing buttons first.</param>
        /// <param name="buttonDatas">Buttons to add.</param>
        protected override void AddButtons(bool clearExisting, params ButtonData[] buttonDatas)
        {
            base.AddButtons(clearExisting, buttonDatas);
            _canvasManager.Resize(new CanvasManager.ResizeDelegate(Resize));
        }

        /// <summary>
        /// Resizes based on button and header.
        /// </summary>
        private void Resize()
        {
            float contentHeight = _content.sizeDelta.y;
            //If to add on padding.
            if (_paddingTransform != null)
            {
                float verticalPadding = (Mathf.Abs(_paddingTransform.offsetMax.y) + Mathf.Abs(_paddingTransform.offsetMin.y));
                contentHeight += verticalPadding;
                if (_limitMaximumResizeHeight)
                    contentHeight = Mathf.Min(_startingHeight, contentHeight);
            }

            _rectTransform.sizeDelta = new Vector2(_rectTransform.sizeDelta.x, contentHeight);
        }

    }


}
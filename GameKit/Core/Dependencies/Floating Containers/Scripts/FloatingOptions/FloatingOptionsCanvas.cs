using GameKit.Dependencies.Utilities.Types.CanvasContainers;
using System.Collections.Generic;
using System.Runtime.CompilerServices;
using UnityEngine;
using GameKit.Core.Dependencies;
using GameKit.Dependencies.Utilities.Types.OptionMenuButtons;
using GameKit.Dependencies.Utilities;
using UnityEngine.UI;
using FishNet.Managing;
using Sirenix.OdinInspector;

namespace GameKit.Core.FloatingContainers.OptionMenuButtons
{
    public class FloatingOptionsCanvas : FloatingOptions
    {
        #region Serialized.
        [Header("FloatingOptionsCanvas")]
        /// <summary>
        /// Image used to close this canvas.
        /// </summary>
        [Tooltip("RectTransform to resize to fit buttons.")] [SerializeField, Indent(1), TabGroup("Components")]
        private GameObject _closeButton;

        /// <summary>
        /// RectTransform to resize to fit buttons.
        /// </summary>
        [Tooltip("RectTransform to resize to fit buttons.")] [SerializeField,Indent(1),  TabGroup("Components")]
        private RectTransform _rectTransform;

        /// <summary>
        /// If not null the size of padding will be considered when resizing.
        /// </summary>
        [Tooltip("If not null the size of padding will be considered when resizing.")]
        [SerializeField, Indent(1), TabGroup("Components")]
        private RectTransform _paddingTransform;

        /// <summary>
        /// Transform to add buttons to.
        /// </summary>
        [Tooltip("Transform to add buttons to.")] [SerializeField, Indent(1), TabGroup("Components")]
        private RectTransform _content;

        /// <summary>
        /// LayoutGroup used to hold objects. If left null this will be automatically from any LayoutGroup on the Content transform.
        /// </summary>
        [SerializeField, Indent(1), TabGroup("Components")]
        public LayoutGroup LayoutGroup;

        [Header("FloatingOptionsCanvas")]
        /// <summary>
        /// Default prefab to use for each button.
        /// </summary>
        [Header("Buttons")]
        [Tooltip("Default prefab to use for each button.")] [SerializeField, Indent(1), TabGroup("Misc")]
        private OptionMenuButton _buttonPrefab;

        /// <summary>
        /// Maximum width and height of the RectTransform. Resizing will occur based on the number of buttons and their sizes but will stay within this range.
        /// </summary>
        [Tooltip(
            "Maximum width and height of the RectTransform. Resizing will occur based on the number of buttons and their sizes but will stay within this range.")]
        [Header("Sizing")]
        [SerializeField, Indent(1), TabGroup("Misc")]
        private Vector2 _sizeLimits = new Vector2(1400f, 800f);

        #endregion

        #region Private.

        /// <summary>
        /// Preferred position of the canvas.
        /// </summary>
        private Vector3 _desiredPosition;

        /// <summary>
        /// Button prefab to use. If not null this will be used instead of the default button prefab.
        /// </summary>
        private OptionMenuButton _buttonPrefabOverride;

        /// <summary>
        /// ClientInstance for the local client.
        /// </summary>
        private ClientInstance _clientInstance;

        /// <summary>
        /// Called when cancel or close is pressed.
        /// </summary>
        private PressedDelegateData _cancelCallback;

        #endregion

        private void Awake()
        {
            if (LayoutGroup == null)
            {
                if (!_content.TryGetComponent<LayoutGroup>(out LayoutGroup))
                    Debug.LogError(
                        $"LayoutGroup was not specified and one does not exist on the content transform {_content.name}.");
            }

            ClientInstance.OnClientInstanceChangeInvoke(
                new ClientInstance.ClientInstanceChangeDel(ClientInstance_OnClientInstanceChange), false);
        }

        private void OnDestroy()
        {
            ClientInstance.OnClientInstanceChange -= ClientInstance_OnClientInstanceChange;
        }

        /// <summary>
        /// Called when a ClientInstance runs OnStop or OnStartClient.
        /// </summary>
        private void ClientInstance_OnClientInstanceChange(ClientInstance instance, ClientInstanceState state,
            bool asServer)
        {
            if (asServer)
                return;
            if (instance == null)
                return;
            //Do not do anything if this is not the instance owned by local client.
            if (!instance.IsOwner)
                return;

            if (state == ClientInstanceState.PreInitialize)
                instance.NetworkManager.RegisterInstance<FloatingOptionsCanvas>(this);
            else if (state == ClientInstanceState.PostInitialize)
                _clientInstance = instance;
        }

        /// <summary>
        /// Shows the canvas.
        /// </summary>
        /// <param name="clearExisting">True to clear existing buttons first.</param>
        /// <param name="position">Position of canvas.</param>
        /// <param name="buttonPrefab">Button prefab to use. If null DefaultButtonPrefab will be used.</param>
        /// <param name="buttonDatas">Datas to use.</param>
        public virtual void Show(bool clearExisting, Vector2 position, IEnumerable<ButtonData> buttonDatas,
            FloatingOptionsConfig config, GameObject buttonPrefab = null)
        {
            if (clearExisting)
                RemoveButtons();

            _closeButton.SetActive(config.AllowCancel);
            _cancelCallback = config.CancelCallback;

            _desiredPosition = position;
            AddButtons(true, buttonDatas);
            //Begin resize.
            ResizeAndShow();
        }

        /// <param name="clearExisting">True to clear existing buttons first.</param>
        /// <param name="startingPoint">Transform to use as the position.</param>
        /// <param name="buttonPrefab">Button prefab to use. If null DefaultButtonPrefab will be used.</param>
        /// <param name="buttonDatas">Datas to use.</param>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public virtual void Show(bool clearExisting, Transform startingPoint, IEnumerable<ButtonData> buttonDatas,
            FloatingOptionsConfig config, GameObject buttonPrefab = null)
        {
            if (startingPoint == null)
            {
                NetworkManagerExtensions.LogError($"A null Transform cannot be used as the starting point.");
                return;
            }

            Show(clearExisting, startingPoint.position, buttonDatas, config, buttonPrefab);
        }

        /// <summary>
        /// Resizes based on button and header.
        /// </summary>
        private void ResizeAndShow()
        {
            //Clear buttons.
            _content.DestroyChildren(true);
            Vector2 buttonSize;
            OptionMenuButton button = (_buttonPrefabOverride == null) ? _buttonPrefab : _buttonPrefabOverride;
            RectTransform buttonRt = button.GetComponent<RectTransform>();
            if (buttonRt == null)
            {
                _clientInstance.NetworkManager.LogWarning(
                    $"Button prefab {button.name} does not contain a rectTransform on it's root object. Resizing cannot occur.");
                return;
            }

            buttonSize = buttonRt.sizeDelta;

            Vector2 padding = Vector2.zero;
            //If to add on padding.
            if (_paddingTransform != null)
            {
                padding = new Vector2(
                    Mathf.Abs(_paddingTransform.offsetMax.x) + Mathf.Abs(_paddingTransform.offsetMin.x),
                    Mathf.Abs(_paddingTransform.offsetMax.y) + Mathf.Abs(_paddingTransform.offsetMin.y)
                );
            }

            /* Maximum size available for buttons. This excludes
             * the padding if padding is used. */
            Vector2 maximumSizeAfterPadding = (_sizeLimits - padding);
            if (maximumSizeAfterPadding.x <= 0f || maximumSizeAfterPadding.y <= 0f)
            {
                _clientInstance.NetworkManager.LogError(
                    $"Maximum size is less than 0f on at least one axes. Resize cannot occur.");
                return;
            }

            Vector2 resizeValue = Vector2.zero;
            int buttonCount = base.Buttons.Count;
            //VerticalLayoutGroup
            if (LayoutGroup is VerticalLayoutGroup vlg)
            {
                SetResizeByVertical(vlg.spacing);
            }
            //HorizontalLayoutGroup
            else if (LayoutGroup is HorizontalLayoutGroup hlg)
            {
                SetResizeByHorizontal(hlg.spacing);
            }
            //GridLayoutGroup.
            else if (LayoutGroup is GridLayoutGroup glg)
            {
                /* If buttonCount is less than fixed count
                 * then change the resizeValue to accomodate
                 * the buttonCount on the fixed axis. */
                bool buttonCountUnderRestraint = (buttonCount < glg.constraintCount);

                if (glg.constraint == GridLayoutGroup.Constraint.FixedRowCount)
                {
                    SetResizeByVertical(glg.spacing.y);
                    if (buttonCountUnderRestraint)
                        resizeValue.x = (buttonSize.x * buttonCount) + (glg.spacing.x * (buttonCount - 1));
                }
                else if (glg.constraint == GridLayoutGroup.Constraint.FixedColumnCount)
                {
                    SetResizeByHorizontal(glg.spacing.x);
                    if (buttonCountUnderRestraint)
                        resizeValue.y = (buttonSize.y * buttonCount) + (glg.spacing.y * (buttonCount - 1));
                }
                else
                {
                    _clientInstance.NetworkManager.LogError(
                        $"GameObject {gameObject.name} GroupLayoutGroup must have a fixed constaint. You can modify the LayoutGroup's settings as runtime by accessing the LayoutGroup property.");
                }
            }
            else
            {
                _clientInstance.NetworkManager.LogError(
                    $"GameObject {gameObject.name} LayoutGroup is of an unsupported type {LayoutGroup.GetType().Name}. Resizing will fail.");
            }

            //Returns how much space is needed to accomodate passed in values.
            float GetSizeWithPadding(int fittingButtonCount, float buttonDelta, float spacing, float sizeLimit)
            {
                //If button count is 0 then return sizeLimit.
                if (fittingButtonCount <= 0)
                    return sizeLimit;

                float paddingRequired = (fittingButtonCount - 1) * spacing;
                return (fittingButtonCount * buttonDelta) + paddingRequired;
            }

            //Sets resizeValue by using resizable Y.
            void SetResizeByVertical(float spacing)
            {
                float buttonDelta = buttonSize.y;
                int fittingButtonCount =
                    Mathf.Min(buttonCount, Mathf.FloorToInt(maximumSizeAfterPadding.y / buttonDelta));
                //Size needed by fittingButtonCount and padding.
                float verticalNeeded = GetSizeWithPadding(fittingButtonCount, buttonDelta, spacing, fittingButtonCount);
                /* If at least one button can fit see how much padding
                 * would be needed to get all fittingButtonCounts in.
                 * If button size delta + padding would exceed maximum size
                 * then reduce the button count by 1, and recalculate
                 * size with padding. */
                if (verticalNeeded > maximumSizeAfterPadding.y)
                {
                    fittingButtonCount--;
                    verticalNeeded = GetSizeWithPadding(fittingButtonCount, buttonDelta, spacing, fittingButtonCount);
                }

                float horizontalNeeded = (buttonSize.x > maximumSizeAfterPadding.x)
                    ? maximumSizeAfterPadding.x
                    : buttonSize.x;
                resizeValue = new Vector2(horizontalNeeded, verticalNeeded);
            }

            //Sets resizeValue by using resizable X.
            void SetResizeByHorizontal(float spacing)
            {
                float buttonDelta = buttonSize.x;
                int fittingButtonCount =
                    Mathf.Min(buttonCount, Mathf.FloorToInt(maximumSizeAfterPadding.y / buttonDelta));

                float horizontalNeeded =
                    GetSizeWithPadding(fittingButtonCount, buttonDelta, spacing, fittingButtonCount);
                if (horizontalNeeded > maximumSizeAfterPadding.x)
                {
                    fittingButtonCount--;
                    horizontalNeeded = GetSizeWithPadding(fittingButtonCount, buttonDelta, spacing, fittingButtonCount);
                }

                float verticalNeeded = (buttonSize.y > maximumSizeAfterPadding.y)
                    ? maximumSizeAfterPadding.y
                    : buttonSize.y;
                resizeValue = new Vector2(horizontalNeeded, verticalNeeded);
            }

            //If able to resize.
            if (resizeValue.x > 0f && resizeValue.y > 0f)
            {
                //Add all the buttons!
                foreach (ButtonData bd in base.Buttons)
                {
                    OptionMenuButton omb = Instantiate(button, _content, false);
                    omb.Initialize(bd);
                }

                //Add padding onto resize value.
                resizeValue += padding;
                _rectTransform.sizeDelta = resizeValue;
                _rectTransform.position =
                    _rectTransform.GetOnScreenPosition(_desiredPosition, Constants.FLOATING_CANVAS_EDGE_PADDING);
                base.Show();
            }
            else
            {
                _clientInstance.NetworkManager.LogError(
                    $"Canvas cannot be resized because one or more axes is at 0f. This usually occurs when the selected button prefab has an invalid value for width or height.");
            }
        }

        /// <summary>
        /// Called when closed is pressed.
        /// </summary>
        public void OnClick_Close()
        {
            _cancelCallback.Invoke();
            ResetState();
        }

        private void ResetState()
        {
            _cancelCallback = default;
            _buttonPrefabOverride = default;
        }
    }
}
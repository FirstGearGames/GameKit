using System.Runtime.CompilerServices;
using UnityEngine;
using GameKit.Core.Dependencies;
using UnityEngine.UI;
using FishNet.Managing;
using GameKit.Core.Resources;
using Sirenix.OdinInspector;
using TMPro;
using GameKit.Dependencies.Utilities.Types;
using GameKit.Dependencies.Utilities;

namespace GameKit.Core.FloatingContainers.OptionMenuButtons
{
    public class SplittingCanvas : CanvasGroupFader
    {
        #region Serialized.
        [Header("SplittingCanvas")]
        /// <summary>
        /// Transform to move where canvas should be.
        /// </summary>
        [Tooltip("Transform to move where canvas should be.")]
        [SerializeField, Indent(1), TabGroup("Components")]
        private RectTransform _rectTransform;

        /// <summary>
        /// Image to show the item being split.
        /// </summary>
        [Tooltip("Image to show the item being split.")]
        [SerializeField, Indent(1), TabGroup("Components")]
        private Image _itemImage;

        /// <summary>
        /// Text showing the item name.
        /// </summary>
        [Tooltip("Text showing the item name.")]
        [SerializeField, Indent(1), TabGroup("Components")]
        private TextMeshProUGUI _itemText;

        /// <summary>
        /// Text showing how many to split of total.
        /// </summary>
        [Tooltip("Text showing how many to split of total.")]
        [SerializeField, Indent(1), TabGroup("Components")]
        private TextMeshProUGUI _splitText;

        /// <summary>
        /// Slider component.
        /// </summary>
        [Tooltip("Slider component.")]
        [SerializeField, Indent(1), TabGroup("Components")]
        private Slider _slider;

        /// <summary>
        /// Text showing how many remaining after the split.
        /// </summary>
        [Tooltip("Text showing how many remaining after the split.")]
        [SerializeField, Indent(1), TabGroup("Components")]
        private TextMeshProUGUI _remainingText;
        #endregion

        #region Private.
        /// <summary>
        /// ClientInstance for the local client.
        /// </summary>
        private ClientInstance _clientInstance;

        /// <summary>
        /// Configuration for the current split.
        /// </summary>
        private SplittingCanvasConfig _config;
        #endregion

        private void Awake()
        {
            _slider.onValueChanged.AddListener(OnSliderValueChange);
            ClientInstance.OnClientInstanceChangeInvoke(new ClientInstance.ClientInstanceChangeDel(ClientInstance_OnClientInstanceChange), false);
        }

        private void OnDestroy()
        {
            _slider.onValueChanged.RemoveListener(OnSliderValueChange);
            ClientInstance.OnClientInstanceChange -= ClientInstance_OnClientInstanceChange;
            if (_clientInstance != null)
                _clientInstance.NetworkManager.UnregisterInstance<SplittingCanvas>();
        }

        /// <summary>
        /// Called when a ClientInstance runs OnStop or OnStartClient.
        /// </summary>
        private void ClientInstance_OnClientInstanceChange(ClientInstance instance, ClientInstanceState state, bool asServer)
        {
            if (asServer)
                return;
            if (instance == null)
                return;
            //Do not do anything if this is not the instance owned by local client.
            if (!instance.IsOwner)
                return;

            if (state == ClientInstanceState.PreInitialize)
                instance.NetworkManager.RegisterInstance<SplittingCanvas>(this);
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
        public virtual void Show(Vector2 position, SplittingCanvasConfig config)
        {
            _config = config;

            //Slider min/max with values.
            _slider.minValue = 1;
            _slider.value = config.SplitValues.Minimum;
            _slider.maxValue = config.SplitValues.Maximum;

            //Update visuals.
            ResourceData rd = config.ResourceEntry.ResourceData;
            _itemImage.sprite = rd.Icon;
            _itemText.text = rd.DisplayName;

            OnSliderValueChange(_slider.value);

            _rectTransform.position = _rectTransform.GetOnScreenPosition(position, Constants.FLOATING_CANVAS_EDGE_PADDING);

            base.Show();
        }

        /// <param name="clearExisting">True to clear existing buttons first.</param>
        /// <param name="startingPoint">Transform to use as the position.</param>
        /// <param name="buttonPrefab">Button prefab to use. If null DefaultButtonPrefab will be used.</param>
        /// <param name="buttonDatas">Datas to use.</param>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public virtual void Show(Transform startingPoint, SplittingCanvasConfig config)
        {
            if (startingPoint == null)
            {
                NetworkManagerExtensions.LogError($"A null Transform cannot be used as the starting point.");
                return;
            }

            Show(startingPoint.position, config);
        }

        /// <summary>
        /// Hides while optionally resetting state.
        /// </summary>
        public void Hide(bool resetState = true)
        {
            if (resetState)
                ResetState();

            base.Hide();
        }

        /// <summary>
        /// Called when confirm is pressed.
        /// </summary>
        public void OnClick_Confirm()
        {
            if (_config.ConfirmedCallback != null)
            {
                int moveCount = (int)System.Math.Clamp(_slider.value, 0, _slider.maxValue);
                _config.ConfirmedCallback.Invoke(_config.ResourceEntry, moveCount);
            }
            Hide(resetState: true);
        }

        /// <summary>
        /// Called when close is pressed.
        /// </summary>
        public void OnClick_Close()
        {
            if (_config.CanceledCallback != null)
                _config.CanceledCallback.Invoke();
            Hide(resetState: true);
        }

        /// <summary>
        /// Called when the slider value changes.
        /// </summary>
        public void OnSliderValueChange(float value)
        {
            int currentValue = (int)value;
            int maxValue = (int)_slider.maxValue;

            int remaining = (maxValue - (int)value);
            _remainingText.text = $"{remaining} Remaining";
            _splitText.text = $"{currentValue} / {maxValue}";
        }

        private void ResetState()
        {
            _config = default;
        }
    }
}
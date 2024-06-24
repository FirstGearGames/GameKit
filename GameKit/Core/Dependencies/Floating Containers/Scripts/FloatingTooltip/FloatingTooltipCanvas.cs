using UnityEngine;
using TMPro;
using GameKit.Dependencies.Utilities.Types;
using GameKit.Dependencies.Utilities.Types.CanvasContainers;
using GameKit.Core.Dependencies;
using Sirenix.OdinInspector;
using FishNet.Managing;

namespace GameKit.Core.FloatingContainers.Tooltips
{

    public class FloatingTooltipCanvas : MonoBehaviour
    {
        #region Types.
        public enum TextAlignmentStyle
        {
            TopLeft,
            TopMiddle,
            TopRight,
            MiddleLeft,
            Middle,
            MiddleRight,
            BottomLeft,
            BottomMiddle,
            BottomRight,
        }
        #endregion

        #region Serialized.
        /// <summary>
        /// Container to show the tooltip.
        /// </summary>
        [Tooltip("Container to show the tooltip.")]
        [SerializeField, BoxGroup("Components")]
        ResizableContainer _container;
        /// <summary>
        /// TextMeshPro to show tooltip text.
        /// </summary>
        [Tooltip("TextMeshPro to show tooltip text.")]
        [SerializeField, BoxGroup("Components")]
        private TextMeshProUGUI _text;
        #endregion

        #region Private.
        /// <summary>
        /// Object calling Show.
        /// </summary>
        private object _caller;
        #endregion

        private void Awake()
        {
            ClientInstance.OnClientInstanceChangeInvoke(new ClientInstance.ClientInstanceChangeDel(ClientInstance_OnClientChange), false);
        }

        private void OnDestroy()
        {
            ClientInstance.OnClientInstanceChange -= ClientInstance_OnClientChange;
        }

        /// <summary>
        /// Called when a ClientInstance runs OnStop or OnStartClient.
        /// </summary>
        private void ClientInstance_OnClientChange(ClientInstance instance, ClientInstanceState state, bool asServer)
        {
            if (asServer)
                return;
            if (instance == null)
                return;
            //Do not do anything if this is not the instance owned by local client.
            if (!instance.IsOwner)
                return;

            if (state == ClientInstanceState.PreInitialize)
                instance.NetworkManager.RegisterInstance<FloatingTooltipCanvas>(this);
        }


        /// <summary>
        /// Shows this canvas.
        /// </summary>
        /// <param name="text">Text to use.</param>
        public void Show(object caller, Vector2 position, string text, Vector2 pivot, TextAlignmentStyle textAlignmentStyle)
        {
            if (string.IsNullOrWhiteSpace(text))
                return;

            //Min/Max for anchor, and pivot.
            Vector2 textAnchorMinMax_Pivot = Vector2.zero;
            TMPro.TextAlignmentOptions textAlignment = TextAlignmentOptions.Center;

            if (textAlignmentStyle == TextAlignmentStyle.TopLeft)
            {
                textAnchorMinMax_Pivot = new Vector2(0f, 1f);
                textAlignment = TextAlignmentOptions.TopLeft;
            }
            else if (textAlignmentStyle == TextAlignmentStyle.TopMiddle)
            {
                textAnchorMinMax_Pivot = new Vector2(0.5f, 1f);
                textAlignment = TextAlignmentOptions.Top;
            }
            else if (textAlignmentStyle == TextAlignmentStyle.TopRight)
            {
                textAnchorMinMax_Pivot = new Vector2(1f, 1f);
                textAlignment = TextAlignmentOptions.TopRight;
            }
            else if (textAlignmentStyle == TextAlignmentStyle.MiddleLeft)
            {
                textAnchorMinMax_Pivot = new Vector2(0f, 0.5f);
                textAlignment = TextAlignmentOptions.Left;
            }
            else if (textAlignmentStyle == TextAlignmentStyle.Middle)
            {
                textAnchorMinMax_Pivot = new Vector2(0.5f, 0.5f);
                textAlignment = TextAlignmentOptions.Center;
            }
            else if (textAlignmentStyle == TextAlignmentStyle.MiddleRight)
            {
                textAnchorMinMax_Pivot = new Vector2(1f, 0.5f);
                textAlignment = TextAlignmentOptions.Right;
            }
            else if (textAlignmentStyle == TextAlignmentStyle.BottomLeft)
            {
                textAnchorMinMax_Pivot = new Vector2(0f, 0f);
                textAlignment = TextAlignmentOptions.BottomLeft;
            }
            else if (textAlignmentStyle == TextAlignmentStyle.BottomMiddle)
            {
                textAnchorMinMax_Pivot = new Vector2(0.5f, 0f);
                textAlignment = TextAlignmentOptions.Bottom;
            }
            else if (textAlignmentStyle == TextAlignmentStyle.BottomRight)
            {
                textAnchorMinMax_Pivot = new Vector2(1f, 0f);
                textAlignment = TextAlignmentOptions.BottomRight;
            }
            else
            {
                NetworkManagerExtensions.LogError($"Unhandled {nameof(TextAlignmentStyle)} of {textAlignmentStyle}.");
            }

            _caller = caller;
            _text.text = text;

            _container.UpdatePosition(position, true);
            _container.UpdatePivot(pivot, false);

            FloatRange2D sizeLimits = _container.SizeLimits;
            RectTransform textRt = _text.rectTransform;
            /* Set the rect of the text to maximum size and change anchoring. This will ensure it will
             * always be one size regardless of parent transforms. This is required because
             * Text.GetPreferredValues() returns differently depending on the last size of the Text
             * component, even if the containing string value is the same. This is surely a Unity bug
             * but I've found no other way around it then what is being done below. */
            textRt.sizeDelta = new Vector2(sizeLimits.X.Maximum, sizeLimits.Y.Maximum);
            _text.alignment = textAlignment;
            _text.enableWordWrapping = true;

            //Update anchor and position.
            textRt.anchorMin = textAnchorMinMax_Pivot;
            textRt.anchorMax = textAnchorMinMax_Pivot;
            textRt.pivot = textAnchorMinMax_Pivot;
            textRt.anchoredPosition3D = Vector3.zero;
            
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
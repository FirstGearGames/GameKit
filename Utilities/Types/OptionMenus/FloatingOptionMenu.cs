using FishNet;
using FishNet.Utility.Performance;
using System.Runtime.CompilerServices;
using UnityEngine;


namespace GameKit.Utilities.Types.OptionMenuButtons
{

    public class FloatingOptionMenu : CanvasGroupFader
    {
        #region Serialized.
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
        private Transform _content;
        #endregion

        #region Private.
        /// <summary>
        /// Current buttons.
        /// </summary>
        private ButtonData[] _buttons;
        #endregion

        public virtual void Show(Vector3 position, Quaternion rotation, Vector3 scale, params ButtonData[] buttonDatas)
        {
            base.Show();
            transform.SetPositionAndRotation(position, rotation);
            transform.localScale = scale;
            //Remove all current buttons then add new ones.
            RemoveButtons();
            AddButtons(buttonDatas);
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public virtual void Show(Vector3 position, params ButtonData[] buttonDatas)
        {
            Show(position, Quaternion.identity, Vector3.one, buttonDatas);
        }
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public virtual void Show(Vector3 position, Quaternion rotation, params ButtonData[] buttonDatas)
        {
            Show(position, rotation, Vector3.one, buttonDatas);
        }
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public virtual void Show(Transform startingPoint, params ButtonData[] buttonDatas)
        {
            if (startingPoint == null)
            {
                InstanceFinder.NetworkManager.LogError($"A null Transform cannot be used as the starting point.");
                return;
            }

            Show(startingPoint.position, startingPoint.rotation, startingPoint.localScale, buttonDatas);
        }

        public override void Hide()
        {
            base.Hide();
        }

        private void AddButtons(params ButtonData[] buttonDatas)
        {

        }

        /// <summary>
        /// Removes all buttons.
        /// </summary>
        private void RemoveButtons()
        {
            if (_buttons == null)
                return;

            foreach (ButtonData item in _buttons)
                DisposableObjectCaches<ButtonData>.Store(item);


        }

    }


}
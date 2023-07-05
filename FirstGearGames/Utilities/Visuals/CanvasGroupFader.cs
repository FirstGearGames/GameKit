using System.Collections;
using UnityEngine;

namespace FirstGearGames.Utilities.Objects
{


    public class CanvasGroupFader : MonoBehaviour
    {
        #region Serialized.
        /// <summary>
        /// CanvasGroup to fade in and out.
        /// </summary>
        [Tooltip("CanvasGroup to fade in and out.")]
        [SerializeField]
        protected CanvasGroup CanvasGroup;
        #endregion

        #region Private.
        /// <summary>
        /// True if showing, which will fade in. False to fade out. Null if not fading at all.
        /// </summary>
        private bool? _isShowing;
        #endregion

        #region Const.
        /// <summary>
        /// How long to fad ein.
        /// </summary>
        private const float FADE_IN_DURATION = 0.1f;
        /// <summary>
        /// How long to fade out.
        /// </summary>
        private const float FADE_OUT_DURATION = 0.3f;
        #endregion

        private void OnDisable()
        {
            ResetSettings();
        }

        private void Update()
        {
            Fade();
        }

        /// <summary>
        /// Shows CanvasGroup with a fade.
        /// </summary>
        public virtual void Show()
        {
            SetShowing(true);
        }

        /// <summary>
        /// Hides CanvasGroup with a fade.
        /// </summary>
        public virtual void Hide()
        {
            //Immediately make unclickable so players cannot hit UI objects as it's fading out.
            SetCanvasGroupActiveWithoutAlpha(false);
            SetShowing(false);
        }

        /// <summary>
        /// Sets showing and begins fading if required.
        /// </summary>
        /// <param name="showing"></param>
        private void SetShowing(bool showing)
        {
            //If set to the same value.
            if (_isShowing.HasValue && showing == _isShowing.Value)
                return;

            _isShowing = showing;
        }

        /// <summary>
        /// Fades in or out over time.
        /// </summary>
        /// <returns></returns>
        private void Fade()
        {
            if (!_isShowing.HasValue)
                return;

            bool fadingIn = (_isShowing.Value);
            float duration = (fadingIn) ? FADE_IN_DURATION : FADE_OUT_DURATION;
            float rate = (1f / duration);
            float targetAlpha = (fadingIn) ? 1f : 0f;

            CanvasGroup.alpha = Mathf.MoveTowards(CanvasGroup.alpha, targetAlpha, rate * Time.deltaTime);

            //If complete.
            if (CanvasGroup.alpha == targetAlpha)
            {             
                SetCanvasGroupActiveWithoutAlpha(fadingIn);
                ResetSettings();
            }
        }

        /// <summary>
        /// Changes CanvasGroup active state without changing alpha.
        /// </summary>
        private void SetCanvasGroupActiveWithoutAlpha(bool active)
        {
            CanvasGroup.SetActive(active, false);
        }

        /// <summary>
        /// Resets settings as if first being used.
        /// </summary>
        private void ResetSettings()
        {
            _isShowing = null;
        }
    }


}
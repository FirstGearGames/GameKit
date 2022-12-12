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
        /// True if showing, which will fade in. False to fade out.
        /// </summary>
        private bool? _showing;
        /// <summary>
        /// Coroutine for fading.
        /// </summary>
        private Coroutine _fadeCoroutine;
        #endregion

        #region Const.
        /// <summary>
        /// How long to fade in and out.
        /// </summary>
        private const float FADE_DURATION = 0.1f;
        #endregion

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
            if (_showing != null && showing == _showing.Value)
                return;

            _showing = showing;
            if (_fadeCoroutine == null)
                _fadeCoroutine = StartCoroutine(__Fade());
        }

        /// <summary>
        /// Fades in or out over time.
        /// </summary>
        /// <returns></returns>
        private IEnumerator __Fade()
        {
            float rate = (1f / FADE_DURATION);
            float targetAlpha;

            do
            {
                targetAlpha = (_showing.Value == true) ? 1f : 0f;
                CanvasGroup.alpha = Mathf.MoveTowards(CanvasGroup.alpha, targetAlpha, rate * Time.deltaTime);
                yield return null;
            } while (CanvasGroup.alpha != targetAlpha);

            //Once complete update canvas group access.
            SetCanvasGroupActiveWithoutAlpha(_showing.Value);
            _fadeCoroutine = null;
        }

        /// <summary>
        /// Changes CanvasGroup active state without changing alpha.
        /// </summary>
        private void SetCanvasGroupActiveWithoutAlpha(bool active)
        {
            CanvasGroup.SetActive(active, false);
        }
    }


}
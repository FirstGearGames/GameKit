using UnityEngine;

namespace GameKit.Utilities.Types
{

    public class CanvasGroupFader : MonoBehaviour
    {
        #region Types.
        /// <summary>
        /// Current fade state or goal for this class.
        /// </summary>
        public enum FadeGoalType
        {
            Unset = 0,
            Hidden = 1,
            Visible = 2,
        }
        #endregion

        #region Public.
        /// <summary>
        /// Current goal for the fader.
        /// </summary>
        public FadeGoalType FadeGoal { get; private set; } = FadeGoalType.Unset;
        /// <summary>
        /// True if hidden or in the process of hiding.
        /// </summary>
        public bool IsHiding => (FadeGoal == FadeGoalType.Hidden);
        /// <summary>
        /// True if visible. Will be true long as the CanvasGroup has alpha. Also see IsHiding.
        /// </summary>
        public bool IsVisible => (CanvasGroup.alpha > 0f);
        #endregion

        #region Serialized.
        /// <summary>
        /// CanvasGroup to fade in and out.
        /// </summary>
        [Tooltip("CanvasGroup to fade in and out.")]
        [SerializeField]
        protected CanvasGroup CanvasGroup;
        #endregion

        #region Protected.
        /// <summary>
        /// How long it should take to fade in the CanvasGroup.
        /// </summary>
        protected virtual float FadeInDuration { get; set; } = 0.1f;
        /// <summary>
        /// How long it should take to fade out the CanvasGroup.
        /// </summary>
        protected virtual float FadeOutDuration { get; set; } = 0.3f;
        #endregion

        #region Private.
        /// <summary>
        /// True if a fade cycle has completed at least once.
        /// </summary>
        private bool _completedOnce;
        #endregion

        protected virtual void OnEnable()
        {
            FadeGoal = (CanvasGroup.alpha > 0f) ? FadeGoalType.Visible : FadeGoalType.Hidden;
        }

        protected virtual void OnDisable()
        {
            bool fadingIn = (FadeGoal == FadeGoalType.Visible);
            CanvasGroup.SetActive(fadingIn, true);
        }

        protected virtual void Update()
        {
            Fade();
        }

        /// <summary>
        /// Shows CanvasGroup immediately.
        /// </summary>
        public virtual void ShowImmediately()
        {
            SetShowing(true);
            CanvasGroup.SetActive(true, true);
        }

        /// <summary>
        /// Hides CanvasGroup immediately. 
        /// </summary>
        public virtual void HideImmediately()
        {
            SetShowing(false);
            CanvasGroup.SetActive(false, true);
        }

        /// <summary>
        /// Shows CanvasGroup with a fade.
        /// </summary>
        public virtual void Show()
        {
            if (FadeInDuration <= 0f)
                ShowImmediately();
            else
                SetShowing(true);
        }

        /// <summary>
        /// Hides CanvasGroup with a fade.
        /// </summary>
        public virtual void Hide()
        {
            if (FadeOutDuration <= 0f)
            {
                HideImmediately();
            }
            else
            {
                //Immediately make unclickable so players cannot hit UI objects as it's fading out.
                SetCanvasGroupActiveWithoutAlpha(false);
                SetShowing(false);
            }
        }

        /// <summary>
        /// Sets showing and begins fading if required.
        /// </summary>
        /// <param name="showing"></param>
        private void SetShowing(bool showing)
        {
            FadeGoal = (showing) ? FadeGoalType.Visible : FadeGoalType.Hidden;
        }

        /// <summary>
        /// Fades in or out over time.
        /// </summary>
        /// <returns></returns>
        private void Fade()
        {
            //Should not be possible.
            if (FadeGoal == FadeGoalType.Unset)
            {
                Debug.LogError($"Fade goal is unset. This should not be possible.");
                return;
            }

            bool fadingIn = (FadeGoal == FadeGoalType.Visible);
            float duration;
            float targetAlpha;
            if (fadingIn)
            {
                targetAlpha = 1f;
                duration = FadeInDuration;
            }
            else
            {
                targetAlpha = 0f;
                duration = FadeOutDuration;
            }

            /* Already at goal and had completed an iteration at least once.
             * This is checked because even if at alpha we want to 
             * complete the cycle if not done once so that all
             * local states and canvasgroup settings are proper. */
            if (_completedOnce && CanvasGroup.alpha == targetAlpha)
                return;

            float rate = (1f / duration);
            CanvasGroup.alpha = Mathf.MoveTowards(CanvasGroup.alpha, targetAlpha, rate * Time.deltaTime);

            //If complete.
            if (CanvasGroup.alpha == targetAlpha)
            {
                SetCanvasGroupActiveWithoutAlpha(fadingIn);
                _completedOnce = true;
            }
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
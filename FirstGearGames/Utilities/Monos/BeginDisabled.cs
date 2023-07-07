using UnityEngine;

namespace FirstGearGames.Utilities.Monos
{
    public class BeginDisabled : MonoBehaviour
    {
        #region Serialized.
        /// <summary>
        /// True to run on Awake rather than Start.
        /// </summary>
        [Tooltip("True to run on Awake rather than Start.")]
        [SerializeField]
        private bool _runOnAwake = true;
        #endregion

        #region Readonly.
        /// <summary>
        /// This script destroys itself if run after the scene has been loaded for the specified amount of time.
        /// </summary>
        private readonly float _ignoreAfterSceneTime = 0.25f;
        #endregion

        private void Awake()
        {
            if (_runOnAwake)
                CheckDisable();
        }
        private void Start()
        {
            if (!_runOnAwake)
                CheckDisable();
        }

        /// <summary>
        /// Checks if this object needs to be disabled.
        /// </summary>
        private void CheckDisable()
        {
            if (Time.timeSinceLevelLoad > _ignoreAfterSceneTime)
            {
                Destroy(this);
                return;
            }

            gameObject.SetActive(false);
        }
    }


}
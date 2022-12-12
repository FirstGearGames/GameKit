using FirstGearGames.Utilities.Objects;
using System.Collections;
using UnityEngine;


namespace FirstGearGames.Utilities.Monos
{

    /// <summary>
    /// Fades in Sprites OnEnable.
    /// </summary>
    public class FadeInSprite : MonoBehaviour
    {
        #region Serialized.
        /// <summary>
        /// How quickly to fade in.
        /// </summary>
        [Tooltip("How quickly to fade in.")]
        [SerializeField]
        private float _fadeInDuration = 0.5f;
        /// <summary>
        /// True to include sprites in children transforms.
        /// </summary>
        [Tooltip("True to include sprites in children transforms.")]
        [SerializeField]
        private bool _includeChildren = false;
        #endregion

        #region Private.
        /// <summary>
        /// SpriteRenderers found.
        /// </summary>
        private SpriteRenderer[] _sprites = new SpriteRenderer[1];
        /// <summary>
        /// Starting opacities of the SpriteRenderers.
        /// </summary>
        private float[] _startingOpacities = new float[1];
        #endregion

        private void Awake()
        {
            FirstInitialize();
            MakeInvisible();
        }

        private void OnEnable()
        {
            StartCoroutine(__FadeIn());
        }

        private void OnDisable()
        {
            MakeInvisible();
        }

        /// <summary>
        /// Initializes the script for first use. Should only be called once in this scripts lifetime.
        /// </summary>
        private void FirstInitialize()
        {
            if (_includeChildren)
                _sprites = GetComponentsInChildren<SpriteRenderer>();
            else
                _sprites[0] = GetComponent<SpriteRenderer>();

            //No sprite renderers found.
            if (_sprites.Length == 0 || _sprites[0] == null)
            {
                Debug.LogError("No sprites were found on transform " + transform.name + ".");
                DestroyImmediate(this);
                return;
            }
            //Renderers found, set starting colors.
            else
            {
                _startingOpacities = new float[_sprites.Length];
                for (int i = 0; i < _sprites.Length; i++)
                    _startingOpacities[i] = _sprites[i].color.a;
            }
        }

        /// <summary>
        /// Makes sprites invisible.
        /// </summary>
        private void MakeInvisible()
        {
            if (_sprites.Length == 0 || _sprites[0] == null)
                return;

            for (int i = 0; i < _sprites.Length; i++)
            {
                _sprites[i].color = new Color(_sprites[0].color.r, _sprites[0].color.g, _sprites[0].color.b, 0f);
            }
        }

        /// <summary>
        /// Fades in sprites over the preset time.
        /// </summary>
        /// <returns></returns>
        private IEnumerator __FadeIn()
        {
            WaitForEndOfFrame endOfFrame = new WaitForEndOfFrame();

            float timePassed = 0f;
            while (timePassed < _fadeInDuration)
            {
                timePassed += Time.deltaTime;
                float percent = timePassed / _fadeInDuration;
                for (int i = 0; i < _sprites.Length; i++)
                {
                    _sprites[i].color = new Color(_sprites[0].color.r, _sprites[0].color.g, _sprites[0].color.b, Mathf.Lerp(0f, _startingOpacities[i], percent));
                }
                yield return endOfFrame;
            }
        }
    }


}
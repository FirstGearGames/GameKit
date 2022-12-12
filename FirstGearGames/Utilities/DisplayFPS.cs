
using FirstGearGames.Utilities.Maths;
using UnityEngine;
using UnityEngine.UI;

namespace FirstGearGames.Utilities.Maths
{


    public class DisplayFPS : MonoBehaviour
    {
        [SerializeField]
        private Text _fpsText = null;

        private FrameRateCalculator _frameRate = new FrameRateCalculator();


        private void Update()
        {
            UpdateFrameRate();
        }

        private void UpdateFrameRate()
        {
            if (_frameRate.Update(Time.unscaledDeltaTime))
                _fpsText.text = _frameRate.GetIntFrameRate().ToString();
        }

    }


}
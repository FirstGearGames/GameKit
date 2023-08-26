using FishNet.Connection;
using System.Text;
using TMPro;
using UnityEngine;

namespace GameKit.Bundles.Chats
{

    public class ChatEntry : MonoBehaviour
    {
        #region Public.
        /// <summary>
        /// Connection which send this message.
        /// </summary>
        public NetworkConnection Sender { get; private set; }
        #endregion

        #region Serialized.
        /// <summary>
        /// Text to display message.
        /// </summary>
        [Tooltip("Text to display message.")]
        [SerializeField]
        private TextMeshProUGUI _text;
        #endregion

        #region Private.
        /// <summary>
        /// Used to build messages
        /// </summary>
        private StringBuilder _builder = new StringBuilder();
        /// <summary>
        /// ChatCanvas which this entry resides.
        /// </summary>
        private ChatCanvasBase _canvas;
        #endregion

        /// <summary>
        /// Sets sender value.
        /// </summary>
        /// <param name="sender"></param>
        public void Initialize(ChatCanvasBase canvas, NetworkConnection sender)
        {
            _canvas = canvas;
            Sender = sender;
        }

        /// <summary>
        /// Sets text.
        /// </summary>
        /// <param name="message"></param>
        public void SetText(string message)
        {
            _text.text = message;
        }

        /// <summary>
        /// Sets text.
        /// </summary>
        /// <param name="objs"></param>
        public void SetText(params object[] objs)
        {
            if (objs.Length == 1)
            {
                _text.text = (string)objs[0];
            }
            else
            {
                Color color;
                string msg;
                //Colors are always first, then text.
                for (int i = 0; i < objs.Length; i++)
                {
                    if (i % 2 == 0)
                    {
                        color = (Color)objs[i];
                        _builder.Append($"<color=#{ColorUtility.ToHtmlStringRGBA(color)}>");
                    }
                    else
                    {
                        msg = (string)objs[i];
                        _builder.Append(msg);
                    }
                }

                _text.text = _builder.ToString();
            }
        }


        /// <summary>
        /// Called when this chat entry is pressed.
        /// </summary>
        public void OnClick()
        {
            _canvas.ChatEntry_Clicked(this);
        }
    }

}
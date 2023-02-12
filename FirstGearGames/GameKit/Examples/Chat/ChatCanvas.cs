using FirstGearGames.FastObjectPool;
using FirstGearGames.Utilities.Objects;
using FishNet;
using FishNet.Connection;
using FishNet.Utility;
using FishNet.Utility.Extension;
using OldFartGames.Gameplay.Dependencies;
using System.Collections.Generic;
using TMPro;
using TriInspector;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

namespace OldFartGames.Gameplay.Canvases.Chats
{
    /// <summary>
    /// Used to display chat and take chat input from the local client.
    /// </summary>
    [DeclareFoldoutGroup("Misc")]
    [DeclareFoldoutGroup("Outbound")]
    [DeclareFoldoutGroup("Colors")]
    public class ChatCanvas : MonoBehaviour
    {
        #region Serialized.
        [SerializeField, Group("Misc")]
        private IChatHotkeys _hotkeys;
        /// <summary>
        /// Sets which ChatManager to use.
        /// </summary>
        /// <param name="value">New Value.</param>
        public void SetChatManager(ChatManager value) => _chatManager = value;
        [Tooltip("ChatManager to use.")]
        [SerializeField, Group("Misc")]
        private ChatManager _chatManager;
        /// <summary>
        /// Scrollbar for the chat.
        /// </summary>
        [Tooltip("Scrollbar for the chat.")]
        [SerializeField, Group("Misc")]
        private Scrollbar _scrollbar;
        /// <summary>
        /// Transform to child messages under.
        /// </summary>
        [Tooltip("Transform to child messages under.")]
        [SerializeField, Group("Misc")]
        private Transform _content;
        /// <summary>
        /// Prefab to spawn for messages.
        /// </summary>
        [Tooltip("Prefab to spawn for messages.")]
        [SerializeField, Group("Misc")]
        private ChatEntry _entryPrefab;
        /// <summary>
        /// Maximum number of messages to be displayed at once.
        /// </summary>
        [Tooltip("Maximum number of messages to be displayed at once.")]
        [SerializeField, Group("Misc")]
        private ushort _maximumMessages = 100;
       
        /// <summary>
        /// Text for outbound messages.
        /// </summary>
        [Tooltip("Text for outbound messages.")]
        [SerializeField, Group("Outbound")]
        private TMP_InputField _outboundText;
        /// <summary>
        /// Maximum length a message may be.
        /// </summary>
        [Tooltip("Maximum length a message may be.")]
        [SerializeField, Group("Outbound")]
        private ushort _maximumMessageSize = 500;
        /// <summary>
        /// Text to show current message target.
        /// </summary>
        [Tooltip("Text to show current message target.")]
        [SerializeField, Group("Outbound")]
        private TextMeshProUGUI _messageTargetText;

        /// <summary>
        /// Name color for messages from self.
        /// </summary>
        [Tooltip("Name color for messages from self.")]
        [SerializeField, Group("Colors")]
        private Color _selfColor;
        /// <summary>
        /// Name color for messages from enemies.
        /// </summary>
        [Tooltip("Name color for messages from enemies.")]
        [SerializeField, Group("Colors")]
        private Color _enemyColor;
        /// <summary>
        /// Name color for messages from friendlies.
        /// </summary>
        [Tooltip("Name color for messages from friendlies.")]
        [SerializeField, Group("Colors")]
        private Color _friendlyColor;
        /// <summary>
        /// Name color for messages from others when spectating.
        /// </summary>
        [Tooltip("Name color for messages from others when spectating.")]
        [SerializeField, Group("Colors")]
        private Color _directColor;
        /// <summary>
        /// Color for messages.
        /// </summary>
        [Tooltip("Color for messages.")]
        [SerializeField, Group("Colors")]
        private Color _messageColor;
        #endregion

        #region Private.
        /// <summary>
        /// Messages added to content.
        /// </summary>
        private Queue<ChatEntry> _contentMessages = new Queue<ChatEntry>();
        /// <summary>
        /// Fixes the scrollbar position when a message is added or removed.
        /// </summary>
        private ScrollbarValueFixer _scrollbarFixer;
        /// <summary>
        /// True if the chat outbound window is selected.
        /// </summary>
        private bool _outboundSelected;
        /// <summary>
        /// Current target for chat messages.
        /// </summary>
        private MessageTargetTypes _currentTargetType = MessageTargetTypes.All;
        /// <summary>
        /// Client which a tell is to be sent to.
        /// </summary>
        private int _tellClientId = -1;
        /// <summary>
        /// Last value for outbound text on value change. This value is after modifications.
        /// </summary>
        private string _lastOutboundText;
        /// <summary>
        /// LayoutElement for the target text.
        /// </summary>
        private LayoutElement _targetTextLayoutElement;
        /// <summary>
        /// Strings which can be used as tell headers.
        /// </summary>
        private readonly string[] _tellCommands = new string[] { "/tell ", "/w ", };
        #endregion

        #region Const
        /// <summary>
        /// Id to use for tell client when client is invalid.
        /// </summary>
        private const int INVALID_TELL_CLIENTID = -1;
        /// <summary>
        /// Default with for chat target when not populated by special content such as a tell name.
        /// </summary>
        private const float DEFAULT_TARGET_WIDTH = 60f;
        #endregion

        private void Awake()
        {
            _scrollbarFixer = new ScrollbarValueFixer(_scrollbar);
            _targetTextLayoutElement = _messageTargetText.GetComponentInParent<LayoutElement>();

            //Remove testing content.
            while (_content.transform.childCount > 0)
                DestroyImmediate(_content.transform.GetChild(0).gameObject);

            UpdateMessageTargetText();
            _outboundText.onValueChanged.AddListener(Outbound_OnValueChange);
            _outboundText.onSubmit.AddListener(SendChatMessage);
            _outboundText.onSelect.AddListener(Outbound_OnChatSelected);
            _outboundText.onDeselect.AddListener(Outbound_OnChatDeselected);
        }

        private void Start()
        {
#if !UNITY_EDITOR
            /* Only check if client when not editor.
            * This is because client will always be true
            * in builds by the time gameplaydependencies is loaded
            * but if the scene is started directly in editor, it
            * may not yet be true. */
            if (InstanceFinder.IsClient)
#endif
            {
                _chatManager.OnBlockedChatMessage += ChatManager_OnBlockedChatMessage;
                _chatManager.OnIncomingChatMessage += ChatManager_OnIncomingChatMessage;
            }
        }

        private void OnDisable()
        {
            if (!ApplicationState.IsQuitting())
            {
                RemoveExcessiveMessages(0);
                _scrollbarFixer.SetValue(0f);
            }
        }

        private void Update()
        {
            CheckChatHotkeys();
            CheckChangeChatType();
        }
        private void LateUpdate()
        {
            if (!ApplicationState.IsQuitting())
                _scrollbarFixer.Fix();
        }

        /// <summary>
        /// Checks if the chat should be selected.
        /// </summary>
        private void CheckChatHotkeys()
        {
            //Checks which are allowed while blocking is enabled.
            if (GlobalManager.CanvasManager.BlockingCanvasOpen)
            {
                if (_outboundSelected && NInput.Keyboard.GetKeyPressed(Key.Escape))
                {
                    SetOutboundSelection(false);
                    EventSystem.current.SetSelectedGameObject(null);
                }
            }
            //Checks only allowed while not blocking.
            else
            {
                if (NInput.Keyboard.GetKeyPressed(Key.Enter))
                {
                    SetOutboundSelection(true);
                }
                else if (NInput.Keyboard.GetKeyPressed(Key.Slash) || NInput.Keyboard.GetKeyPressed(Key.Backslash))
                {
                    _outboundText.text = "/";
                    _outboundText.caretPosition = _outboundText.text.Length;
                    SetOutboundSelection(true);
                }
            }
        }

        /// <summary>
        /// Checks to change between chat types, such as friendly and enemy.
        /// </summary>
        private void CheckChangeChatType()
        {
            if (_outboundSelected && NInput.Keyboard.GetKeyPressed(Key.Tab))
            {
                int highestValue = EnumFN.GetHighestValue<MessageTargetTypes>();
                int nextValue = ((int)_currentTargetType + 1);
                if (nextValue > highestValue)
                    nextValue = 1;

                _currentTargetType = (MessageTargetTypes)nextValue;
                UpdateMessageTargetText();
            }
        }

        /// <summary>
        /// Updates message target text based on current target.
        /// </summary>
        private void UpdateMessageTargetText()
        {
            //Update with and color of target text.
            Color c;
            float width;
            if (_tellClientId != INVALID_TELL_CLIENTID && _currentTargetType == MessageTargetTypes.Tell)
            {
                c = _directColor;
                //Update width to fit tell name.
                _messageTargetText.ForceMeshUpdate();
                width = (_messageTargetText.GetRenderedValues(false).x + 20f);
            }
            else
            {
                c = _messageColor;
                width = DEFAULT_TARGET_WIDTH;
                _messageTargetText.text = _currentTargetType.ToString();
            }
            _targetTextLayoutElement.preferredWidth = width;

            _messageTargetText.color = c;
        }

        /// <summary>
        /// Called when chat is received.
        /// </summary>
        private void ChatManager_OnIncomingChatMessage(IncomingChatMessage obj)
        {
            IChatEntity entity = _chatManager.GetChatEntity(obj.Sender);
            TeamTypes tt = entity.GetTeamType();
            string entityName = entity.GetEntityName();

            ShowMessage(obj.TargetType, tt, entityName, obj.Message, obj.Outbound, obj.Sender);
        }

        /// <summary>
        /// Called when an outgoing message is blocked.
        /// </summary>
        private void ChatManager_OnBlockedChatMessage(BlockedChatMessage obj)
        {
            NetworkConnection blockedConn = obj.Sender;
            if (blockedConn == null || !blockedConn.IsLocalClient)
                return;

            if (obj.Reason == BlockedChatReason.InvalidTargetId)
                ShowMessage(Color.red, $"User is not logged on.");
            else if (obj.Reason == BlockedChatReason.InvalidState)
                ShowMessage(Color.red, $"You are not connected.");
            else if (obj.Reason == BlockedChatReason.TooManyMessages)
                ShowMessage(Color.yellow, $"You are sending messages too fast.");
        }

        /// <summary>
        /// Shows a message from a player.
        /// </summary>
        public void ShowMessage(MessageTargetTypes targetType, TeamTypes playerType, string playerName, string message, bool outbound, NetworkConnection sender = null)
        {
         
            Color c;
            // Color c;
            if (targetType == MessageTargetTypes.Tell)
            {
                c = _directColor;
            }
            else if (playerType == TeamTypes.Self)
            {
                c = _selfColor;
            }
            else if (playerType == TeamTypes.Enemy)
            {
                c = _enemyColor;
            }
            else if (playerType == TeamTypes.Friendly)
            {
                c = _friendlyColor;
            }
            else
            {
                c = Color.white;
                Debug.LogError($"Unhandled playerType {playerType.ToString()}.");
            }

            float scrollStart = _scrollbar.value;
            ChatEntry ce = ObjectPool.Retrieve<ChatEntry>(_entryPrefab.gameObject, _content, false);
            Color msgTypeColor = new Color(0.50f, 0.50f, 0.50f);

            string prefix;
            if (targetType == MessageTargetTypes.Tell)
                prefix = (outbound) ? "To " : "From ";
            else
                prefix = targetType.ToString();
            ce.SetText(msgTypeColor, $"[{prefix}] ", c, playerName, _messageColor, $": {message}");
            AddMessage(ce, scrollStart, sender);
        }

        /// <summary>
        /// Shows a message with a specified color.
        /// </summary>
        public void ShowMessage(Color c, string message, NetworkConnection sender = null)
        {
            float scrollStart = _scrollbar.value;
            ChatEntry ce = ObjectPool.Retrieve<ChatEntry>(_entryPrefab.gameObject, _content, false);
            ce.SetText(c, message);
            AddMessage(ce, scrollStart, sender);
        }

        /// <summary>
        /// Shows a message without adding formatting.
        /// </summary>
        public void ShowMessage(string message, NetworkConnection sender = null)
        {
            float scrollStart = _scrollbar.value;
            ChatEntry ce = ObjectPool.Retrieve<ChatEntry>(_entryPrefab.gameObject, _content, false);
            ce.SetText(message);
            AddMessage(ce, scrollStart, sender);
        }

        /// <summary>
        /// Removes messages which exceed maximum messages.
        /// </summary>
        private void AddMessage(ChatEntry newEntry, float scrollStart, NetworkConnection sender)
        {
            if (newEntry != null)
            {
                newEntry.Initialize(this, sender);
                _contentMessages.Enqueue(newEntry);
            }

            RemoveExcessiveMessages(_maximumMessageSize);
            _scrollbarFixer.SetValue(scrollStart);
        }

        /// <summary>
        /// Removes excessive messages.
        /// </summary>
        /// <param name="maximumMessages"></param>
        private void RemoveExcessiveMessages(ushort maximumMessages)
        {
            while (_contentMessages.Count > maximumMessages)
            {
                ChatEntry ce = _contentMessages.Dequeue();
                ObjectPool.Store(ce.gameObject);
            }
        }

        /// <summary>
        /// Called when a chat entry is pressed.
        /// </summary>
        /// <param name="ce"></param>
        public void ChatEntry_Clicked(ChatEntry ce)
        {
            IChatEntity entity = _chatManager.GetChatEntity(ce.Sender);
            if (entity == null)
                return;

            _tellClientId = entity.GetConnection().ClientId;
            UpdateToTellTarget(entity.GetEntityName());
            SetOutboundSelection(true);
        }

        /// <summary>
        /// Sends a message to the server.
        /// </summary>
        private void SendChatMessage(string message)
        {
            message = message.Trim();
            if (message == string.Empty)
                return;

            /* Make sure the user doesn't do a dumb, protect them from sending tells in public.
             * If the message leads with a tell command then it means the name didnt
             * auto complete. */
            foreach (string item in _tellCommands)
            {
                if (message.StartsWith(item, System.StringComparison.OrdinalIgnoreCase))
                    return;
            }

            bool sendResult = false;
            if (_currentTargetType == MessageTargetTypes.Tell)
            {
                sendResult = _chatManager.SendDirectChatToServer(_tellClientId, message);
            }
            else if (_currentTargetType == MessageTargetTypes.All)
            {
                sendResult = _chatManager.SendMultipleChatToServer(false, message);
            }
            else if (_currentTargetType == MessageTargetTypes.Team)
            {
                sendResult = _chatManager.SendMultipleChatToServer(true, message);
            }
            else
            {
                Debug.LogError($"Unhandled MessageTargetType of {_currentTargetType}.");
            }

            if (!sendResult)
                return;

            _outboundText.text = string.Empty;
            SetOutboundSelection(_chatManager.GetKeepChatSelected());
        }

        /// <summary>
        /// Called when the outbound chat is selected.
        /// </summary>
        private void Outbound_OnChatSelected(string s)
        {
            SetOutboundSelection(true);
        }

        /// <summary>
        /// Called when the outbound chat is deselected.
        /// </summary>
        private void Outbound_OnChatDeselected(string s)
        {
            SetOutboundSelection(false);
        }

        /// <summary>
        /// Called when the outbound text changes.
        /// </summary>        
        private void Outbound_OnValueChange(string s)
        {
            //Nothing to process.
            if (s == _lastOutboundText)
                return;
            bool hasText = (s.Length > 0);

            //Has leading white space.
            if (hasText && s.Substring(0, 1) == " ")
            {
                s = s.Substring(1);
                SetOutboundText(s, false);
                return;
            }

            //Don't try to set a tell if not a tell command.            
            bool tellFound = false;
            foreach (string item in _tellCommands)
            {
                if (s.StartsWith(item, System.StringComparison.OrdinalIgnoreCase))
                {
                    tellFound = true;
                    break;
                }

            }
            if (!tellFound)
                return;

            string[] words = s.Split(' ');
            //Not enough words to indicate a tell.
            if (words.Length < 3)
                return;

            int foundId = -1;
            string name = words[1].ToLower();
            //Find name in all client instances.
            foreach (KeyValuePair<int, ClientInstance> kvp in ClientInstance.ClientInstances)
            {
                ClientInstance ci = kvp.Value;
                //if (ci.Owner.IsLocalClient)
                //    continue;

                if (ci.PlayerName.ToLower() != name)
                    continue;

                //If here then found the player.
                foundId = kvp.Key;
                words[1] = ci.PlayerName;
                break;
            }

            //If found.
            if (foundId != INVALID_TELL_CLIENTID)
            {
                _tellClientId = foundId;
                UpdateToTellTarget(words[1]);
                string wordsWithoutName = string.Join(" ", words, 2, words.Length - 2);
                SetOutboundText(wordsWithoutName, false);
            }
        }

        /// <summary>
        /// Sets message target text to show a tell name.
        /// </summary>
        /// <param name="tellName"></param>
        private void UpdateToTellTarget(string tellName)
        {
            _currentTargetType = MessageTargetTypes.Tell;
            _messageTargetText.text = $"Tell {tellName}";
            UpdateMessageTargetText();
        }

        /// <summary>
        /// Sets outbound text value.
        /// </summary>
        private void SetOutboundText(string s, bool moveCarotToEnd)
        {
            _lastOutboundText = s;
            _outboundText.text = s;

            if (moveCarotToEnd)
                _outboundText.caretPosition = _outboundText.text.Length;
        }

        /// <summary>
        /// Sets if the outbound chat is selected or not.
        /// </summary>
        private void SetOutboundSelection(bool select)
        {
            _outboundSelected = true;
            if (select)
            {
                _outboundText.ActivateInputField();
                GlobalManager.CanvasManager.AddBlockingCanvas(this);
            }
            else
            {
                _outboundText.DeactivateInputField();
                GlobalManager.CanvasManager.RemoveBlockingCanvas(this);

            }
        }
    }

}
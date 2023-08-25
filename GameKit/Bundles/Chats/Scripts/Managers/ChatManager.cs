using FishNet.Object;
using System.Collections.Generic;
using System;
using FishNet.Connection;
using UnityEngine;
using System.Runtime.CompilerServices;
using FishNet;

using FishNet.Transporting;
using GameKit.Dependencies.Inspectors;
using GameKit.Core.Chats;
using GameKit.Dependencies.Utilities;

namespace GameKit.Bundles.Chats.Managers
{

    /// <summary>
    /// Sets weaponIds on server objects.
    /// </summary>
    public class ChatManager : NetworkBehaviour
    {

        #region Types.
        /// <summary>
        /// Where the blocked message response came from.
        /// </summary>
        private enum BlockedChatResponseTypes
        {
            LocalCLient = 0,
            LocalServer = 1,
            FromServer = 2,
        }
        /// <summary>
        /// Tracks chat frequency of a client.
        /// </summary>
        private class ChatFrequencyData : IResettable
        {
            #region Private.
            /// <summary>
            /// Number of infractions where client has sent messages before the message window had expired.
            /// </summary>
            private byte _infractions;
            /// <summary>
            /// Unscaled time of the last received message.
            /// </summary>
            private float _lastMessageTime = float.MinValue;
            #endregion

            /// <summary>
            /// Removes infractions based on time passed since last message.
            /// </summary>
            [MethodImpl(MethodImplOptions.AggressiveInlining)]
            private void RemoveInfractions(float window)
            {
                if (_infractions == 0)
                    return;

                float timeDifference = (Time.unscaledTime - _lastMessageTime);
                int removalCount = (int)(timeDifference / window);
                //If there are infractions to remove.
                if (removalCount > 0)
                    RemoveInfractions((byte)removalCount);
            }

            /// <summary>
            /// Returns if client can send a message, and adjusts values based on result.
            /// </summary>
            [MethodImpl(MethodImplOptions.AggressiveInlining)]
            public bool TrySendMessage(float window, int maximumMessages)
            {
                //First try to remove infractions.
                RemoveInfractions(window);

                //Too many infractions at the moment.
                if (_infractions >= maximumMessages)
                    return false;

                float unscaledTime = Time.unscaledTime;
                //See if to increase infractions.
                if (unscaledTime - _lastMessageTime < window)
                    _infractions++;

                _lastMessageTime = unscaledTime;
                return true;
            }

            /// <summary>
            /// Removes a number of infractions.
            /// </summary>
            public void RemoveInfractions(byte count)
            {
                int result = (_infractions - count);
                _infractions = (byte)Mathf.Max(result, 0);
            }

            public void ResetState()
            {
                _infractions = 0;
                _lastMessageTime = float.MinValue;
            }

            public void InitializeState() { }
        }
        #endregion

        #region Public.
        /// <summary>
        /// Called when a chat message is received. This may execute on server or client.
        /// </summary>
        public event Action<ChatMessage, bool> OnIncomingChatMessage;
        /// <summary>
        /// Called when a chat message is blocked. This may execute on server or client.
        /// </summary>
        public event Action<BlockedChatMessage, bool> OnBlockedChatMessage;
        /// <summary>
        /// Currently registered chat entities.
        /// </summary>
        public Dictionary<NetworkConnection, IChatEntity> ChatEntities = new Dictionary<NetworkConnection, IChatEntity>();
        #endregion

        #region Serialized.
        /// <summary>
        /// True to keep the chat window selected after sending a message. False to deselect the chat window after sending a message.
        /// </summary>
        /// <returns></returns>
        public bool GetKeepChatSelected() => _keepChatSelected;
        /// <summary>
        /// Sets KeepChatSelected value.
        /// </summary>
        /// <param name="value">New value.</param>
        public void SetKeepChatSelected(bool value) => _keepChatSelected = value;
        [Tooltip("True to keep the chat window selected after sending a message. False to deselect the chat window after sending a message.")]
        [SerializeField, Group("Outbound")]
        private bool _keepChatSelected;
        #endregion

        #region Private.
        /// <summary>
        /// How frequent clients send data. This is only available on the server.
        /// </summary>
        private Dictionary<NetworkConnection, ChatFrequencyData> _clientChatFrequencies = new Dictionary<NetworkConnection, ChatFrequencyData>();
        /// <summary>
        /// How frequent the current client sends chats.
        /// Initialize immediately, even on server. One instance will not hurt anything and it's safer than trying to check initializing timing.
        /// </summary>
        private ChatFrequencyData _selfChatFrequency = new ChatFrequencyData();
        #endregion

        #region Const.
        /// <summary>
        /// Maximum size a message may be.
        /// </summary>
        private const int MAXIMUM_MESSAGE_SIZE = 250;
        /// <summary>
        /// Maximum number of messages per rolling time window.
        /// </summary>
        private const int MAXIMUM_CHAT_INFRACTIONS = 3;
        /// <summary>
        /// When a message is received within this window the clients message timer is increased.
        /// </summary>
        private const float TIME_WINDOW = 5f;
        /// <summary>
        /// Value to multiple TIME_WINDOW on client side. This is an effort to stop the client from sending too many messages locally before pushing them to the server.
        /// </summary>
        private const float CLIENT_TIME_WINDOW_MULTIPLIER = 1.25f;
        #endregion

        public override void OnStartNetwork()
        {
            base.OnStartNetwork();
            base.NetworkManager.RegisterInstance(this);
        }

        public override void OnStartClient()
        {
            base.OnStartClient();
            //No need to also track client side if server.
            if (!base.IsServer)
            {
                //Listen for future clients.
                base.ClientManager.OnRemoteConnectionState += ClientManager_OnRemoteConnectionState;
                //Add all current clients as chat entities.
                foreach (NetworkConnection conn in base.ClientManager.Clients.Values)
                    AddChatEntity(conn, GetConnectionPlayerName(conn));
                //todo add name stuff to clientinstance
            }
        }

        public override void OnSpawnServer(NetworkConnection connection)
        {
            base.OnSpawnServer(connection);
            _clientChatFrequencies[connection] = ResettableObjectCaches<ChatFrequencyData>.Retrieve();
            AddChatEntity(connection, GetConnectionPlayerName(connection));
        }

        public override void OnDespawnServer(NetworkConnection connection)
        {
            base.OnDespawnServer(connection);
            //Push data back to the cache.
            ChatFrequencyData data;
            if (_clientChatFrequencies.TryGetValue(connection, out data))
            {
                _clientChatFrequencies.Remove(connection);
                ResettableObjectCaches<ChatFrequencyData>.Store(data);
            }
            if (ChatEntities.TryGetValue(connection, out IChatEntity ce))
            {
                ChatEntities.Remove(connection);
                ResettableObjectCaches<IChatEntity>.Store(ce);
            }
        }

        public override void OnStopClient()
        {
            base.OnStopClient();
            base.ClientManager.OnRemoteConnectionState -= ClientManager_OnRemoteConnectionState;
        }

        public override void OnStopNetwork()
        {
            base.OnStopNetwork();
            base.NetworkManager.UnregisterInstance<ChatManager>();
            ChatEntities.Clear();
        }

        /// <summary>
        /// Called when a client other than self connects.
        /// This is only available when using ServerManager.ShareIds.
        /// </summary>
        private void ClientManager_OnRemoteConnectionState(RemoteConnectionStateArgs obj)
        {
            if (obj.ConnectionState == RemoteConnectionState.Started)
            {
                if (base.ClientManager.Clients.TryGetValue(obj.ConnectionId, out NetworkConnection conn))
                    AddChatEntity(conn, GetConnectionPlayerName(conn));
            }
        }

        //todo make use actual name / client instance.
        private string GetConnectionPlayerName(NetworkConnection conn)
        {
            return $"Player-{conn.ClientId}";
        }

        /// <summary>
        /// Adds a chat entity for a connection.
        /// </summary>
        /// <param name="conn">Connection to add for.</param>
        private void AddChatEntity(NetworkConnection conn, string name)
        {
            IChatEntity ce = GetChatEntity(conn, name);
            ChatEntities[conn] = ce;
        }

        /// <summary>
        /// Registers an IChatEntity with a connection.
        /// </summary>
        /// <param name="conn">Connection the entity is for.</param>
        /// <param name="entity">Entity to register to connection.</param>
        public void RegisterChatEntity(NetworkConnection conn, IChatEntity entity)
        {
            if (conn == null)
            {
                InstanceFinder.NetworkManager.LogError($"Connection cannot be null when registering a ChatEntity.");
                return;
            }

            ChatEntities[conn] = entity;
        }
        /// <summary>
        /// Unregisters a connection from ChatEntities.
        /// </summary>
        /// <param name="conn">Connection to unregister.</param>
        public void UnregisterChatEntity(NetworkConnection conn)
        {
            if (conn == null)
            {
                InstanceFinder.NetworkManager.LogError($"Connection cannot be null when unregistering a ChatEntity.");
                return;
            }

            ChatEntities.Remove(conn);
        }

        /// <summary>
        /// Gets the chat entity for the local client.
        /// </summary>
        /// <returns></returns>
        public IChatEntity GetChatEntity()
        {
            return GetChatEntity(base.ClientManager.Connection);
        }

        /// <summary>
        /// Returns a chat entity for a connection.
        /// </summary>
        /// <param name="conn">Connection to return entity for.</param>
        /// <returns></returns>
        public IChatEntity GetChatEntity(NetworkConnection conn)
        {
            if (ChatEntities.TryGetValue(conn, out IChatEntity entity))
                return entity;
            else
                return default;
        }

        /// <summary>
        /// Returns a chat entity for name.
        /// </summary>
        /// <param name="name">Name to match.</param>
        /// <param name="allowPartial">True to allow partial matches.</param>
        /// <param name="excludedConnections">Entities to ignore if connection is within this value.</param>
        /// <returns>Returns entity found. If multiple partial matches occur then null is returned.</returns>
        public IChatEntity GetChatEntity(string name, bool allowPartial, HashSet<NetworkConnection> excludedConnections)
        {
            IChatEntity foundEntity = null;
            //Find name in all chat entities.
            foreach (KeyValuePair<NetworkConnection, IChatEntity> item in ChatEntities)
            {
                if (excludedConnections != null && excludedConnections.Contains(item.Key))
                    continue;

                //Partial match.
                string lowerEntityName = item.Value.GetEntityName().ToLower();
                //Partial or full match found.
                if (lowerEntityName.Contains(name))
                {
                    //Exact match found.
                    if (lowerEntityName == name)
                    {
                        return item.Value;
                    }
                    else if (allowPartial)
                    {
                        /* If foundPlayerName already has value
                         * then a partial match was previously found.
                         * This is now two or more partial matches when
                         * cannot be made out to who the client wants to
                         * send a message to. In this case, do not auto
                         * complete the send. */
                        if (foundEntity != null)
                        {
                            foundEntity = null;
                            break;
                        }
                        else
                        {
                            foundEntity = item.Value;
                        }
                    }
                }
            }

            return foundEntity;
        }

        /// <summary>
        /// Returns all chat entities that partially or full match name.
        /// </summary>
        /// <param name="name">Name to match.</param>
        /// <returns>Returns entities found.</returns>
        public List<IChatEntity> GetChatEntities(string name, List<NetworkConnection> excludedConnections)
        {
            List<IChatEntity> result = new List<IChatEntity>();
            //Find name in all chat entities.
            foreach (KeyValuePair<NetworkConnection, IChatEntity> item in ChatEntities)
            {
                if (excludedConnections != null && excludedConnections.Contains(item.Key))
                    continue;

                //Partial match.
                string lowerEntityName = item.Value.GetEntityName().ToLower();
                //Partial or full match found.
                if (lowerEntityName.Contains(name))
                    result.Add(item.Value);
            }

            return result;
        }

        /// <summary>
        /// Gets an IChatEntity and initializes it.
        /// </summary>
        private IChatEntity GetChatEntity(NetworkConnection conn, string name)
        {
            IChatEntity result = ResettableObjectCaches<IChatEntity>.Retrieve();
            result.Initialize(conn, name);
            return result;
        }

        /// <summary>
        /// Returns if a message can be sent.
        /// </summary>
        private bool CanSendMessage(MessageType messageType, NetworkConnection sender, NetworkConnection target, string message, bool asServer, out BlockedChatReason blockedReason)
        {
            blockedReason = BlockedChatReason.Unset;

            //Check frequency.
            ChatFrequencyData frequencyData = null;
            //True if frequency data was found.
            bool frequencyFound = true;
            if (!asServer)
            {
                frequencyData = _selfChatFrequency;
            }
            else
            {
                //Sender disconnected. Do nothing.
                if (!sender.IsActive)
                {
                    frequencyFound = false;
                }
                /* No data for the connection. This should never happen but
                 * won't hurt anything to make new data. */
                else if (!_clientChatFrequencies.TryGetValue(sender, out frequencyData))
                {
                    frequencyData = ResettableObjectCaches<ChatFrequencyData>.Retrieve();
                    _clientChatFrequencies[sender] = frequencyData;
                }
            }

            //Frequency limit to use.
            float window = (asServer) ? TIME_WINDOW
                : (TIME_WINDOW * CLIENT_TIME_WINDOW_MULTIPLIER);

            //Frequency data not found, cannot continue.
            if (!frequencyFound)
            {
                blockedReason = BlockedChatReason.InvalidState;
            }
            //Sending too fast.
            else if (!frequencyData.TrySendMessage(window, MAXIMUM_CHAT_INFRACTIONS))
            {
                blockedReason = BlockedChatReason.TooManyMessages;
            }
            //Too long of a message. Should not be possible since textbox limits message length.
            else if (message.Length > MAXIMUM_MESSAGE_SIZE)
            {
                blockedReason = BlockedChatReason.NoResponse;
            }
            //Check active states. 
            else if ((!asServer && !base.IsClient) || (asServer && !base.IsServer))
            {
                blockedReason = BlockedChatReason.InvalidState;
            }
            //Trying to message self.
            else if (messageType == MessageType.Tell)
            {
                //Null target.
                if (target == null)
                {
                    blockedReason = BlockedChatReason.InvalidTargetId;
                }
                //Sending to self.
                else if (sender == target)
                {
                    blockedReason = BlockedChatReason.NoResponse;
                }
            }

            if (blockedReason != BlockedChatReason.Unset)
            {
                OnBlockedChatMessage?.Invoke(new BlockedChatMessage((ushort)messageType, sender, message, blockedReason), asServer);
                return false;
            }
            else
            {
                return true;
            }
        }

        /// <summary>
        /// Handles a blocked chat message response.
        /// </summary>
        /// <param name="msg"></param>
        /// <param name="fromServer">True if response is from the server.</param>
        private void HandleBlockedChatMessage(BlockedChatMessage msg, BlockedChatResponseTypes rt)
        {
            //If server blocked locally then do nothing.
            if (rt == BlockedChatResponseTypes.LocalServer)
                return;

        }

        /// <summary>
        /// Sends a message to a specific player.
        /// </summary>
        [Client]
        public bool SendDirectChatToServer(NetworkConnection target, string message)
        {
            NetworkConnection selfConn = base.ClientManager.Connection;
            if (!CanSendMessage(MessageType.Tell, selfConn, target, message, false, out _))
                return false;

            ServerSendChat(MessageType.Tell, target, message);
            return true;
        }


        /// <summary>
        /// Sends a message to team chat, or all players.
        /// </summary>
        [Client]
        public bool SendMultipleChatToServer(bool teamOnly, string message)
        {
            MessageType messageType = (teamOnly) ? MessageType.Team : MessageType.All;

            if (!CanSendMessage(messageType, base.ClientManager.Connection, null, message, false, out _))
                return false;

            ServerSendChat(messageType, null, message);
            return true;
        }


        /// <summary>
        /// Sends a message to a specific player, all, or teammates.
        /// </summary>
        [ServerRpc(RequireOwnership = false)]
        private void ServerSendChat(MessageType messageType, NetworkConnection target, string message, NetworkConnection sender = null)
        {
            IChatEntity senderEntity = GetChatEntity(sender);
            if (senderEntity == null)
                return;

            BlockedChatReason blockedReason;
            if (!CanSendMessage(messageType, sender, target, message, true, out blockedReason))
            {
                //Only send resaon if invalid target id.
                if (blockedReason == BlockedChatReason.InvalidTargetId)
                {
                    BlockedChatMessage msg = new BlockedChatMessage((ushort)messageType, sender, message, blockedReason);
                    TargetSendBlockedChatMessage(sender, msg);
                }
                return;
            }


            /* Invoke on server as well. This can be so we may hook into this
             * later if we want to store messages for review. */
            OnIncomingChatMessage?.Invoke(new ChatMessage((ushort)messageType, null, sender, message, false), true);

            /* //TODO
             * Make a team manager so that teams can be looked up by team Id, which would
             * return all players on that team.
             * 
             * EG: Dictionary<ushort, HashSet<NetworkConnection> _teams = new();
             * if (_teams.TryGetValue(10, out HashSet<NetworkConnection> playersOnTeam))
             *   //Send message to teammates by iterating players on team.
             *   
             * For the time being a team manager is not present so we will use the
             * team comparer option on the IChatEntity interface. */
            //Team.
            if (messageType == MessageType.Team)
            {
                HashSet<NetworkConnection> observers = base.Observers;
                foreach (NetworkConnection c in observers)
                {
                    IChatEntity observerEntity = GetChatEntity(c);
                    //Skip entry if enity could not be found for observer.
                    if (observerEntity == null)
                        continue;

                    //If sending to sender or teammate.
                    if (c == sender || (TeamType)senderEntity.GetTeamType(c) == TeamType.Friendly)
                        TargetReceiveGroupChat(c, messageType, sender, message);
                }
            }
            //All chat.
            else if (messageType == MessageType.All)
            {
                /* Send to all observers. A target rpc is used instead
                 * to keep the code base simpler. */
                foreach (NetworkConnection c in base.Observers)
                    TargetReceiveGroupChat(c, messageType, sender, message);
            }
            //Direct.
            else if (messageType == MessageType.Tell)
            {
                //Try to get target. If not able then do not send.
                IChatEntity targetEntity = GetChatEntity(target);
                if (targetEntity == null)
                    return;
                /* Send to sender and receiver. This is so sender
                 * can see that their chat was delivered. */
                //Send to the original sender so that they may see their message was sent.
                TargetSendDirectChat(sender, target, message);
                //Sent to the receiving target of the message.
                TargetReceiveDirectChat(target, sender, message);
            }
        }

        /// <summary>
        /// Sends a message to a specific player.
        /// </summary>
        [TargetRpc(ValidateTarget = false)]
        private void TargetReceiveGroupChat(NetworkConnection conn, MessageType messageType, NetworkConnection senderConn, string message)
        {
            if (senderConn == null)
                return;

            bool outbound = senderConn.IsLocalClient;
            //fix GlobalManager.SysOpManager.FilterChatString(ref message);
            OnIncomingChatMessage?.Invoke(new ChatMessage((ushort)messageType, conn, senderConn, message, outbound), false);
        }


        /// <summary>
        /// Sends a message to a specific player.
        /// </summary>
        [TargetRpc(ValidateTarget = false)]
        private void TargetReceiveDirectChat(NetworkConnection conn, NetworkConnection senderConn, string message)
        {
            if (senderConn == null)
                return;

            bool outbound = false;
            //fix GlobalManager.SysOpManager.FilterChatString(ref message);
            OnIncomingChatMessage?.Invoke(new ChatMessage((ushort)MessageType.Tell, conn, senderConn, message, outbound), false);
        }

        /// <summary>
        /// Sends a message to a specific player.
        /// </summary>
        [TargetRpc(ValidateTarget = false)]
        private void TargetSendDirectChat(NetworkConnection conn, NetworkConnection targetConn, string message)
        {
            if (targetConn == null)
                return;

            bool outbound = true;
            //fix GlobalManager.SysOpManager.FilterChatString(ref message);
            OnIncomingChatMessage?.Invoke(new ChatMessage((ushort)MessageType.Tell, conn, targetConn, message, outbound), false);
        }

        /// <summary>
        /// Called when the server informs a client that their chat was not sent.
        /// </summary>
        /// <param name="msg">Information about the blocked message.</param>
        [TargetRpc(ValidateTarget = false)]
        private void TargetSendBlockedChatMessage(NetworkConnection conn, BlockedChatMessage msg)
        {
            OnBlockedChatMessage?.Invoke(msg, false);
        }


    }


}
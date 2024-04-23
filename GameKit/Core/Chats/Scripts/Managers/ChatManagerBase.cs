using FishNet.Object;
using System.Collections.Generic;
using System;
using FishNet.Connection;
using UnityEngine;
using System.Runtime.CompilerServices;
using FishNet.Transporting;
using GameKit.Dependencies.Utilities;
using System.Text.RegularExpressions;
using System.Text;
using FishNet.Managing;
using Sirenix.OdinInspector;

namespace GameKit.Core.Chats.Managers
{

    /// <summary>
    /// Sets weaponIds on server objects.
    /// </summary>
    public abstract class ChatManagerBase : NetworkBehaviour
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
        [SerializeField, BoxGroup("Outbound")]
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
        /// <summary>
        /// Words to be filtered from chat.
        /// </summary>
        private Regex[] _filteredChatWords;
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

        /// <summary>
        /// Retrieve an IChatEntity from the cache.
        /// </summary>
        /// <returns></returns>
        protected abstract IChatEntity RetrieveIChatEntity();
        /// <summary>
        /// Store an IChatEntity to the cache.
        /// </summary>
        protected abstract void StoreIChatEntity(IChatEntity entity);
        /// <summary>
        /// Returns the messageType for direct messages.
        /// </summary>
        protected abstract ushort GetDirectMessageType();
        /// <summary>
        /// Returns the messageType for world messages.
        /// </summary>
        protected abstract ushort GetWorldMessageType();
        /// <summary>
        /// Returns the messageType for team messages.
        /// </summary>
        protected abstract ushort GetTeamMessageType();
        /// <summary>
        /// Returns if Sender can send a message to Target.
        /// This could be any message type, but the specified target may not want or be able to receive this message.
        /// </summary>
        /// <param name="sender">Client sending the message.</param>
        /// <param name="target">Client to receive the message. </param>
        /// <param name="messageType">MessageType being sent.</param>
        /// <param name="asServer">True if check is being performed on the server, false if on the client.</param>
        /// <returns>True if the message gets sent.</returns>
        protected abstract bool CanSendMessageToTarget(NetworkConnection sender, NetworkConnection target, ushort messageType, bool asServer);

        public override void OnStartNetwork()
        {
            base.NetworkManager.RegisterInstance(this);
        }

        public override void OnStartClient()
        {
            LoadFilteredChatWords(true);

            //No need to also track client side if server.
            if (!base.IsServerStarted)
            {
                //Listen for future clients.
                base.ClientManager.OnRemoteConnectionState += ClientManager_OnRemoteConnectionState;
                //Add all current clients as chat entities.
                foreach (NetworkConnection conn in base.ClientManager.Clients.Values)
                    AddChatEntity(conn, GetConnectionPlayerName(conn));
            }
        }

        public override void OnSpawnServer(NetworkConnection connection)
        {
            _clientChatFrequencies[connection] = ResettableObjectCaches<ChatFrequencyData>.Retrieve();
            AddChatEntity(connection, GetConnectionPlayerName(connection));
        }

        public override void OnDespawnServer(NetworkConnection connection)
        {
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
                StoreIChatEntity(ce);
            }
        }

        public override void OnStopClient()
        {
            base.ClientManager.OnRemoteConnectionState -= ClientManager_OnRemoteConnectionState;
        }

        public override void OnStopNetwork()
        {
            base.NetworkManager.UnregisterInstance<ChatManagerBase>();
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



        /// <summary>
        /// Loads bad words into memory.
        /// </summary>
        /// <param name="checkSpaces">True to also check for spaces between letters of bad words.</param>
        protected virtual void LoadFilteredChatWords(bool checkSpaces)
        {
            string words = @"pike?(ys?|ies)|||pakis?|||(ph|f)agg?s?([e0aio]ts?|oted|otry)|||nigg?s?|||nigg?[aeoi]s?|||(ph|f)[@a]gs?|||n[i!j1e]+gg?(rs?|ett?e?s?|lets?|ress?e?s?|r[a0oe]s?|[ie@ao0!]rs?|r[o0]ids?|ab[o0]s?|erest)|||j[!i]gg?[aer]+(boo?s?|b00?s?)|||jigg?[aer]+(b[0o]ing)|||p[0o]rch\\s*-?m[0o]nke?(ys?|ies?)|||g(ooks?|00ks?)|||k[iy]+kes?|||b[ea]ne[ry]s?|||(towel|rag)\\s*heads?|||wet\\s*backs?|||dark(e?y|ies?)|||(shit|mud)\\s*-?skins?|||tarbab(ys?|ies?)|||ape\\s*-?fricans?|||lesbos?|||coons?(y|i?e?s?|er)|||trann(ys?|ies?)|||mignorants?|||lady\\s*-?boys?|||spics?|||/?r?/?coon\\s*town|||/?r?/?ni?1?ggers?|||you\\s*('?re|r)gay|||shit\\s*lords?|||Homos?|||groids?|||chimpires?|||mud\\s*childr?e?n?|||n[1!i]gs?-?|||gays?(est|ly|er)|||dune\\s*coone?r?s?|||high\\s*yellows?|||shee?\\s*boons?|||cock\\s*suckers?|||tards?|||retards?|||retard\\*s?(ed|edly)|||cunts?y?|||dot\\s*heads?|||china\\s*m[ae]n|||queer\\s*bags?|||NAMBLA|||fucking\\s*(whores?)|||puss(y|ies?)|||whore\\s*mouth|||fuck\\s*boys?|||fat\\s*fucks?|||obeasts?|||fuck\\s*(wits?|tards?)|||beetusbehemoths?|||book\\s*fags?|||shit\\s*(bags?|dicks?)|||twats?|||fupas?|||holo\\s*hoaxe?s?|||Muslimes?|||dind[ous]|||boot\\s*lips?|||jig\\s*apes?|||nig\\s*town|||suspooks?|||cums?|||vags?|||whores?|||slutt?y?s?|||rapes?d?y?|||sex\\s*slaves?|||anuse?s?|||mast[ue]rbat(es?|ing|ion)|||action: report|||report_reason: naughty words - {{match}}|||terrori(sts?|m)|||jihadi?s?|||Allah|||islami?c?|||Islamist|||Muslims?|||sharia|||Israeli?s?|||false\\s*flags?|||shills?|||zionis(ts?|m)|||anti\\s*christ|||double\\s*thinks?|||censors?e?d?|||sjws?|||sheeple|||social\\s*justice|||justice\\s*wariors?|||Mossad|||new\\s*world\\s*orders?|||patriot\\s*act|||double\\s*speak|||sock\\s*puppets?|||/?r?/?subredditcancer|||fatwah|||anti-?\\s*semit(iecst)|||ban\\s*me|||liberal\\s*moron|||fat\\s*(asse?s?)|||cocks?|||shut\\s*the\\s*fuck\\s*up|||piece\\s*of\\s*shit|||fuck\\s*(you|yourself|yourselves|heads?|wads?|offs?|heads?|faces?)|||ass\\s*(wipes?|munch|clowns?|holes?)|||d[o0]uche\\s*(bags?|n[0o]zzles?|y|mods?)|||obese?(ity)|||over\\s*weight|||condescending|||pompous|||pricks?|||suck\\s*it|||stick\\s*up\\s*(their|there|thier|they'?r?e?)\\s*asse?s?|||ignorant|||wankers?|||jack\\s*(offs?|tards?|asse?s?)|||butt\\s*(munch|holes?|hurt)|||load\\s?of\\s*shit|||insulti?n?g\\s*(you|my)|||f[#u]+ck\\s*is\\s*wrong|||piss\\s*off|||suck\\s*it|||b[o0i]tche?s?(ing|ed|iness)|||an\\s*(ass)|||neck\\s*beards?|||passive\\s*aggressive|||dick\\s*weed|||you'?re?\\s*dumb|||pa?edos?|||pa?edophiles?|||(dumb)\\s*fucks?|||shit\\s*(dicks?|heads?)|||entitled|||fucking\\s*(idiots?|bitche?s?|morons?|dicks?|mouth)|||shit\\s*posts?|||diatribe|||self\\s*-?important|||elitis(t|m)|||patroniz(e|ing)|||dickish|||man\\s*child|||man\\s*children|||fedoras?|||m'lady|||autists?|||circle\\s*jerki?n?g?|||pedants?|||suck\\s*my]|||action: report|||report_reason: Fightin' words  - {{match}}|||J[3e]ws?|||kam[phf]|||kram[phf]|||hitler'?s?|||Adolf'?s?|||neo\\s*nazis?|||apes?|||bigot(s|ed)|||monk(ey|ies)|||gorillas?|||chimpanzees?|||interracial|||great\\s*apes?|||mud\\s*huts?|||baboons?|||white\\s*blood|||w?raci[s]+s|||urban\\s*thugs?|||white\\s*devils?|||racists?|||g[a]ys?|||white\\s*(supremacists?|man|males?|powers?)|||persons?\\s*of\\s*color|||race\\s*mixing|||ethnonationalist|||cis\\s*males?|||whites|||black\\s*(lies?|lives?)|||red\\s*pills?|||action: report|||chimp|||shekel|||goyim|||nuffin|||groid|||kill\\s*your(self|selves)|||commit\\s*suicide|||I\\s*hope\\s*(you|she|he)\\s*dies?|||^[a@][s\$][s\$]$|||[a@][s\$][s\$]h[o0][l1][e3][s\$]?|||b[a@][s\$][t\+][a@]rd |||b[e3][a@][s\$][t\+][i1][a@]?[l1]([i1][t\+]y)?|||b[e3][a@][s\$][t\+][i1][l1][i1][t\+]y|||b[e3][s\$][t\+][i1][a@][l1]([i1][t\+]y)?|||b[i1][t\+]ch[s\$]?|||b[i1][t\+]ch[e3]r[s\$]?|||b[i1][t\+]ch[e3][s\$]|||b[i1][t\+]ch[i1]ng?|||b[l1][o0]wj[o0]b[s\$]?|||c[l1][i1][t\+]|||^(c|k|ck|q)[o0](c|k|ck|q)[s\$]?$|||(c|k|ck|q)[o0](c|k|ck|q)[s\$]u|||(c|k|ck|q)[o0](c|k|ck|q)[s\$]u(c|k|ck|q)[e3]d |||(c|k|ck|q)[o0](c|k|ck|q)[s\$]u(c|k|ck|q)[e3]r|||(c|k|ck|q)[o0](c|k|ck|q)[s\$]u(c|k|ck|q)[i1]ng|||(c|k|ck|q)[o0](c|k|ck|q)[s\$]u(c|k|ck|q)[s\$]|||^cum[s\$]?$|||cumm??[e3]r|||cumm?[i1]ngcock|||(c|k|ck|q)um[s\$]h[o0][t\+]|||(c|k|ck|q)un[i1][l1][i1]ngu[s\$]|||(c|k|ck|q)un[i1][l1][l1][i1]ngu[s\$]|||(c|k|ck|q)unn[i1][l1][i1]ngu[s\$]|||(c|k|ck|q)un[t\+][s\$]?|||(c|k|ck|q)un[t\+][l1][i1](c|k|ck|q)|||(c|k|ck|q)un[t\+][l1][i1](c|k|ck|q)[e3]r|||(c|k|ck|q)un[t\+][l1][i1](c|k|ck|q)[i1]ng|||cyb[e3]r(ph|f)u(c|k|ck|q)|||d[a@]mn|||d[i1]ck|||d[i1][l1]d[o0]|||d[i1][l1]d[o0][s\$]|||d[i1]n(c|k|ck|q)|||d[i1]n(c|k|ck|q)[s\$]|||[e3]j[a@]cu[l1]|||(ph|f)[a@]g[s\$]?|||(ph|f)[a@]gg[i1]ng|||(ph|f)[a@]gg?[o0][t\+][s\$]?|||(ph|f)[a@]gg[s\$]|||(ph|f)[e3][l1][l1]?[a@][t\+][i1][o0]|||(ph|f)u(c|k|ck|q)|||(ph|f)u(c|k|ck|q)[s\$]?|||g[a@]ngb[a@]ng[s\$]?|||g[a@]ngb[a@]ng[e3]d|||g[a@]y|||h[o0]m?m[o0]|||h[o0]rny|||j[a@](c|k|ck|q)\-?[o0](ph|f)(ph|f)?|||j[e3]rk\-?[o0](ph|f)(ph|f)?|||j[i1][s\$z][s\$z]?m?|||[ck][o0]ndum[s\$]?|||mast(e|ur)b(8|ait|ate)|||n[i1]gg?[e3]r[s\$]?|||[o0]rg[a@][s\$][i1]m[s\$]?|||[o0]rg[a@][s\$]m[s\$]?|||p[e3]nn?[i1][s\$]|||p[i1][s\$][s\$]|||p[i1][s\$][s\$][o0](ph|f)(ph|f) |||p[o0]rn|||p[o0]rn[o0][s\$]?|||p[o0]rn[o0]gr[a@]phy|||pr[i1]ck[s\$]?|||pu[s\$][s\$][i1][e3][s\$]|||pu[s\$][s\$]y[s\$]?|||[s\$][e3]x|||[s\$]h[i1][t\+][s\$]?|||[s\$][l1]u[t\+][s\$]?|||[s\$]mu[t\+][s\$]?|||[s\$]punk[s\$]?|||[t\+]w[a@][t\+][s\$]?";

            StringBuilder sb = new StringBuilder();
            string[] wordsSplit = words.Split("|||");
            int length = wordsSplit.Length;
            _filteredChatWords = new Regex[length];

            for (int i = 0; i < length; i++)
            {
                //Modify string to check for spaces.
                if (checkSpaces)
                {
                    char prevChar = '.';
                    sb.Clear();
                    foreach (char item in wordsSplit[i])
                    {
                        //0-9 or a-z.
                        bool prevCharValid = CharIsValid(prevChar);
                        if (prevCharValid && CharIsValid(item))
                            sb.Append(@"\s*");

                        sb.Append(item);
                        prevChar = item;

                        //Returns if the previous char is a valid alphanumeric.
                        bool CharIsValid(char c)
                        {
                            return (c >= 48 && c <= 58)
                                || (c >= 97 && c <= 122);
                        }
                    }

                    _filteredChatWords[i] = new Regex(sb.ToString(), RegexOptions.IgnoreCase);
                }
                //Do not modify string to check for spaces.
                else
                {
                    _filteredChatWords[i] = new Regex(wordsSplit[i], RegexOptions.IgnoreCase);
                }
            }
        }

        /// <summary>
        /// Removes bad words from a string.
        /// </summary>
        /// <param name="value">String being filtered.</param>
        /// <param name="replaceText">Text to replace bad words with.</param>
        public virtual void FilterChatString(ref string value, string replaceText = "**")
        {
            Regex[] words = _filteredChatWords;
            foreach (Regex re in words)
                value = re.Replace(value, replaceText);
        }


        //TODO: make use actual name / client instance.
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
            IChatEntity ce = RetrieveChatEntity(conn, name);
            ChatEntities[conn] = ce;
        }

        /// <summary>
        /// Registers an existing IChatEntity with a connection.
        /// </summary>
        /// <param name="conn">Connection the entity is for.</param>
        /// <param name="ce">Entity to register to connection.</param>
        public void AddChatEntity(NetworkConnection conn, IChatEntity ce)
        {
            if (conn == null)
            {
                base.NetworkManager.LogError($"Connection cannot be null when registering a ChatEntity.");
                return;
            }
            if (ce == null || !ce.GetConnection().IsValid)
            {
                base.NetworkManager.LogError($"Entity cannot be null nor can the connection on entity be null.");
                return;
            }

            ChatEntities[conn] = ce;
        }
        /// <summary>
        /// Unregisters a connection from ChatEntities.
        /// </summary>
        /// <param name="conn">Connection to unregister.</param>
        public void UnregisterChatEntity(NetworkConnection conn)
        {
            if (conn == null)
            {
                base.NetworkManager.LogError($"Connection cannot be null when unregistering a ChatEntity.");
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
            if (conn == null)
            {
                base.NetworkManager.LogWarning($"Connection is null. A default {nameof(IChatEntity)} will be returned.");
                return default;
            }
            else
            {
                if (ChatEntities.TryGetValue(conn, out IChatEntity entity))
                    return entity;
                else
                    return default;
            }
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
        private IChatEntity RetrieveChatEntity(NetworkConnection conn, string name)
        {
            IChatEntity result = RetrieveIChatEntity();
            result.Initialize(conn, name);
            return result;
        }

        /// <summary>
        /// Returns if a message can be sent. Target may be null if not a direct message.
        /// </summary>
        protected virtual bool CanSendMessage(ushort messageType, NetworkConnection sender, NetworkConnection target, string message, bool asServer, out BlockedChatReason blockedReason)
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
            else if (messageType == GetDirectMessageType())
            {
                //Null target.
                if (target == null)
                    blockedReason = BlockedChatReason.InvalidTargetId;
                //Allow sending to self as many players use this to check if conection is alive.
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
        /// Sends a message to a specific player.
        /// </summary>
        [Client]
        public virtual bool SendDirectChatToServer(NetworkConnection target, string message)
        {
            ushort messageType = GetDirectMessageType();
            NetworkConnection sender = base.ClientManager.Connection;
            //Check for specific target blocks.
            if (!CanSendMessageToTarget(sender, target, messageType, false))
                return false;

            return SendChatToServer(sender, target, messageType, message);
        }

        /// <summary>
        /// Sends a message to players friendly to the local client.
        /// </summary>
        /// <returns>True if sent.</returns>
        public virtual bool SendTeamChatToServer(string message)
        {
            ushort messageType = GetTeamMessageType();
            return SendChatToServer(base.ClientManager.Connection, null, messageType, message);
        }

        /// <summary>
        /// Sends a message to players friendly to the local client.
        /// </summary>
        /// <returns>True if sent.</returns>
        public virtual bool SendWorldChatToServer(string message)
        {
            ushort messageType = GetWorldMessageType();
            return SendChatToServer(base.ClientManager.Connection, null, messageType, message);
        }

        /// <summary>
        /// Sends a chat to the server after sanity checks.
        /// </summary>
        /// <returns>True if sent.</returns>
        private bool SendChatToServer(NetworkConnection sender, NetworkConnection target, ushort messageType, string message)
        {
            /* Validate if target exist locally before sending.
            * This will prevent from hitting frequency limitations. */
            if (!CanSendMessage(messageType, sender, target, message, false, out _))
                return false;

            ServerSendChat(messageType, target, message);
            return true;
        }

        /// <summary>
        /// Sends a message to a specific player, all, or teammates.
        /// </summary>
        [ServerRpc(RequireOwnership = false)]
        private void ServerSendChat(ushort messageType, NetworkConnection target, string message, NetworkConnection sender = null)
        {
            IChatEntity senderEntity = GetChatEntity(sender);
            if (senderEntity == null)
                return;

            /* Internal blocking for exploit checks
             * and other abuse. */
            BlockedChatReason blockedReason;
            if (!CanSendMessage(messageType, sender, target, message, true, out blockedReason))
            {
                //Only send resaon if invalid target id.
                if (blockedReason == BlockedChatReason.InvalidTargetId)
                {
                    BlockedChatMessage msg = new BlockedChatMessage(messageType, sender, message, blockedReason);
                    TargetSendBlockedChatMessage(sender, msg);
                }
                return;
            }

            /* Invoke on server as well. This can be so we may hook into this
             * later if we want to store messages for review. */
            OnIncomingChatMessage?.Invoke(new ChatMessage(messageType, null, sender, message, false), true);

            /* If there is a target then it's a direct message.
             * Otherwise the message could be going to any number of
             * individuals. The developer(you) at this point would override
             * CanSendMessage to determine if the passed in connection can receive
             * the message. */
            if (messageType == GetDirectMessageType())
            {
                //Try to get target. If not able then do not send.
                IChatEntity targetEntity = GetChatEntity(target);
                if (targetEntity == null)
                    return;
                if (!CanSendMessageToTarget(sender, target, messageType, true))
                    return;
                /* Send to sender and receiver. This is so sender
                 * can see that their chat was delivered. */
                //Send to the original sender so that they may see their message was sent.
                TargetSendDirectChat(sender, target, message);
                //Sent to the receiving target of the message.
                TargetReceiveDirectChat(target, sender, message);
            }
            //Multiple targets.
            else
            {
                HashSet<NetworkConnection> observers = base.Observers;
                foreach (NetworkConnection c in observers)
                {
                    if (!CanSendMessageToTarget(sender, c, messageType, true))
                        continue;

                    TargetReceiveGroupChat(c, messageType, sender, message);
                }
            }
        }

        /// <summary>
        /// Sends a message to a specific player.
        /// </summary>
        [TargetRpc(ValidateTarget = false)]
        private void TargetReceiveGroupChat(NetworkConnection conn, ushort messageType, NetworkConnection senderConn, string message)
        {
            if (senderConn == null)
                return;

            bool outbound = senderConn.IsLocalClient;
            FilterChatString(ref message);
            OnIncomingChatMessage?.Invoke(new ChatMessage(messageType, conn, senderConn, message, outbound), false);
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
            FilterChatString(ref message);
            OnIncomingChatMessage?.Invoke(new ChatMessage(GetDirectMessageType(), conn, senderConn, message, outbound), false);
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
            FilterChatString(ref message);
            OnIncomingChatMessage?.Invoke(new ChatMessage(GetDirectMessageType(), conn, targetConn, message, outbound), false);
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
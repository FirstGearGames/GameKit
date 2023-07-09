using FishNet.Managing.Logging;
using FishNet.Managing.Server;
using FishNet.Object;
using UnityEngine;

namespace GameKit.Crafting
{

    public partial class Crafter : NetworkBehaviour
    {
        #region Private.
        /// <summary>
        /// The last time client failed a crafting action. -1 is unset.
        /// This is used to prevent excessive spoofed crafting attempts.
        /// </summary>
        private float _failedActionTime = -1f;
        /// <summary>
        /// Last time the client canceled a crafting. This is to prevent excessive cancel spam.
        /// </summary>
        private float _serverCancelTime = -1f;
        /// <summary>
        /// Number of times the client had consecutively canceled crafting a recipe.
        /// </summary>
        private int _serverCanceledCount;
        /// <summary>
        /// Current recipe being crafted on the server, and it's progress.
        /// Only one of these would be needed for dedicated servers, but for clientHost testing both are required.
        /// </summary>
        private CraftingProgress _serverCraftingProgress;
        /// <summary>
        /// Number of objects crafted in sequence on the server.
        /// Only one of these would be needed for dedicated servers, but for clientHost testing both are required.
        /// </summary>
        private int _serverSequentialCount;
        #endregion

        #region Const.
        /// <summary>
        /// How quickly another craft request must come through since the last to be considered sequential.
        /// </summary>
        private const float SERVER_SEQUENTIAL_TIME_LIMIT = 1f;
        /// <summary>
        /// How much time must pass between cancels to reset serverCanceledCount.
        /// </summary>
        private const float CANCEL_TIME_LIMIT = 10f;
        /// <summary>
        /// How much time must past between failed attempts for the client to not be kicked.
        /// </summary>
        private const float FAILED_TIME_LIMIT = 10f;
        #endregion

        public override void OnStartServer()
        {
            base.OnStartServer();
            _serverCraftingProgress = new CraftingProgress();
        }

        /// <summary>
        /// Cancels crafting of the current recipe.
        /// </summary>
        [ServerRpc]
        private void ServerCancelCrafting(IRecipe r)
        {
            //Did not deserialize, client did something bad.
            if (IsRecipeNull(r, true))
                return;
            //If crafting isn't active then confirm cancel.
            if (!_serverCraftingProgress.Active)
            {
                SendFailedCraftingResult(r, CraftingResult.Canceled);
                return;
            }
            //Can cancel.
            else
            {
                //This cancel happened very recently to another.
                if ((Time.unscaledTime - _serverCancelTime) < CANCEL_TIME_LIMIT)
                {
                    _serverCanceledCount++;
                    if (_serverCanceledCount > 2)
                    {
                        base.Owner.Kick(KickReason.UnusualActivity, LoggingType.Common, $"Connection Id {base.Owner.ClientId} has been kicked for too many crafting cancels.");
                        return;
                    }
                }
                //Enough tiem passed.
                else
                {
                    _serverCanceledCount = 0;
                }

                _failedActionTime = -1f;
                ResetCraftingProgress(true);
                TargetCraftingResult(base.Owner, r, CraftingResult.Canceled);
            }
        }

        /// <summary>
        /// Tries to craft a recipe for a client.
        /// </summary>
        [ServerRpc]
        private void ServerCraftRecipe(IRecipe r)
        {
            //Did not deserialize, client did something bad.
            if (IsRecipeNull(r, true))
                return;
            if (_serverCraftingProgress.Active)
            {
                SendFailedCraftingResult(r, CraftingResult.FullQueue);
                return;
            }
            /* Does not have the resources. Should not be possible for
             * client and server to desync on inventory. The client is
             * probably trying to cheat. */
            if (!HasCraftingResources(r))
                return;
            if (TryInvokeNoSpace(r, true))
                return;

            BeginCraftingRecipe(r, _serverCraftingProgress, ref _serverSequentialCount);
            OnCraftingStarted?.Invoke(r, true);
        }

        /// <summary>
        /// Sends a crafting result to owner and checks to take action for the failed result.
        /// </summary>
        [Server]
        private void SendFailedCraftingResult(IRecipe r, CraftingResult result)
        {
            //If owner is clientHost just send response.
            if (base.Owner.IsLocalClient)
            {
                TargetCraftingResult(base.Owner, r, result);
            }
            //Otherwise perform security checks.
            else
            {
                float unscaledTime = Time.unscaledTime;
                //Recently failed another craft attempt, likely trying to cheat.
                if (_failedActionTime != -1f && (unscaledTime - _failedActionTime) <= FAILED_TIME_LIMIT)
                {
                    base.Owner.Kick(KickReason.UnusualActivity, LoggingType.Common, $"Connection Id {base.Owner.ClientId} has been kicked for too many failed crafting attempts.");
                }
                //First failed attempt.
                else
                {
                    _failedActionTime = unscaledTime;
                    TargetCraftingResult(base.Owner, r, result);
                }
            }
        }

        /// <summary>
        /// Resets or increases the sequential count and returns the value.
        /// </summary>
        private int SetSequentialCount(IRecipe r, ref int sequentialCount)
        {
            //Recipe change.
            if (r != _lastCraftedRecipe)
                sequentialCount = 0;
            /* Too much time has passed since last.
             * Use the same time on client and server since
             * this check is not sensitive to timing. */
            else if ((Time.unscaledTime - _lastCompletedCraftTime) > SERVER_SEQUENTIAL_TIME_LIMIT)
                sequentialCount = 0;
            else
                sequentialCount++;

            return sequentialCount;
        }

        /// <summary>
        /// Called on the server when crafting completes.
        /// </summary>
        [Server(Logging = LoggingType.Off)]
        private void CraftingCompleted_Server(IRecipe r)
        {
            _lastCompletedCraftTime = Time.unscaledTime;
            _lastCraftedRecipe = r;
            _failedActionTime = -1f;
            ResetCraftingProgress(true);
            OnCraftingResult?.Invoke(r, CraftingResult.Completed, true);
            TargetCraftingResult(base.Owner, r, CraftingResult.Completed);
        }

        /// <summary>
        /// Returns if a recipe is null with the option to kick client if so.
        /// </summary>
        /// <param name="r"></param>
        /// <param name="kick"></param>
        /// <returns></returns>
        private bool IsRecipeNull(IRecipe r, bool kick)
        {
            bool isNull = (r == null);
            if (isNull && kick)
                base.Owner.Kick(KickReason.ExploitAttempt, LoggingType.Common, $"Connection Id {base.Owner.ClientId} has been kicked for sending a recipe which could not be deserialized.");

            return isNull;
        }
    }


}
namespace GameKit.Core.Crafting
{
    public enum CraftingResult : byte
    {
        /// <summary>
        /// Generic failed reason.
        /// </summary>
        Failed = 0,
        /// <summary>
        /// Canceled by server or client.
        /// </summary>
        Canceled = 1,
        /// <summary>
        /// Crafting is already in progress and no more may be queued.
        /// </summary>
        FullQueue = 2,
        /// <summary>
        /// Completed successfully.
        /// </summary>
        Completed = 3,
        /// <summary>
        /// Not enough space is available to accomodate recipe results.
        /// </summary>
        NoSpace = 4,
        /// <summary>
        /// Not enough resources to craft the recipe.
        /// </summary>
        NoResources = 5,
    }

}
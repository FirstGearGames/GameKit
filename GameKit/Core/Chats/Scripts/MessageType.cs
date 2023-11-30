namespace GameKit.Core.Chats
{
    /// <summary>
    /// Where a message is going to or coming from.
    /// </summary>
    public enum MessageType : ushort
    {
        Tell = 0,
        All = 1,
        Team = 2,
        System = 3,
    }

}
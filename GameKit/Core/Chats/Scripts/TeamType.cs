
namespace GameKit.Core.Chats
{
    /// <summary>
    /// What faction sent the message.
    /// </summary>
    public enum TeamType : ushort
    {
        Unset = 0,
        Self = 1,
        Enemy = 2,
        Friendly = 3,
    }

}
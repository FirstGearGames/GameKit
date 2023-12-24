using System;

namespace GameKit.Core.Quests
{
    [Flags]
    public enum QuestState
    {
        Unset = 0,
        Started = 1,
        Canceled = 2,
        Completed = 4,
        Failed = 8,
        Active = 16,
    }
   

}
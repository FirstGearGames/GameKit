using UnityEngine;

public class OldInputChatHotkeys : ChatHotkeys
{
    public override bool GetBackslashPressed()
    {
#if !ENABLE_INPUT_SYSTEM
        return Input.GetKeyDown(KeyCode.Backslash);
#else
        return false;
#endif
    }

    public override bool GetEnterPressed()
    {
#if !ENABLE_INPUT_SYSTEM
        return Input.GetKeyDown(KeyCode.Return);
#else
        return false;
#endif
    }

    public override bool GetEscapePressed()
    {
#if !ENABLE_INPUT_SYSTEM
        return Input.GetKeyDown(KeyCode.Escape);
#else
        return false;
#endif
    }

    public override bool GetSlashPressed()
    {
#if !ENABLE_INPUT_SYSTEM
        return Input.GetKeyDown(KeyCode.Slash);
#else
        return false;
#endif
    }

    public override bool GetTabPressed()
    {
#if !ENABLE_INPUT_SYSTEM
        return Input.GetKeyDown(KeyCode.Tab);
#else
        return false;
#endif
    }
}

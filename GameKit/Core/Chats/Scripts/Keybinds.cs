using GameKit.Core.Chats;
using GameKit.Dependencies.Utilities;
using UnityEngine;

#if ENABLE_INPUT_SYSTEM
using UnityEngine.InputSystem;
#endif

namespace GameKit.Core.Chats
{

    public class Keybinds : MonoBehaviour, IKeybinds
    {
        public virtual bool GetBackslashPressed()
        {
#if ENABLE_INPUT_SYSTEM
            return NewInput.GetButtonPressed(Key.Backslash);
#else
            return Input.GetKeyDown(KeyCode.Backslash);
#endif
        }

        public virtual bool GetEnterPressed()
        {
#if ENABLE_INPUT_SYSTEM
            return NewInput.GetButtonPressed(Key.Enter);
#else
            return Input.GetKeyDown(KeyCode.Return);
#endif
        }

        public virtual bool GetEscapePressed()
        {
#if ENABLE_INPUT_SYSTEM
            return NewInput.GetButtonPressed(Key.Escape);
#else
            return Input.GetKeyDown(KeyCode.Escape);
#endif
        }

        public virtual bool GetSlashPressed()
        {
#if ENABLE_INPUT_SYSTEM
            return NewInput.GetButtonPressed(Key.Slash);
#else
            return Input.GetKeyDown(KeyCode.Slash);
#endif
        }

        public virtual bool GetTabPressed()
        {
#if ENABLE_INPUT_SYSTEM
            return NewInput.GetButtonPressed(Key.Tab);
#else
            return Input.GetKeyDown(KeyCode.Tab);            
#endif
        }
    }

}
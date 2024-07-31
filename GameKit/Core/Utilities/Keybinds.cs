using UnityEngine;

#if NEW_INPUT_SYSTEM
using UnityEngine.InputSystem;
#endif

namespace GameKit.Core.Utilities
{

    public static class Keybinds
    {
        public static bool IsBackslashPressed
        {
            get
            {
#if NEW_INPUT_SYSTEM
                return NewInput.GetButtonPressed(Key.Backslash);
#else
                return Input.GetKeyDown(KeyCode.Backslash);
#endif
            }
        }

        public static bool IsEnterPressed
        {
            get
            {
#if NEW_INPUT_SYSTEM
                return NewInput.GetButtonPressed(Key.Enter);
#else
                return Input.GetKeyDown(KeyCode.Return);
#endif
            }
        }

        public static bool IsEscapePressed
        {
            get
            {
#if NEW_INPUT_SYSTEM
            return NewInput.GetButtonPressed(Key.Escape);
#else
                return Input.GetKeyDown(KeyCode.Escape);
#endif
            }
        }

        public static bool IsSlashPressed
        {
            get
            {

#if NEW_INPUT_SYSTEM
               return NewInput.GetButtonPressed(Key.Slash);
#else
                return Input.GetKeyDown(KeyCode.Slash);
#endif
            }
        }

        public static bool IsTabPressed
        {
            get
            {
#if NEW_INPUT_SYSTEM
                return NewInput.GetButtonPressed(Key.Tab);
#else
                return Input.GetKeyDown(KeyCode.Tab);
#endif
            }
        }

        public static bool IsShiftHeld
        {
            get
            {
#if NEW_INPUT_SYSTEM
                return (NewInput.GetButtonHeld(Key.LeftShift) || NewInput.GetButtonHeld(Key.RightShift);
#else
                return (Input.GetKey(KeyCode.LeftShift) || Input.GetKey(KeyCode.RightShift));
#endif
            }
        }
    }

}
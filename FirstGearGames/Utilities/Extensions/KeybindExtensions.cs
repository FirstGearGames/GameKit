//using OldFartGames.Menus.Configurations.Gameplay;
//using UnityEngine;
//using UnityEngine.InputSystem;

//public static class KeybindExtensions
//{    
//    /// <summary>
//    /// Returns how many entries can fit into a GridLayoutGroup
//    /// </summary>
//    /// <param name=""></param>
//    public static InputBinding GetInputBinding(this InputAction ia, bool modifier, KeybindSlots bindSlot)
//    {
//        int index = GetInputBindingIndex(modifier, bindSlot);
//        if (index >= ia.bindings.Count)
//        {
//            Debug.LogError($"Index of {index} is out of range of bindings count {ia.bindings.Count}.");
//            return default;
//        }

//        return ia.bindings[index];
//    }


//    /// <summary>
//    /// Returns the index for a bind type.
//    /// </summary>
//    /// <param name="modifier">True if a modifier key.</param>
//    /// <param name="bindSlot">KeybindSlot to get index for.</param>
//    /// <returns></returns>
//    public static int GetInputBindingIndex(bool modifier, KeybindSlots bindSlot)
//    {
//        if (bindSlot == KeybindSlots.Main)
//        {
//            return (modifier) ? 1 : 2;
//        }
//        else if (bindSlot == KeybindSlots.Alternative)
//        {
//            return (modifier) ? 4 : 5;
//        }
//        else
//        {
//            Debug.LogError($"Invalid bindSlot of {bindSlot.ToString()}");
//            return -1;
//        }
//    }

//    /// <summary>
//    /// Swaps two InputBindings.
//    /// </summary>
//    public static void SwapInputBinding(this InputAction ia, int indexA, int indexB)
//    {
//        InputBinding a = ia.bindings[indexA];
//        ia.ApplyBindingOverride(indexA, ia.bindings[indexB]);
//        ia.ApplyBindingOverride(indexB, a);
//    }

//    /// <summary>
//    /// Clears an InputBinding index.
//    /// </summary>
//    public static void ClearInputBinding(this InputAction ia, int index)
//    {
//        ia.ApplyBindingOverride(index, string.Empty);
//    }

//}

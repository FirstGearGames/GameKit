namespace GameKit.Core.Resources
{
    /// <summary>
    /// Categories a resource may be used for.
    /// A resource could have multiple categories, such as food and crafting.
    /// </summary>
    [System.Flags]
    public enum ResourceCategory : uint
    {        
        Unset = 0,
        //Junk.
        Scrap = 1,
        //Can be used in crafting.
        Crafting = 2,
        //Can be used as food, potions, and so on.
        Consumable = 4,
        //Can be a weapon.
        Weapon = 8,
        //Can be equipped on characters.
        Equipped = 16,
    }

}
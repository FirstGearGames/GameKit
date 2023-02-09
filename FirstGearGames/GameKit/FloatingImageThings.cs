
using UnityEngine;

public enum SpaceType : byte
{
    /// <summary>
    /// Object will use world space components.
    /// </summary>
    World = 0,
    /// <summary>
    /// Object will use userinterface components.
    /// </summary>
    UserInterface = 1,
}

public struct FloatingImageSettings
{
    /// <summary>
    /// Space to initialize the object for.
    /// </summary>
    public SpaceType SpaceType;
    /// <summary>
    /// Size to use for the renderer. If left null the sprite's size will be used.
    /// </summary>
    public Vector3? Size;

    public FloatingImageSettings(SpaceType spaceType, Vector3? size)
    {
        SpaceType = spaceType;
        Size = size;
    }
}

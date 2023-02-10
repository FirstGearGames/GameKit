using FishNet;
using System.Runtime.CompilerServices;
using UnityEngine;


public class FloatingContainer : MonoBehaviour
{
    /// <summary>
    /// True if not visible, or in the process of resetting.
    /// </summary>
    public bool IsHiding =>  (!IsVisible || IsResetting);
    /// <summary>
    /// True if visible. Could be true if in the progress of resetting as well; see IsResetting and IsHiding.
    /// </summary>
    public bool IsVisible { get; protected set; }
    /// <summary>
    /// True if this floating container is resetting.
    /// </summary>
    public bool IsResetting { get; protected set; }

    /// <summary>
    /// How quickly to move towards the starting position and rotation when calling reset.
    /// </summary>
    [SerializeField]
    protected float ResetSpeed = 5f;

    /// <summary>
    /// Start position of when this was initialized.
    /// </summary>
    private Vector3 _startPosition;
    /// <summary>
    /// Start rotation of when this was initialized.
    /// </summary>
    private Quaternion _startRotation;


    /// <summary>
    /// Attachs a gameObject as a child of this object.
    /// </summary>
    /// <param name="go">GameObject to attach.</param>
    public void AttachGameObject(GameObject go)
    {
        if (go == null)
            return;

        Transform goT = go.transform;
        goT.SetParent(transform);
        goT.localPosition = Vector3.zero;
        goT.localRotation = Quaternion.identity;
        goT.localScale = Vector3.one;
    }

    //Shows with new sprite.
    public virtual void Show(Vector3 position, Quaternion rotation, Vector3 scale)
    {
        _startPosition = position;
        _startRotation = rotation;
        UpdatePositionAndRotation(position, rotation);
        //Set root active.
        gameObject.SetActive(true);
        IsVisible = true;
    }
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public virtual void Show(Vector3 position)
    {
        Show(position, Quaternion.identity, Vector3.one);
    }
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public virtual void Show(Vector3 position, Quaternion rotation)
    {
        Show(position, rotation, Vector3.one);
    }
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public virtual void Show(Transform startingPoint)
    {
        if (startingPoint == null)
        {
            InstanceFinder.NetworkManager.LogError($"A null Transform cannot be used as the starting point.");
            return;
        }

        Show(startingPoint.position, startingPoint.rotation, startingPoint.localScale);
    }

    public void Hide()
    {
        IsResetting = false;
        IsVisible = false;
        gameObject.SetActive(false);
    }

    //presumed world space if world object, or mouse space if not.
    public virtual void UpdatePosition(Vector3 position)
    {
        transform.position = position;
        //if (_worldObject)
        //{
        //    WorldRoot.transform.position = position;
        //}
        //else
        //{
        //    //position.z = 0f;
        //    RectTransform rt = _imageRenderer.GetComponent<RectTransform>();
        //    rt.position = position;
        //}
    }

    public virtual void UpdateRotation(Quaternion rotation)
    {
        transform.rotation = rotation;
    }

    //presumed world space if world object, or mouse space if not.
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public virtual void UpdatePositionAndRotation(Vector3 position, Quaternion rotation)
    {
        UpdatePosition(position);
        UpdateRotation(rotation);
    }
}

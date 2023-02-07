using System.Runtime.CompilerServices;
using UnityEngine;
using UnityEngine.UI;

public class DraggableImage : MonoBehaviour
{
    public GameObject WorldRoot;
    public GameObject UiRoot;
    //UI renderer.
    [SerializeField]
    private Image _imageRenderer;
    //World space renderer
    [SerializeField]
    private SpriteRenderer _spriteRenderer;
    //How quickly to move towards reset position when resetting.
    [SerializeField]
    private float _resetSpeed = 5f;

    private Vector3 _startPosition;
    private Quaternion _startRotation;
    private bool _worldObject;

    //Shows with new sprite.
    public void Show(Sprite sprite, bool worldObject, Vector3 position, Quaternion rotation)
    {
        _worldObject = worldObject;
        WorldRoot.SetActive(worldObject);
        UiRoot.SetActive(!worldObject);

        _spriteRenderer.sprite = sprite;
        _imageRenderer.sprite = sprite;
        Vector2 imageUnitSize = (sprite.bounds.size * sprite.pixelsPerUnit) * 2f;
        _imageRenderer.rectTransform.sizeDelta = imageUnitSize;

        _startPosition = position;
        _startRotation = rotation;
        UpdatePositionAndRotation(position, rotation);
        gameObject.SetActive(true);
    }

    //Show with the current sprite.
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public void Show(Vector3 position, Quaternion rotation)
    {
        Show(_spriteRenderer.sprite, _worldObject, position, rotation);
    }

    public void Hide()
    {
        gameObject.SetActive(false);
    }

    //presumed world space if world object, or mouse space if not.
    public void UpdatePosition(Vector3 position)
    {
        if (_worldObject)
        {
            WorldRoot.transform.position = position;
        }
        else
        {
            //position.z = 0f;
            RectTransform rt = _imageRenderer.GetComponent<RectTransform>();
            rt.position = position;
        }
    }

    public void UpdateRotation(Quaternion rotation)
    {
        transform.rotation = rotation;
    }

    //presumed world space if world object, or mouse space if not.
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public void UpdatePositionAndRotation(Vector3 position, Quaternion rotation)
    {
        UpdatePosition(position);
        UpdateRotation(rotation);
    }
}

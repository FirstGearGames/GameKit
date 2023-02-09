using FishNet;
using System.Runtime.CompilerServices;
using UnityEngine;
using UnityEngine.UI;


public class FloatingImage : MonoBehaviour
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


    private FloatingImageSettings _settings;


    //Shows with new sprite.
    public void Show(Sprite sprite, FloatingImageSettings settings, Vector3 position, Quaternion rotation)
    {
        _settings = settings;

        //Disable both then enable as needed.
        WorldRoot.SetActive(false);
        UiRoot.SetActive(false);

        //Size for the renderer.
        Vector3 size = (settings.Size == null)
            ? (sprite.bounds.size * sprite.pixelsPerUnit)
            : settings.Size.Value;

        bool worldSpace = (settings.SpaceType == SpaceType.World);

        if (worldSpace)
        {
            WorldRoot.SetActive(true);
            _spriteRenderer.sprite = sprite;
            _spriteRenderer.size = size;
        }
        else
        {
            UiRoot.SetActive(true);
            _imageRenderer.sprite = sprite;
            _imageRenderer.rectTransform.sizeDelta = size;
        }

        _startPosition = position;
        _startRotation = rotation;
        UpdatePositionAndRotation(position, rotation);
        //Set root active.
        gameObject.SetActive(true);
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public void Show(Sprite sprite, FloatingImageSettings settings, Transform startingPoint)
    {
        if (startingPoint == null)
        {
            InstanceFinder.NetworkManager.LogError($"A null Transform cannot be used as the starting point.");
            return;
        }

        Show(sprite, settings, startingPoint.position, startingPoint.rotation);
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public void Show(Sprite sprite, FloatingImageSettings settings, RectTransform startingPoint)
    {
        if (startingPoint == null)
        {
            InstanceFinder.NetworkManager.LogError($"A null RectTransform cannot be used as the starting point.");
            return;
        }

        Show(sprite, settings, startingPoint.position, startingPoint.rotation);
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

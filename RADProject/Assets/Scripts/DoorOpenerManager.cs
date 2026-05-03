using Unity.VisualScripting;
using UnityEngine;

public class DoorOpenerManager : MonoBehaviour
{
    public enum DoorType
    {
        Wooden,
        Iron
    }

    public DoorType _doorType;

    [SerializeField] private BoxCollider2D _collider2D;

    [SerializeField] private SpriteRenderer _spriteRenderer;
    // Start is called once before the first execution of Update after the MonoBehaviour is created
    void Start()
    {
        _collider2D = GetComponent<BoxCollider2D>();
        _spriteRenderer = GetComponent<SpriteRenderer>();
    }

    public void OpenDoor()
    {
        if (_doorType == DoorType.Iron)
        {
            AudioManager.Instance.Play(SoundType.Door_Open);
            _spriteRenderer.enabled = false;
        }
        else
        {
            AudioManager.Instance.Play(SoundType.Door_Open);
            _collider2D.enabled = false;
            _spriteRenderer.enabled = false;
        }
        
    }

    public void CloseDoor()
    {
        if (_doorType == DoorType.Iron)
        {
            AudioManager.Instance.Play(SoundType.Door_Close);
            _spriteRenderer.enabled = true;
        }
    }
}

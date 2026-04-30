using System.Collections;
using TMPro;
using UnityEngine;

public class TextDestroyer : MonoBehaviour
{
    [SerializeField] private float _moveSpeed = 10f;
    [SerializeField] private float _lifetime = 4f;
    
    private RectTransform _rectTransform;
    private Vector2 _moveDirection = Vector2.up;
    
    void Start()
    {
        _rectTransform = GetComponent<RectTransform>();
        Destroy(gameObject, _lifetime);
    }
    
    void Update()
    {
        // Move in the set direction
        _rectTransform.anchoredPosition += _moveDirection * _moveSpeed * Time.deltaTime;
    }
    
    public void SetText(string text)
    {
        // If using TextMeshPro
        TextMeshProUGUI tmp = GetComponent<TextMeshProUGUI>();
        if (tmp != null)
            tmp.text = text;
    }
    
    public void SetMoveDirection(Vector2 direction)
    {
        _moveDirection = direction.normalized; // Normalize to keep consistent speed
    }
}

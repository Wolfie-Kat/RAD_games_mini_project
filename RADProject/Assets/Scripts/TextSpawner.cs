using System.Collections.Generic;
using UnityEngine;

public class TextSpawner : MonoBehaviour
{
    [SerializeField] private GameObject _textPrefab;
    [SerializeField] private List<string> _phrases;
    
    // Start is called once before the first execution of Update after the MonoBehaviour is created
    void Start()
    {
        InvokeRepeating("SpawnText", 0f, 0.25f);
    }

    private void SpawnText()
    {
        GameObject newText = Instantiate(_textPrefab, gameObject.transform);
        RectTransform textRect = newText.GetComponent<RectTransform>();
        float randomX = Random.Range(-400f, 400f);
        float randomY = Random.Range(-200f, 200f);
        textRect.anchoredPosition = new Vector2(randomX, randomY);

        TextDestroyer textDestroyer = newText.GetComponent<TextDestroyer>();
        textDestroyer.SetText(_phrases[Random.Range(0, _phrases.Count)]);
        Vector2 randomDirection = new Vector2(Random.Range(-1f, 1f), Random.Range(-1f, 1f)).normalized;
        textDestroyer.SetMoveDirection(randomDirection);
    }
}

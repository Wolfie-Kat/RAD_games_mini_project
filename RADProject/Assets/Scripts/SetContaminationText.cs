using TMPro;
using UnityEngine;

public class SetContaminationText : MonoBehaviour
{
    [SerializeField] private PlayerMovement _player;
    // Start is called once before the first execution of Update after the MonoBehaviour is created
    void Start()
    {
        _player = FindFirstObjectByType<PlayerMovement>().GetComponent<PlayerMovement>();
    }

    // Update is called once per frame
    void Update()
    {
        GetComponent<TextMeshProUGUI>().text = $"{_player.Contamination}%";
    }
}

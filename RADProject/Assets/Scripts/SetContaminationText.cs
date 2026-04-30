using TMPro;
using UnityEngine;

public class SetContaminationText : MonoBehaviour
{
    [SerializeField] private PlayerMovement _player;
    [SerializeField] private TextMeshProUGUI _contamination;
    [SerializeField] private TextMeshProUGUI _maxReturns;
    [SerializeField] private TextMeshProUGUI _currentReturns;
    // Start is called once before the first execution of Update after the MonoBehaviour is created
    void Start()
    {
        _player = FindFirstObjectByType<PlayerMovement>().GetComponent<PlayerMovement>();
    }

    // Update is called once per frame
    void Update()
    {
        _contamination.text = $"{_player.Contamination}%";
        _currentReturns.text = $"{_player.CurrentReturns}";
        _maxReturns.text = $"{_player.MaxReturns}";
    }
}

using System.Collections;
using System.Collections.Generic;
using TMPro;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.Tilemaps;
using UnityEngine.UI;

public class PlayerMovement : MonoBehaviour
{
    [Header("Movement Settings")] 
    [SerializeField] private float _moveSpeed = 5f;
    [SerializeField] private LayerMask _obstacleMask;
    [SerializeField] private LayerMask _doorMask;
    [SerializeField] private LayerMask _washingMask;
    [SerializeField] private LayerMask _finishMask;

    [Header("Player Stats")] 
    public int Contamination;
    public int MaxReturns;
    public int CurrentReturns;
    public string CurrentKey;
    public float CleaningSatisfaction;

    [Header("Path Taken")] 
    [SerializeField] private List<Vector2> _path;

    [Header("References")] 
    [SerializeField] private Grid _grid;
    [SerializeField] private Slider _slider;
    [SerializeField] private GameObject _playerCanvas;
    [SerializeField] private TextMeshProUGUI _keyText;
    [SerializeField] private CanvasGroup _fadein;
    [SerializeField] private TypeWriterScript _typeWriterScript;
    private Vector2 _targetPos;
    private bool _isMoving;
    private bool _contaminated;
    private bool _onDoor;
    private bool _isCleaning;
    private DoorOpenerManager _currentDoorManager;
    private float _cleaningMinigameTimer;
    private float _cleaningMinigameTimerMax;
    private int _minigameProgress;
    private int _minigameMaxProgress;
    
    // Start is called once before the first execution of Update after the MonoBehaviour is created
    void Awake()
    {
        CurrentReturns = 0;
        // If no grid is assigned, try to find one in the scene
        if (_grid == null)
        {
            _grid = FindFirstObjectByType<Grid>();
        }

        _typeWriterScript = _fadein.gameObject.transform.Find("Text").gameObject.GetComponent<TypeWriterScript>();
        
        // Snap the player to the nearest grid cell at start
        transform.position = GetSnappedPosition(transform.position);
        _targetPos = transform.position;
        StartCoroutine(Fade(false, true));
    }

    // Update is called once per frame
    void Update()
    {
        if (_isCleaning)
        {
            _cleaningMinigameTimer += Time.deltaTime;
            CleaningSatisfaction -= 4 * Time.deltaTime;
            _slider.value = CleaningSatisfaction;
            if (Input.GetKeyDown(CurrentKey))
            {
                if (CleaningSatisfaction < 10)
                {
                    CleaningSatisfaction += 1f;
                }

                if (CleaningSatisfaction > 10)
                {
                    CleaningSatisfaction = 10;
                }
            }

            if (_cleaningMinigameTimer >= _cleaningMinigameTimerMax)
            {
                _cleaningMinigameTimer = 0;
                _cleaningMinigameTimerMax = Random.Range(4, 7);
                GenerateNewLetter();
                _minigameProgress++;
            }

            if (CleaningSatisfaction <= 0)
            {
                if (_typeWriterScript.StartedTyping == false)
                {
                    StartCoroutine(Fade(true, false,
                    "You spiral because you did not clean yourself properly, leading to excessive cleaning and wasting too much time."));
                }
            }

            if (_minigameProgress == _minigameMaxProgress)
            {
                _isCleaning = false;
                CurrentKey = "";
                CurrentReturns++;
                _playerCanvas.SetActive(false);
            }
            return;
        }
        if (Contamination == 100)
        {
            Contamination = 0;
            _moveSpeed *= 2;
            
            StartCoroutine(FollowPath());
        }

        if (CurrentReturns == MaxReturns)
        {
            if (_typeWriterScript.StartedTyping == false)
            {
                StartCoroutine(Fade(true, false,
                "You cleaned yourself too many times and as such you missed your psychiatrist appointment"));
            }
        }
        
        // Only accept input when not already moving
        if (_isMoving || _contaminated) return;

        Vector2 inputDirection = GetInputDirection();
        if (inputDirection != Vector2.zero)
        {
            Vector2 targetCell = _targetPos + inputDirection * _grid.cellSize;
            if (CanMoveTo(targetCell))
            {
                StartCoroutine(MoveToTarget(targetCell));
                _path.Add(transform.position);
                if (IsDoorTile(targetCell))
                {
                    SetContamination(20);
                }
                else
                {
                    if (_onDoor)
                    {
                        CloseDoor();
                    }
                }

                if (IsWashingTile(targetCell))
                {
                    _isCleaning = true;
                    _playerCanvas.SetActive(true);
                    CleaningSatisfaction = 10;
                    GenerateNewLetter();
                    _cleaningMinigameTimerMax = Random.Range(4, 6);
                    _minigameMaxProgress = Random.Range(4, 6);
                    _minigameProgress = 0;
                    Contamination = 0;
                    _path.Clear();
                }

                if (IsFinishTile(targetCell))
                {
                    if (_typeWriterScript.StartedTyping == false)
                    {
                        StartCoroutine(Fade(true, true, "You leave your house and head for the psychiatrist office.", "Level 2"));
                    }
                }
            }
        }
    }

    /// <summary>Converts WASD / arrow keys into a grid‑aligned direction.</summary>
    private Vector2 GetInputDirection()
    {
        float horizontal = Input.GetAxisRaw("Horizontal");
        float vertical = Input.GetAxisRaw("Vertical");
        
        // Prioritise horizontal movement over vertical (stops diagonal input)
        if (Mathf.Abs(horizontal) > 0.1f)
        {
            return new Vector2(Mathf.Sign(horizontal), 0f) * _grid.cellSize.x;
        }
        else if (Mathf.Abs(vertical) > 0.1f)
        {
            return new Vector2(0f, Mathf.Sign(vertical)) * _grid.cellSize.y;
        }
        
        return Vector2.zero;
    }

// Modified IsDoorTile method
    private bool IsDoorTile(Vector2 worldPosition)
    {
        float checkRadius = _grid.cellSize.x * 0.4f;
        Collider2D hit = Physics2D.OverlapCircle(worldPosition, checkRadius, _doorMask);
    
        if (hit == null) return false;
    
        DoorOpenerManager doorOpenerManager = hit.GetComponent<DoorOpenerManager>();
        if (doorOpenerManager == null) return false;
    
        doorOpenerManager.OpenDoor();
    
        if (doorOpenerManager._doorType == DoorOpenerManager.DoorType.Iron)
        {
            _onDoor = true;
            _currentDoorManager = doorOpenerManager; // Store reference
        }
    
        return true;
    }

    // Simplified CloseDoor method
    private void CloseDoor()
    {
        if (_currentDoorManager != null)
        {
            _currentDoorManager.CloseDoor();
            _currentDoorManager = null;
        }
        _onDoor = false;
    }
    
    private bool IsWashingTile(Vector2 worldPosition)
    {
        float checkRadius = _grid.cellSize.x * 0.4f;
        Collider2D hit = Physics2D.OverlapCircle(worldPosition, checkRadius, _washingMask);
        return hit;
    }

    private bool IsFinishTile(Vector2 worldPosition)
    {
        float checkRadius = _grid.cellSize.x * 0.4f;
        Collider2D hit = Physics2D.OverlapCircle(worldPosition, checkRadius, _finishMask);
        return hit;
    }

    /// <summary>Checks if the destination cell is free of obstacles.</summary>
    private bool CanMoveTo(Vector2 worldPosition)
    {
        // Simple overlap check using a small circle (or box) at the target cell centre.
        // Adjust the check radius to match your collider size.
        float checkRadius = _grid.cellSize.x * 0.4f;
        Collider2D hit = Physics2D.OverlapCircle(worldPosition, checkRadius, _obstacleMask);
        return hit == null;
    }

    /// <summary>Smoothly moves the player from current position to target.</summary>
    private IEnumerator MoveToTarget(Vector2 newTarget)
    {
        _isMoving = true;
        _targetPos = newTarget;
        while (Vector2.Distance(transform.position, _targetPos) > 0.01f)
        {
            transform.position = Vector2.MoveTowards(transform.position, _targetPos, _moveSpeed * Time.deltaTime);
            yield return null;
        }

        transform.position = _targetPos;
        _isMoving = false;
    }

    private void GenerateNewLetter()
    {
        string[] availableLetters = new[]
        {
            "a", "b", "c", "d", "e", "f", "g", "h", "i", "j", "k", "l", "m", "n", "o", "p", "q", "r", "s", "t", "u",
            "v", "w", "x", "y", "z"
        };

        CurrentKey = availableLetters[Random.Range(0, availableLetters.Length - 1)];
        _keyText.text = $"Press \"{CurrentKey.ToUpper()}\"!";
    }

    private IEnumerator FollowPath()
    {
        _contaminated = true;
        print("Following Path!");
        for (int i = _path.Count - 1; i >= 0; i--)
        {
            StartCoroutine(MoveToTarget(_path[i]));
            yield return new WaitWhile(() => _isMoving);
            _path.Remove(_path[i]);
        }
        
        if (_path.Count == 0)
        {
            if (_typeWriterScript.StartedTyping == false)
            {
                StartCoroutine(Fade(true, false,
                "You feel too contaminated and use too much time trying to clean yourself. You miss your psychiatrist appointment."));
            }
        }
    }

    // Better structure:
    private IEnumerator Fade(bool fadeOut, bool win, string text = "", string sceneName = "")
    {
        if (fadeOut)
        {
            yield return StartCoroutine(FadeInCoroutine(1f));
            if (_typeWriterScript.StartedTyping == false)
            {
                yield return StartCoroutine(_typeWriterScript.StartTyping(text));
            }
        }
        else
        {
            yield return StartCoroutine(FadeOutCoroutine(1f));
        }
    
        // Handle scene transition AFTER all fades and typing
        if (_typeWriterScript.DoneTyping)
        {
            if (win)
                SceneManager.LoadScene(sceneName);
            else
                ReloadScene();
        }
    }

    private IEnumerator FadeInCoroutine(float duration)
    {
        float elapsed = 0;
        float startAlpha = _fadein.alpha;
    
        while (elapsed < duration)
        {
            elapsed += Time.deltaTime;
            _fadein.alpha = Mathf.Lerp(startAlpha, 1f, elapsed / duration);
            yield return null;
        }
        _fadein.alpha = 1f;
    }

    private IEnumerator FadeOutCoroutine(float duration)
    {
        float elapsed = 0;
        float startAlpha = _fadein.alpha;
    
        while (elapsed < duration)
        {
            elapsed += Time.deltaTime;
            _fadein.alpha = Mathf.Lerp(startAlpha, 0f, elapsed / duration);
            yield return null;
        }
        _fadein.alpha = 0f;
    }

    private void ReloadScene()
    {
        string currentSceneName = SceneManager.GetActiveScene().name;
        SceneManager.LoadScene(currentSceneName);
    }

    /// <summary>Snaps a world position to the centre of its grid cell.</summary>
    private Vector2 GetSnappedPosition(Vector2 worldPos)
    {
        Vector3Int cellPos = _grid.WorldToCell(worldPos);
        return _grid.GetCellCenterWorld(cellPos);
    }

    private void SetContamination(int amount)
    {
        Contamination += amount;
    }
}

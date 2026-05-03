using System.Collections;
using System.Collections.Generic;
using TMPro;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.Serialization;
using UnityEngine.Tilemaps;
using UnityEngine.UI;

public class PlayerMovement : MonoBehaviour
{
    [Header("Movement Settings")]
    [SerializeField] private float _moveSpeed = 1f;
    [SerializeField] private LayerMask _obstacleMask;
    [SerializeField] private LayerMask _doorMask;
    [SerializeField] private LayerMask _washingMask;
    [SerializeField] private LayerMask _finishMask;
    [SerializeField] private LayerMask _psychMask;

    [Header("Player Stats")]
    public int Contamination;
    public int MaxReturns;
    public int CurrentReturns;
    public string CurrentKey;
    public float CleaningSatisfaction;
    public bool TalkedToPsychiatrist;

    [Header("Path Taken")]
    [SerializeField] private List<Vector2> _path;

    [Header("References")]
    [SerializeField] private Grid _grid;
    [SerializeField] private Slider _minigameSlider;
    [SerializeField] private Slider _contaminationSlider;
    [SerializeField] private GameObject _playerCanvas;
    [SerializeField] private TextMeshProUGUI _keyText;
    [SerializeField] private CanvasGroup _fadein;
    [SerializeField] private CanvasGroup _whispers;
    [SerializeField] private TypeWriterScript _typeWriterScript;
    [SerializeField] private FullscreenOverlay overlay;
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
    private bool _stepAlternate = true;
    private bool isInteracting = false;
    private Animator animator;
    private SpriteRenderer sr;
    private bool _isCoping;
    private bool _isCopingTriggered;
    private float _copingCooldownTimer;
    private bool _onCopingCooldown;

    // Start is called once before the first execution of Update after the MonoBehaviour is created
    void Awake()
    {
        AmbianceManager();

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

        animator = GetComponent<Animator>();
        sr = GetComponent<SpriteRenderer>();
    }

    // Update is called once per frame
    void Update()
    {
        if (_isCleaning)
        {
            if (SceneManager.GetActiveScene().name.Contains("1") || SceneManager.GetActiveScene().name.Contains("2") || SceneManager.GetActiveScene().name == "Tutorial")
            {
                MovementAnimationManager(0, 1);
            }
            else
            {
                MovementAnimationManager(0, 0);
            }
            _cleaningMinigameTimer += Time.deltaTime;
            CleaningSatisfaction -= 4 * Time.deltaTime;
            _minigameSlider.value = CleaningSatisfaction;
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
                        "You did not clean yourself fast enough and missed your psychiatrist appointment."));
                }
            }

            if (_minigameProgress == _minigameMaxProgress)
            {
                _isCleaning = false;
                CurrentKey = "";
                CurrentReturns++;
                overlay.SetContamination(0);
                _playerCanvas.SetActive(false);
                WashingStop();
            }
            return;
        }
        if (Contamination > 0)
        {
            AudioManager.Instance.Ambience(SoundType.Whispers);
            if (Contamination >= 20)
            {
                AudioManager.Instance.SetAmbienceVolume(SoundType.Whispers, 0.1f);
            }
            if (Contamination >= 40)
            {
                AudioManager.Instance.SetAmbienceVolume(SoundType.Whispers, 0.2f);
            }
            if (Contamination >= 60)
            {
                AudioManager.Instance.SetAmbienceVolume(SoundType.Whispers, 0.3f);
            }
            if (Contamination >= 80)
            {
                AudioManager.Instance.SetAmbienceVolume(SoundType.Whispers, 0.4f);
            }
            if (Contamination >= 100 && !TalkedToPsychiatrist)
            {
                AudioManager.Instance.Play(SoundType.Sudden_Bass);
            }
        }
        
        // Fix the contamination check:
        if (Contamination >= 100 && !_isCopingTriggered && !_onCopingCooldown)
        {
            Contamination = 100; // Clamp to 100
    
            if (TalkedToPsychiatrist == false)
            {
                Contamination = 0;
                _moveSpeed *= 2;
                StartCoroutine(FollowPath());
                StartCoroutine(Fade(true, false,
                    "You feel too contaminated and miss your psychiatrist appointment.", 2f));
            }
            else
            {
                if (_isCoping == false && _fadein.alpha <= 0.1f)
                {
                    StartCoroutine(StartCopeMinigame());
                }
            }
        }

        if (_isCoping)
        {
            _cleaningMinigameTimer += Time.deltaTime;
            CleaningSatisfaction -= 4 * Time.deltaTime;
            _minigameSlider.value = CleaningSatisfaction;
    
            if (Input.GetKeyDown(CurrentKey))
            {
                if (CleaningSatisfaction < 10)
                {
                    CleaningSatisfaction += 1f;
                }

                if (CleaningSatisfaction < 0)
                {
                    CleaningSatisfaction = 0;
                }
            }
    
            if (CleaningSatisfaction >= 10)
            {
                // Stop the forced movement FIRST
                StopAllCoroutines(); // This stops FollowPath
                _isMoving = false;
                transform.position = GetSnappedPosition(transform.position);
                _targetPos = transform.position;
                _playerCanvas.SetActive(false);
                CurrentKey = "";
                _moveSpeed = 4;
                _isCoping = false;
                _contaminated = false;
                _isCopingTriggered = false;

                // Start cooldown before next coping minigame can trigger
                _onCopingCooldown = true;
                _copingCooldownTimer = 0f;
            }
        }

        if (CurrentReturns == MaxReturns)
        {
            if (_typeWriterScript.StartedTyping == false)
            {
                StartCoroutine(Fade(true, false,
                "You cleaned yourself too many times and missed your psychiatrist appointment."));
            }
        }
        
        // Handle coping cooldown timer
        if (_onCopingCooldown)
        {
            _copingCooldownTimer += Time.deltaTime;
            if (_copingCooldownTimer >= 5) // 10 second cooldown before next coping event
            {
                _onCopingCooldown = false;
                _copingCooldownTimer = 0f;
            }
        }

        // Only accept input when not already moving
        if (_isMoving || _contaminated) return;

        Vector2 inputDirection = GetInputDirection();
        MovementAnimationManager(inputDirection.x, inputDirection.y);
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
                    if (Contamination > 0)
                    {
                        WashingStart();
                        _isCleaning = true;
                        _playerCanvas.SetActive(true);
                        CleaningSatisfaction = 10;
                        GenerateNewLetter();
                        _cleaningMinigameTimerMax = Random.Range(4, 6);
                        _minigameMaxProgress = Random.Range(4, 6);
                        _minigameProgress = 0;
                        Contamination = 0;
                        _whispers.alpha = 0;
                        _contaminationSlider.value = 0;
                        _path.Clear();
                    }
                }

                if (IsFinishTile(targetCell))
                {
                    if (_typeWriterScript.StartedTyping == false)
                    {
                        if (SceneManager.GetActiveScene().name.ToLower().Contains("tutorial"))
                        {
                            StartCoroutine(Fade(true, true, "Open Closed Doors.\n \n Make it to your psychiatrist on time.", 1f, false, "Level1 Frederik"));
                        }
                        else if (SceneManager.GetActiveScene().name.Contains("1"))
                        {
                            StartCoroutine(Fade(true, true, "You leave your house and head outside.", 1f, false, "Level2"));
                        }
                        else if (SceneManager.GetActiveScene().name.Contains("2"))
                        {
                            StartCoroutine(Fade(true, true, "You reach the psychiatrists office.", 1f, false, "Level3 Frederik"));
                        }
                        else if (SceneManager.GetActiveScene().name.Contains("3"))
                        {
                            StartCoroutine(Fade(true, true, "(O)pening (C)losed (D)oors \n " +
                                                            "\n Around 2% of the world suffer from OCD." +
                                                            "\n Contamination OCD is the intense, irrational fear of feeling contaminated." +
                                                            "\n People with Contamination OCD can spend a long time performing their rituals or cleaning habits to deal with the issue." +
                                                            "\n One of the only ways to deal with OCD is exposure therapy, teaching people with OCD to reduce their rituals and learn to balance the need to clean and acceptance of feeling contaminated." +
                                                            "\n Acknowledging OCD being a real and serious issue is important.", 1f, false, "Tutorial"));
                        }
                    }
                }

                if (IsPsychTile(targetCell) && TalkedToPsychiatrist == false)
                {
                    TalkedToPsychiatrist = true;
                    _contaminated = true;
                    StartCoroutine(Fade(true, false, "You have a great session with your psychiatrist. \n (You can now resist the contamination)", 1f, true));
                }
                
            }
        }
    }

    private IEnumerator StartCopeMinigame()
    {
        _isCopingTriggered = true;
        _isCoping = true;
        _contaminated = true; // Block movement
    
        yield return new WaitForSeconds(Random.Range(2, 4));
        GenerateNewLetter();
    
        _moveSpeed *= 0.5f;
        CleaningSatisfaction = 0;
        _playerCanvas.SetActive(true);
    
        yield return StartCoroutine(FollowPath()); // Use yield return to wait for completion
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
    
    private bool IsPsychTile(Vector2 worldPosition)
    {
        float checkRadius = _grid.cellSize.x * 0.4f;
        Collider2D hit = Physics2D.OverlapCircle(worldPosition, checkRadius, _psychMask);
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

        // Footstep manager
        FootstepManager();

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
            "b", "c", "e", "f", "g", "h", "i", "j", "k", "l", "m", "n", "o", "p", "q", "r", "t", "u",
            "v", "x", "y", "z"
        };

        CurrentKey = availableLetters[Random.Range(0, availableLetters.Length - 1)];
        _keyText.text = $"Press \"{CurrentKey.ToUpper()}\"!";
    }

    private IEnumerator FollowPath()
    {
        _contaminated = true;
        _isMoving = true; // Block input during forced movement
        print("Following Path!");
    
        // Make a copy of the path to iterate through
        List<Vector2> pathCopy = new List<Vector2>(_path);
    
        for (int i = pathCopy.Count - 1; i >= 0; i--)
        {
            Vector2 targetPosition = pathCopy[i];
            Vector2 direction = (targetPosition - (Vector2)transform.position).normalized;
        
            // Set facing direction BEFORE moving
            MovementAnimationManager(direction.x, direction.y);
        
            // Wait for movement to complete
            yield return StartCoroutine(MoveToTarget(targetPosition));
        
            // Remove from original path only after successfully moving there
            if (_path.Count > 0)
            {
                _path.RemoveAt(_path.Count - 1);
            }
        }
    
        _isMoving = false;
    }

    // Better structure:
    private IEnumerator Fade(bool fadeOut, bool win, string text = "", float duration = 1f, bool psychiatristSquare = false, string sceneName = "")
    {
        if (fadeOut)
        {
            yield return StartCoroutine(FadeInCoroutine(duration));
            if (_typeWriterScript.StartedTyping == false)
            {
                yield return StartCoroutine(_typeWriterScript.StartTyping(text));
            }
        }
        else
        {
            yield return StartCoroutine(FadeOutCoroutine(duration));
        }

        // Handle scene transition AFTER all fades and typing
        if (_typeWriterScript.DoneTyping)
        {
            if (win)
                SceneManager.LoadScene(sceneName);
            else if (psychiatristSquare)
            {
                StartCoroutine(FadeOutCoroutine(duration));
            }
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
        if (_contaminated)
        {
            _contaminated = false;
        }

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
        _contaminationSlider.value = Contamination;
        _whispers.alpha = Contamination * 0.01f;
        overlay.SetContamination(Contamination * 0.01f);
    }

    // Audio Managers

    private void WashingStart()
    {
        if (SceneManager.GetActiveScene().name.Contains("2"))
        {
            AudioManager.Instance.Ambience(SoundType.Pond_Water);
        }
        else
        {
            AudioManager.Instance.Play(SoundType.Sink_Water_Start);
            Invoke(nameof(StartLoopSounds), 0.85f);
        }
    }
    private void StartLoopSounds()
    {
        AudioManager.Instance.Ambience(SoundType.Sink_Water_Loop);
        AudioManager.Instance.Ambience(SoundType.Pond_Water);
    }

    private void WashingStop()
    {
        if (SceneManager.GetActiveScene().name.Contains("2"))
        {
            AudioManager.Instance.StopAmbience(SoundType.Pond_Water);
            StopWhispers();
        }
        else
        {
            AudioManager.Instance.StopAmbience(SoundType.Sink_Water_Loop);
            AudioManager.Instance.StopAmbience(SoundType.Pond_Water);
            AudioManager.Instance.Play(SoundType.Sink_Water_Stop);
            StopWhispers();
        }
    }

    private void StopWhispers()
    {
        AudioManager.Instance.StopAmbience(SoundType.Whispers);
    }

    private void AmbianceManager()
    {
        if (AudioManager.Instance == null)
        {
            return;
        }
        if (SceneManager.GetActiveScene().name.Contains("1"))
        {
            AudioManager.Instance.Ambience(SoundType.Light_Ambience);
        }
        else if (SceneManager.GetActiveScene().name.Contains("2"))
        {
            AudioManager.Instance.Ambience(SoundType.Birds_Ambience);
        }
        else if (SceneManager.GetActiveScene().name.Contains("3"))
        {
            AudioManager.Instance.Ambience(SoundType.Fan_Ambience);
        }
    }
    private void MovementAnimationManager(float x, float y)
    {

        bool wantsToMove = x != 0 || y != 0;

        if (_isCleaning)
        {
            animator.SetBool("Interacting", true);
            animator.SetBool("Moving", false);
        }
        else if (_isMoving || wantsToMove)
        {
            animator.SetBool("Interacting", false);
            animator.SetBool("Moving", true);
        }
        else
        {
            animator.SetBool("Interacting", false);
            animator.SetBool("Moving", false);
        }

        if(_isCleaning) return;

        if (x > 0)      { animator.SetInteger("Facing", 2); sr.flipX = false; }
        else if (x < 0) { animator.SetInteger("Facing", 2); sr.flipX = true; }
        else if (y > 0) { animator.SetInteger("Facing", 1); sr.flipX = false; }
        else if (y < 0) { animator.SetInteger("Facing", 0); sr.flipX = false; }
    }

    private void FootstepManager()
    {
        if (SceneManager.GetActiveScene().name.Contains("2"))
        {
            if (_stepAlternate)
            {
                AudioManager.Instance.Play(SoundType.Step_Grass_1);
                _stepAlternate = !_stepAlternate;
            }
            else
            {
                AudioManager.Instance.Play(SoundType.Step_Grass_2);
                _stepAlternate = !_stepAlternate;
            }
        }
        else
        {
            if (_stepAlternate)
            {
                AudioManager.Instance.Play(SoundType.Step_Stone_1);
                _stepAlternate = !_stepAlternate;
            }
            else
            {
                AudioManager.Instance.Play(SoundType.Step_Stone_2);
                _stepAlternate = !_stepAlternate;
            }
        }
    }
}

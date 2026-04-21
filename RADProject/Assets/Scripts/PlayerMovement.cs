using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.Tilemaps;

public class PlayerMovement : MonoBehaviour
{
    [Header("Movement Settings")] 
    [SerializeField] private float _moveSpeed = 5f;
    [SerializeField] private LayerMask _obstacleMask;
    [SerializeField] private LayerMask _doorMask;
    [SerializeField] private LayerMask _washingMask;

    [Header("Player Stats")] 
    public int Contamination;

    [Header("Path Taken")] 
    [SerializeField] private List<Vector2> _path;

    [Header("References")] 
    [SerializeField] private Grid _grid;
    private Vector2 _targetPos;
    private bool _isMoving;
    private bool _contaminated;
    private bool _onDoor;
    private DoorOpenerManager _currentDoorManager;
    
    // Start is called once before the first execution of Update after the MonoBehaviour is created
    void Awake()
    {
        // If no grid is assigned, try to find one in the scene
        if (_grid == null)
        {
            _grid = FindFirstObjectByType<Grid>();
        }
        
        // Snap the player to the nearest grid cell at start
        transform.position = GetSnappedPosition(transform.position);
        _targetPos = transform.position;
    }

    // Update is called once per frame
    void Update()
    {
        if (Contamination == 100)
        {
            Contamination = 0;
            _moveSpeed *= 2;
            
            StartCoroutine(FollowPath());
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
                    Contamination = 0;
                    _path.Clear();
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
            string currentSceneName = SceneManager.GetActiveScene().name; 
            SceneManager.LoadScene(currentSceneName);
        }
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

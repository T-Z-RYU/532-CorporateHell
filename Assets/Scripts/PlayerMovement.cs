using UnityEngine;

[RequireComponent(typeof(Rigidbody2D))]
public class PlayerMovement : MonoBehaviour
{
    [Header("Movement Settings")]
    [SerializeField] private float moveSpeed = 5f;
    
    [Header("Dash Settings")]
    [SerializeField] private float dashSpeed = 15f;
    [SerializeField] private float dashDuration = 0.2f;
    [SerializeField] private float dashCooldown = 1f;
    [SerializeField] private bool dashHasInvincibility = true;
    
    [Header("Screen Boundaries")]
    [SerializeField] private float screenMargin = 1f;
    [SerializeField] private bool constrainToScreen = true;
    
    [Header("Input Settings")]
    [SerializeField] private bool useWASD = true;
    [SerializeField] private bool useArrowKeys = true;
    
    // Components
    private Rigidbody2D rb;
    private SpriteRenderer spriteRenderer;
    
    // Input
    private Vector2 inputVector;
    private bool dashInput;
    
    // Movement state
    private bool isDashing = false;
    private float dashTimer = 0f;
    private float dashCooldownTimer = 0f;
    
    // Properties for external access
    public bool IsDashing => isDashing;
    public bool CanDash => dashCooldownTimer <= 0f && !isDashing;
    public Vector2 Velocity => rb.linearVelocity;
    public float MoveSpeed => moveSpeed;
    
    void Awake()
    {
        // Get required components
        rb = GetComponent<Rigidbody2D>();
        spriteRenderer = GetComponent<SpriteRenderer>();
        
        // Configure rigidbody for smooth top-down movement
        rb.gravityScale = 0f;
        rb.linearDamping = 0f;
        rb.freezeRotation = true; // Prevent rotation jitter
        rb.collisionDetectionMode = CollisionDetectionMode2D.Continuous; // Better collision detection
    }
    
    void Update()
    {
        HandleInput();
        UpdateTimers();
    }
    
    void FixedUpdate()
    {
        if (isDashing)
        {
            HandleDashMovement();
        }
        else
        {
            HandleNormalMovement();
        }
    }
    
    void LateUpdate()
    {
        if (constrainToScreen)
        {
            ConstrainToScreen();
        }
    }
    
    private void HandleInput()
    {
        // Reset input vector
        inputVector = Vector2.zero;
        
        // Get horizontal input
        if (useWASD)
        {
            if (Input.GetKey(KeyCode.A)) inputVector.x -= 1f;
            if (Input.GetKey(KeyCode.D)) inputVector.x += 1f;
            if (Input.GetKey(KeyCode.W)) inputVector.y += 1f;
            if (Input.GetKey(KeyCode.S)) inputVector.y -= 1f;
        }
        
        if (useArrowKeys)
        {
            if (Input.GetKey(KeyCode.LeftArrow)) inputVector.x -= 1f;
            if (Input.GetKey(KeyCode.RightArrow)) inputVector.x += 1f;
            if (Input.GetKey(KeyCode.UpArrow)) inputVector.y += 1f;
            if (Input.GetKey(KeyCode.DownArrow)) inputVector.y -= 1f;
        }
        
        // Normalize diagonal movement
        if (inputVector.magnitude > 1f)
        {
            inputVector.Normalize();
        }
    }
    
    private void UpdateTimers()
    {
        // Update dash timer
        if (isDashing)
        {
            dashTimer -= Time.deltaTime;
            if (dashTimer <= 0f)
            {
                EndDash();
            }
        }
        
        // Update dash cooldown timer
        if (dashCooldownTimer > 0f)
        {
            dashCooldownTimer -= Time.deltaTime;
        }
    }
    
    private void HandleNormalMovement()
    {
        // Smooth instant movement - no acceleration/deceleration for arcade feel
        Vector2 targetVelocity = inputVector * moveSpeed;
        rb.linearVelocity = targetVelocity;
    }
    
    private void HandleDashMovement()
    {
        // Maintain dash direction and speed
        rb.linearVelocity = inputVector.normalized * dashSpeed;
    }
    
    private void StartDash()
    {
        isDashing = true;
        dashTimer = dashDuration;
        dashCooldownTimer = dashCooldown;
        
        // Set dash direction based on current input or last movement direction
        if (inputVector.magnitude < 0.1f)
        {
            // If no input, dash in last movement direction
            inputVector = rb.linearVelocity.normalized;
        }
        
        // Trigger dash effects (can be extended for visual/audio feedback)
        OnDashStart();
    }
    
    private void EndDash()
    {
        isDashing = false;
        dashTimer = 0f;
        
        // Stop movement briefly for dash end effect
        rb.linearVelocity = Vector2.zero;
        
        // Trigger dash end effects
        OnDashEnd();
    }
    
    private void ConstrainToScreen()
    {
        Vector3 pos = transform.position;
        Vector2 screenBounds = GetScreenBounds();
        
        // Clamp position to screen bounds with margin
        pos.x = Mathf.Clamp(pos.x, -screenBounds.x + screenMargin, screenBounds.x - screenMargin);
        pos.y = Mathf.Clamp(pos.y, -screenBounds.y + screenMargin, screenBounds.y - screenMargin);
        
        transform.position = pos;
    }
    
    private Vector2 GetScreenBounds()
    {
        // Get screen bounds in world coordinates
        Camera cam = Camera.main;
        if (cam == null) return Vector2.one * 10f; // Fallback bounds
        
        float height = cam.orthographicSize * 2f;
        float width = height * cam.aspect;
        
        return new Vector2(width * 0.5f, height * 0.5f);
    }
    
    // Virtual methods for extending functionality
    protected virtual void OnDashStart()
    {
        // Override in derived classes for custom dash start effects
        // Example: Play dash sound, create dash particles, etc.
    }
    
    protected virtual void OnDashEnd()
    {
        // Override in derived classes for custom dash end effects
        // Example: Stop dash sound, create dash end particles, etc.
    }
    
    // Public methods for external systems
    public void SetMoveSpeed(float newSpeed)
    {
        moveSpeed = newSpeed;
    }
    
    public void SetDashCooldown(float newCooldown)
    {
        dashCooldown = newCooldown;
    }
    
    public void ForceStop()
    {
        rb.linearVelocity = Vector2.zero;
        isDashing = false;
        dashTimer = 0f;
    }
    
    public void ResetDashCooldown()
    {
        dashCooldownTimer = 0f;
    }
    
    // Debug visualization
    void OnDrawGizmosSelected()
    {
        if (constrainToScreen)
        {
            Gizmos.color = Color.yellow;
            Vector2 bounds = GetScreenBounds();
            Gizmos.DrawWireCube(Vector3.zero, new Vector3(bounds.x * 2, bounds.y * 2, 0));
            
            Gizmos.color = Color.red;
            Gizmos.DrawWireCube(Vector3.zero, new Vector3(
                (bounds.x - screenMargin) * 2, 
                (bounds.y - screenMargin) * 2, 
                0));
        }
    }
}

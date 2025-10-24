using UnityEngine;

public class PlayerScript : MonoBehaviour
{
	[Header( "Movement Settings" )]
	[SerializeField] private float moveSpeed = 5f;
	public bool canMove;

	[Header( "Input Settings" )]
	[SerializeField] private bool useWASD = true;
	[SerializeField] private bool useArrowKeys = true;

	private Vector2 inputVector;

	[HideInInspector] public GameObject currInteractObject;

	// Components
	private Rigidbody2D rb;
	private SpriteRenderer spriteRenderer;
	// Start is called once before the first execution of Update after the MonoBehaviour is created

	void Awake( )
	{
		canMove = true;
		// Get required components
		rb = GetComponent<Rigidbody2D>();
		spriteRenderer = GetComponent<SpriteRenderer>();

		// Configure rigidbody for smooth top-down movement
		rb.gravityScale = 0f;
		rb.linearDamping = 0f;
		rb.freezeRotation = true; // Prevent rotation jitter
		rb.collisionDetectionMode = CollisionDetectionMode2D.Continuous; // Better collision detection
	}

	void Start()
    {
        
    }

    // Update is called once per frame
    void Update()
    {
		if(canMove)
		{
			HandleInput();
		}
    }


	private void HandleInput( )
	{
		// Reset input vector
		inputVector = Vector2.zero;

		// Get horizontal input
		if(useWASD)
		{
			if(Input.GetKey( KeyCode.A )) inputVector.x -= 1f;
			if(Input.GetKey( KeyCode.D )) inputVector.x += 1f;
			if(Input.GetKey( KeyCode.W )) inputVector.y += 1f;
			if(Input.GetKey( KeyCode.S )) inputVector.y -= 1f;
		}

		if(useArrowKeys)
		{
			if(Input.GetKey( KeyCode.LeftArrow )) inputVector.x -= 1f;
			if(Input.GetKey( KeyCode.RightArrow )) inputVector.x += 1f;
			if(Input.GetKey( KeyCode.UpArrow )) inputVector.y += 1f;
			if(Input.GetKey( KeyCode.DownArrow )) inputVector.y -= 1f;
		}

		// Normalize diagonal movement
		if(inputVector.magnitude > 1f)
		{
			inputVector.Normalize();
		}

		// Smooth instant movement - no acceleration/deceleration for arcade feel
		Vector2 targetVelocity = inputVector * moveSpeed;
		rb.linearVelocity = targetVelocity;

		if(Input.GetKeyDown( KeyCode.E ))
		{
			Interact();
		}
	}

	public void Interact( )
	{
		if(currInteractObject != null)
		{
			currInteractObject.GetComponent<CanInteract>().InteractWithUI();
			FreezePlayer();
		}
	}

	public void FreezePlayer( )
	{
		canMove = false;
	}

	public void UnFreezePlayer( )
	{
		canMove = true;
	}
}

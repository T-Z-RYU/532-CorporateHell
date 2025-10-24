using UnityEngine;
using UnityEngine.UIElements;

public class PhonePressMinigamePressScript : MonoBehaviour
{

    [HideInInspector]public float speed;

	[HideInInspector] public bool beginLower;

	public Transform endPosition;
    private Vector3 startPosition;
    private PhonePressMinigame PPMinigame;
    

    public enum PressState
    {
        Ready,
        Lowering,
        Rising
    }

    public PressState currState;
	// Start is called once before the first execution of Update after the MonoBehaviour is created

	void Awake( )
	{
        beginLower = false;
        startPosition = gameObject.transform.position;
		PPMinigame = GameObject.Find( "PhonePressGame" ).GetComponent<PhonePressMinigame>();
        speed = PPMinigame.pressSpeed;
	}
	void Start()
    {

	}

    // Update is called once per frame
    void Update()
    {
		switch(currState)
		{
			case PressState.Ready:
                if(beginLower)
                    currState = PressState.Lowering;
				break;

			case PressState.Lowering:
				Lower();
				break;

			case PressState.Rising:
				Rising();
				break;

		}
	}

    public void Lower( )
    {
        var step = speed * Time.deltaTime;
        transform.position = Vector3.MoveTowards( transform.position, endPosition.position, step );

        if(Vector3.Distance( transform.position, endPosition.position ) < 0.001f)
        {
            currState = PressState.Rising;
        }

    }

    public void Rising( )
    {
        var step = speed * Time.deltaTime;
        transform.position = Vector3.MoveTowards( transform.position, startPosition, step );

		if(Vector3.Distance( transform.position, startPosition ) < 0.001f)
		{
            beginLower = false;
			currState = PressState.Ready;
		}
	}

	private void OnTriggerEnter2D(Collider2D col)
	{
		if(col.tag == "Phone Prefab")
        {
            col.GetComponent<PhoneScript>().ChangeState();
        }
	}
}

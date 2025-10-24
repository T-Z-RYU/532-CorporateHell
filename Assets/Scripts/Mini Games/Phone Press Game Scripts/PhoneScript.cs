using Unity.VisualScripting;
using UnityEngine;
using UnityEngine.UI;

public class PhoneScript : MonoBehaviour
{
    private float phoneSpeed;

    [HideInInspector] public Transform EndTransform;
    [HideInInspector] public bool halt;


    public enum PhoneState
    {
        Components,
        NewPhone,
    }

    public PhoneState state;

    private PhonePressMinigame PPM;

    [SerializeField] private Color newPhoneColor;

	// Start is called once before the first execution of Update after the MonoBehaviour is created

	void Awake( )
	{
        halt = false;
		EndTransform = GameObject.Find( "PhoneEnd" ).GetComponent<Transform>();
		PPM = GameObject.Find( "PhonePressGame" ).GetComponent<PhonePressMinigame>();
		phoneSpeed = PPM.phoneSpeed;


	}
	void Start()
    {
    }

    // Update is called once per frame
    void Update( )
    {
        if(!halt)
        {
			Move();
		}
    }

    public void Move( )
    {
		var step = phoneSpeed * Time.deltaTime;
        transform.position = Vector3.MoveTowards( transform.position, EndTransform.position, step );
        if(Vector3.Distance(transform.position, EndTransform.position) < 000.1f)
        {
            ReachedEnd();
        }
    }

    public void ChangeState( )
    {
        state = PhoneState.NewPhone;
        gameObject.GetComponent<Image>().color = newPhoneColor;

    }

    public void ReachedEnd( )
    {
        if(state == PhoneState.NewPhone)
        {
            PPM.finishedPhones++;
            Destroy(gameObject);
        } 
        
        else
        {
            PPM.MissedPress();
            Destroy(gameObject);
        }
		PPM.SubtractFromList( gameObject );

	}

}

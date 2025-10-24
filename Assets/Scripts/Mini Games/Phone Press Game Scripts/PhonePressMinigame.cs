using Unity.VisualScripting;
using UnityEngine;
using System.Collections.Generic;

public class PhonePressMinigame : MonoBehaviour
{
    [Header("Game Settings")]
    public float pressSpeed;
    public float phoneSpeed;
    public int maxPhones;
    public float phoneSpawnRate;
    public float missedPressDelay;
    public int componentToPhoneAmount;
    public int componentAmount;

    [Header( "AudioClips" )]
    public AudioClip machineNoise;

    public GameObject TemplateObject;
    public GameObject PhoneSpawnLocation;
	public List<GameObject> ConveyorElements = new List<GameObject>();

	public int finishedPhones;

    [SerializeField]private GameObject PressObject;

    private float currPhoneSpawnRate;
    private int currPhoneAmount;
    private GameObject spawnParent;
    private bool halt;
    private float currPressDelay;

    private AudioSource audioSource;

	// Start is called once before the first execution of Update after the MonoBehaviour is created

	private void Awake( )
	{
        halt = false;
        currPressDelay = missedPressDelay;
        currPhoneSpawnRate = phoneSpawnRate;
        currPhoneAmount = 0;
	}
	void Start()
    {
		spawnParent = GameObject.Find( "Conveyor Belt" );
        audioSource = GetComponent<AudioSource>();
		audioSource.clip = machineNoise;
		audioSource.Play();
	}

	// Update is called once per frame
	void Update()
    {
        if(!halt)
        {
            SpawnPhones();
        } else
        {
            HaltCountDown();
        }
    }

    public void ButtonClicked( )
    {
        PressObject.GetComponent<PhonePressMinigamePressScript>().beginLower = true;

        
    }

    public void MissedPress( )
    {
        halt = true;
        for(int i = 0; ConveyorElements.Count > i; i++)
        {
            ConveyorElements[i].GetComponent<PhoneScript>().halt = true;
        }
    }

    public void HaltCountDown( )
    {
        if(currPressDelay !> 0)
        {
            currPressDelay -= Time.deltaTime;
        } else
        {
            currPressDelay = missedPressDelay;
			for(int i = 0; ConveyorElements.Count > i; i++)
			{
				ConveyorElements[i].GetComponent<PhoneScript>().halt = false;
			}
            halt = false;
		}
    }

    public void SpawnPhones( )
    {
        if(currPhoneSpawnRate <= 0 && currPhoneAmount < maxPhones && componentAmount >= componentToPhoneAmount)
        {
            componentAmount -= componentToPhoneAmount;
            RectTransform spawnRectTrans = PhoneSpawnLocation.GetComponent<RectTransform>();
            Vector3 spawnLocation = new Vector3(spawnRectTrans.localPosition.x,spawnRectTrans.localPosition.y,spawnRectTrans.localPosition.z);
            GameObject newPhone = Instantiate( TemplateObject, spawnLocation, Quaternion.identity );
            newPhone.transform.SetParent( spawnParent.transform, false );
            ConveyorElements.Add( newPhone );
            currPhoneSpawnRate = phoneSpawnRate;
            currPhoneAmount++;
        } else
        {
            currPhoneSpawnRate -= Time.deltaTime;
        }
    }

    public void SubtractFromList(GameObject phone)
    {
		for(int i = 0; ConveyorElements.Count > i; i++)
		{
            if(ConveyorElements[i] == phone)
            {
                ConveyorElements.RemoveAt( i );
                currPhoneAmount--;
            }
		}
	}
	public void ExitUI( )
	{
		gameObject.SetActive( false );
		GameObject.Find( "Player" ).GetComponent<PlayerScript>().UnFreezePlayer();
	}
}

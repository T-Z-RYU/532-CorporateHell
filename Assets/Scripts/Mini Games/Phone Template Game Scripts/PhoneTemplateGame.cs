using UnityEngine;
using System.Collections.Generic;
using UnityEngine.UI;
using TMPro;

public class PhoneTemplateGame : MonoBehaviour
{
    public List<GameObject> sliderGameobjects = new List<GameObject>();
    public List<SliderChangeObject> BodyPartChangeList = new List<SliderChangeObject>();

    public TMP_Text postCaption;

    public AudioClip postSound;
    public GameObject NoResourceObject;
    public int accounts;

    private List<float> OriginalSliderVal = new List<float>();
    private AudioEffectsScript audioEffectsScript;
    private GameManager gameManager;
    // Start is called once before the first execution of Update after the MonoBehaviour is created
    void Start()
    {
        for(int i = 0; i < sliderGameobjects.Count; i++)
        {
            sliderGameobjects[i].GetComponent<PhoneTemplateSlider>().templateListIndex = i;
            OriginalSliderVal.Add( sliderGameobjects[i].GetComponent<PhoneTemplateSlider>().startSliderValue );
        }
        

        //Get Reference
		audioEffectsScript = GameObject.FindWithTag( "Audio Effect Object" ).GetComponent<AudioEffectsScript>();
        gameManager = GameObject.FindGameObjectWithTag("Game Manager").GetComponent<GameManager>();
    }

	// Update is called once per frame
	void Update()
    {
		accounts = gameManager.accounts;

		if(accounts > 0)
        {
            NoResourceObject.SetActive( false );
        } else
        {
            NoResourceObject.SetActive ( true );
        }
	}

	public void UpdateStringValue( float value, string name) {
        for(int i = 0; sliderGameobjects.Count > i; i++)
        {
            if(sliderGameobjects[i].name == name)
            {
                if(!BodyPartChangeList[i].fullScale)
                {
                    Vector3 oldScale = BodyPartChangeList[i].BodyGameObject.GetComponent<RectTransform>().localScale;
                    Vector3 newScale = new Vector3(value,oldScale.y, oldScale.z );
                    BodyPartChangeList[i].BodyGameObject.GetComponent<RectTransform>().localScale = newScale;
					break;
                } else
                {
					Vector3 oldScale = BodyPartChangeList[i].BodyGameObject.GetComponent<RectTransform>().localScale;
					Vector3 newScale = new Vector3( value, value, value );
					BodyPartChangeList[i].BodyGameObject.GetComponent<RectTransform>().localScale = newScale;
				}
				break;
            }
        }
	}

    public void ExitUI( )
    {
        gameObject.SetActive(false);
        //Clear Values of Sliders
        for(int i = 0; i < sliderGameobjects.Count; i++)
        {
            sliderGameobjects[i].GetComponent<Slider>().value = OriginalSliderVal[i];
            postCaption.text = "";
        }
    }

    public void PlayPostSound( )
    {
        audioEffectsScript.GetComponent<AudioSource>().clip = postSound;
        audioEffectsScript.GetComponent<AudioSource>().Play();
    }
}

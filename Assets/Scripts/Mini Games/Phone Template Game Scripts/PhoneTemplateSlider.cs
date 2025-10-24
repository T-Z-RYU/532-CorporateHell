using UnityEngine;
using UnityEngine.UI;
using TMPro;
public class PhoneTemplateSlider : MonoBehaviour
{
    [HideInInspector]
    public float startSliderValue;

    public int templateListIndex;

    private PhoneTemplateGame templateScript;
	// Start is called once before the first execution of Update after the MonoBehaviour is created
	private void Awake( )
	{
		startSliderValue = GetComponent<Slider>().value;
	}

	void Start()
    {
        templateScript = GameObject.Find("PhotoShopGame").GetComponent<PhoneTemplateGame>();
    }

    // Update is called once per frame
    void Update()
    {
        
    }

    public void UpdateSliderValue(float value )
    {
        templateScript.UpdateStringValue(value, gameObject.name );

    }
}

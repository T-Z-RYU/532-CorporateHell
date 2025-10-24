using UnityEngine;
using UnityEngine.UI;

public class CanInteract : MonoBehaviour
{

    public GameObject InteractUI;
	public Color highlightColor;

	private Color startColor;

	private SpriteRenderer thisSR;
    // Start is called once before the first execution of Update after the MonoBehaviour is created
    void Start()
    {
		thisSR = GetComponent<SpriteRenderer>();
		startColor = thisSR.color;
    }

    // Update is called once per frame
    void Update()
    {
        
    }

    public void InteractWithUI( )
    {
        InteractUI.SetActive( true );
    }

	private void OnMouseOver( )
	{
		thisSR.color = highlightColor;
		Debug.Log( "Set Interact Object" );
	}

	private void OnMouseExit( )
	{
		thisSR.color = startColor;
		Debug.Log( "Cleared Interact Object" );
	}

	private void OnMouseDown( )
	{
		InteractWithUI( );
	}
}

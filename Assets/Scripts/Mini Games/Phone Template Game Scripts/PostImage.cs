using System.Collections;
using Unity.VisualScripting;
using UnityEngine;

public class PostImage : MonoBehaviour
{

    public GameObject ImageFrame;

    private GameManager gameManager;
    private PhoneTemplateGame phoneGameScript;
    private AudioEffectsScript audioEffectsScript;
    // Start is called once before the first execution of Update after the MonoBehaviour is created
    void Start()
    {
        gameManager = GameObject.FindGameObjectWithTag("Game Manager").GetComponent<GameManager>();
        phoneGameScript = GameObject.Find( "PhotoShopGame" ).GetComponent<PhoneTemplateGame>();
    }

    // Update is called once per frame
    void Update()
    {
        
    }

    public void SaveAndPost( )
    {
        StartCoroutine( CoroutineScreenshot() );
    }

    private IEnumerator CoroutineScreenshot( )
    {
        yield return new WaitForEndOfFrame();
        RectTransform rectTransform = ImageFrame.GetComponent<RectTransform>();
        Canvas canvas = ImageFrame.GetComponentInParent<Canvas>();
        Rect pixelRect = RectTransformUtility.PixelAdjustRect( ImageFrame.GetComponent<RectTransform>(), canvas );

        Vector3[] corners = new Vector3[4];
        rectTransform.GetWorldCorners(corners);

        Vector3 bottomLeftCorner = corners[0];

        Rect rect = new Rect(bottomLeftCorner.x,bottomLeftCorner.y, rectTransform.rect.width, rectTransform.rect.height );
		Texture2D screenShotTexture = new Texture2D( ((int)pixelRect.width), ((int)pixelRect.height), TextureFormat.ARGB32, false );
        screenShotTexture.ReadPixels(rect,0,0);
        screenShotTexture.Apply();

        gameManager.AddPost( screenShotTexture,phoneGameScript.postCaption.text );
        gameManager.accounts--;
        gameManager.AccountUpdate();

        //byte[] byteArray =  screenShotTexture.EncodeToPNG();
        //System.IO.File.WriteAllBytes(Application.dataPath + "/Screenshots/PostScreenshot.png",byteArray);

        //Object.Destroy( screenShotTexture );
        phoneGameScript.PlayPostSound();
        phoneGameScript.ExitUI();

		Debug.Log("Saved Screenshot");

	}
}

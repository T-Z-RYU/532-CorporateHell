using NUnit.Framework;
using UnityEngine;
using System.Collections.Generic;
using TMPro;
public class GameManager : MonoBehaviour
{
    [Header("Resources")]
    public int accounts;
    public int gold;
	public List<PostObjects> PostList = new List<PostObjects>();

	[Header("Audio")]
    public AudioClip backgroundMusic;
    public string Testing;
    [Header("UI Text")]
    public TMP_Text goldAmountText;
    public TMP_Text accountsAmountText;


    // Start is called once before the first execution of Update after the MonoBehaviour is created
    void Start()
    {
        GoldUpdate();
        AccountUpdate();
    }

    // Update is called once per frame
    void Update()
    {
        
    }

    public void AddPost(Texture2D postImage, string postCaption)
    {
        PostObjects postObject = new PostObjects();
        postObject.PostImage = postImage;
        postObject.PostCaption = postCaption;
        PostList.Add( postObject );
	}

    public void GoldUpdate( )
    {
        goldAmountText.text = ("Gold: " + gold.ToString());
    }

    public void AccountUpdate( )
    {
        accountsAmountText.text = ("Accounts: " +  accounts.ToString());
    }

    //[YarnCommand("BringUpGame")]
    public void BringUpGame( )
    {
        Debug.Log( "Test" );
    }
}

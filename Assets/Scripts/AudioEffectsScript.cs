using UnityEngine;

public class AudioEffectsScript : MonoBehaviour
{

    private AudioSource audioSource;
    // Start is called once before the first execution of Update after the MonoBehaviour is created
    void Start()
    {
        
    }

    // Update is called once per frame
    void Update()
    {
        
    }

    public void PlayClip(AudioClip clip)
    {
        audioSource.clip = clip;
        audioSource.Play();
    }
}

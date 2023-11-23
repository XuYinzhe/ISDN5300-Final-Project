using UnityEngine;

public class MusicSpectrum : MonoBehaviour
{
    public AudioClip musicClip;
    private AudioSource audioSource;
    public GameObject sphereObject;
    private float sphereScale = 1f;

    private void Start()
    {
        // Create an Audio Source component
        audioSource = gameObject.AddComponent<AudioSource>();

        // Set the clip of the AudioSource to the musicClip
        audioSource.clip = musicClip;

        // Play the music
        audioSource.Play();
        sphereObject = GameObject.CreatePrimitive(PrimitiveType.Sphere);
        sphereObject.transform.position = Vector3.zero;
    }

    private void Update()
    {
        // Get the spectrum data of the music
        float[] spectrumData = new float[512];
        audioSource.GetSpectrumData(spectrumData, 0, FFTWindow.BlackmanHarris);
        sphereScale = Mathf.Lerp(sphereScale, Mathf.Max(spectrumData) * 10f, Time.deltaTime * 5f);
        sphereObject.transform.localScale = new Vector3(sphereScale, sphereScale, sphereScale);
    
        // for (int i = 0; i < spectrumData.Length; i++)
        // {
        //     Debug.Log("Spectrum Data at index " + i + ": " + spectrumData[i]);
        // }
    }
}
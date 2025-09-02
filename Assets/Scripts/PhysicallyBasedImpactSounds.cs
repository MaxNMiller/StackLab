using UnityEngine;
using System.Collections;

[RequireComponent(typeof(AudioSource), typeof(Rigidbody))]
public class PhysicallyBasedImpactSounds : MonoBehaviour
{
    [Header("Impact Settings")]
    public AudioClip[] impactSounds;
    public float minImpactForce = 2f;
    public float maxImpactForce = 25f;
    public float volumeMultiplier = 1f;

    [Header("Pitch Variation")]
    public float minPitch = 0.85f;
    public float maxPitch = 1.15f;

    [Header("Collision Cooldown")]
    public float minCollisionCooldown = 0.1f;

    [Header("Debug")]
    public bool enableDebugging = true;

    private AudioSource audioSource;
    private Rigidbody rb;
    private float lastCollisionTime;

    void Start()
    {
        audioSource = GetComponent<AudioSource>();
        rb = GetComponent<Rigidbody>();

        // Check if components are properly assigned
        if (audioSource == null)
        {
            Debug.LogError("AudioSource component missing on " + gameObject.name, this);
            return;
        }

        if (rb == null)
        {
            Debug.LogError("Rigidbody component missing on " + gameObject.name, this);
            return;
        }

        // Configure audio source
        audioSource.spatialBlend = 1f; // 3D sound
        audioSource.playOnAwake = false;
        audioSource.maxDistance = 20f;
        audioSource.rolloffMode = AudioRolloffMode.Logarithmic;

        // Check if audio clips are assigned
        if (impactSounds == null || impactSounds.Length == 0)
        {
            Debug.LogWarning("No impact sounds assigned to " + gameObject.name, this);
        }
        else
        {
            // Check for null clips in the array
            for (int i = 0; i < impactSounds.Length; i++)
            {
                if (impactSounds[i] == null)
                {
                    Debug.LogWarning("Impact sound at index " + i + " is null on " + gameObject.name, this);
                }
            }
        }
    }

    void OnCollisionEnter(Collision collision)
    {
        // Check if we have sounds to play
        if (impactSounds == null || impactSounds.Length == 0)
            return;

        // Check if audio source is available
        if (audioSource == null)
            return;

        // Calculate impact strength
        float impactStrength = collision.relativeVelocity.magnitude;

        if (enableDebugging)
        {
            Debug.Log("Collision detected with force: " + impactStrength +
                     " (Min required: " + minImpactForce + ")", this);
        }

        // Check if impact is strong enough and cooldown has passed
        if (impactStrength > minImpactForce &&
            Time.time > lastCollisionTime + minCollisionCooldown)
        {
            // Calculate volume based on impact strength
            float volume = Mathf.InverseLerp(minImpactForce, maxImpactForce, impactStrength);
            volume = Mathf.Clamp01(volume) * volumeMultiplier;

            // Randomize pitch for variety
            float pitch = Random.Range(minPitch, maxPitch);

            // Select a random sound (with additional safety check)
            int randomIndex = Random.Range(0, impactSounds.Length);
            AudioClip selectedClip = impactSounds[randomIndex];

            // Make sure the selected clip isn't null
            if (selectedClip != null && volume > 0)
            {
                if (enableDebugging)
                {
                    Debug.Log("Playing sound: " + selectedClip.name +
                             " at volume: " + volume + " and pitch: " + pitch, this);
                }

                audioSource.clip = selectedClip;
                audioSource.volume = volume;
                audioSource.pitch = pitch;
                audioSource.Play();

                lastCollisionTime = Time.time;
            }
            else if (enableDebugging)
            {
                if (selectedClip == null)
                {
                    Debug.LogWarning("Selected clip is null", this);
                }
                else
                {
                    Debug.Log("Volume too low: " + volume, this);
                }
            }
        }
        else if (enableDebugging)
        {
            if (impactStrength <= minImpactForce)
            {
                Debug.Log("Impact force too low: " + impactStrength + " <= " + minImpactForce, this);
            }
            else
            {
                Debug.Log("Cooldown active: " + (Time.time - lastCollisionTime) + " < " + minCollisionCooldown, this);
            }
        }
    }

    // Test method to manually trigger a sound
    public void TestSound()
    {
        if (impactSounds != null && impactSounds.Length > 0 && audioSource != null)
        {
            AudioClip testClip = impactSounds[0];
            if (testClip != null)
            {
                audioSource.clip = testClip;
                audioSource.volume = 0.5f;
                audioSource.pitch = 1f;
                audioSource.Play();
                Debug.Log("Test sound played: " + testClip.name, this);
            }
        }
        else
        {
            Debug.LogWarning("Cannot play test sound - no audio clips available", this);
        }
    }
}
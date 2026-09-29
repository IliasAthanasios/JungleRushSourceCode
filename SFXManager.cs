using UnityEngine;
using UnityEngine.Audio;

public class SFXManager : MonoBehaviour
{
    private static AudioMixerGroup sfxGroup;

    public static void PlaySFXAtPoint(AudioClip clip, Vector3 position, float volume = 1.0f)
    {
        if (clip == null) return;

        if (sfxGroup == null)
        {
            AudioMixer mixer = Resources.Load<AudioMixer>("MainMixer");
            if (mixer == null)
            {
                AudioMixer[] mixers = Resources.FindObjectsOfTypeAll<AudioMixer>();
                if (mixers.Length > 0) mixer = mixers[0];
            }

            if (mixer != null)
            {
                AudioMixerGroup[] groups = mixer.FindMatchingGroups("SFX");
                if (groups.Length > 0) sfxGroup = groups[0];
            }
        }

        GameObject tempGO = new GameObject("TempSFX");
        tempGO.transform.position = position;

        AudioSource audioSource = tempGO.AddComponent<AudioSource>();
        audioSource.clip = clip;
        audioSource.volume = volume;
        audioSource.spatialBlend = 1.0f; // 3D sound
        audioSource.outputAudioMixerGroup = sfxGroup;
        audioSource.Play();

        Object.Destroy(tempGO, clip.length);
    }
}

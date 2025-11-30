using UnityEngine;

public static class WorldAudio
{
    public static void WorldSoundOneshot(GameObject origin, MinotaurBehaviorController minotaur, AudioSource source, AudioClip sound)
    {
        source.PlayOneShot(sound);
        float minotaurDist = Vector3.Distance(origin.GetComponent<Rigidbody>().position, minotaur.rb.position);
        minotaur.aggro.HearingCheck(origin, source.volume, minotaurDist);
    }
}

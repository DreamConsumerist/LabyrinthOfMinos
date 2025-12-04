using UnityEngine;

public static class WorldAudio
{
    public static void WorldSoundOneshot(GameObject origin,AudioSource source, AudioClip sound)
    {
        source.PlayOneShot(sound);
        float minotaurDist = Vector3.Distance(origin.GetComponent<Rigidbody>().position, MinotaurBehaviorController.Instance.rb.position);
        MinotaurBehaviorController.Instance.aggro.HearingCheck(origin, source.volume, minotaurDist);
    }

    public static void SprintSoundBroadcast(GameObject origin, float volume)
    {
        // May need to regulate for sprinting vs walking volume
        float minotaurDist = Vector3.Distance(origin.GetComponent<Rigidbody>().position, MinotaurBehaviorController.Instance.rb.position);
        MinotaurBehaviorController.Instance.aggro.HearingCheck(origin, volume, minotaurDist);
    }
    public static void WalkSoundBroadcast(GameObject origin, float volume)
    {
        // May need to regulate for walking vs sprinting volume
        float minotaurDist = Vector3.Distance(origin.GetComponent<Rigidbody>().position, MinotaurBehaviorController.Instance.rb.position);
        MinotaurBehaviorController.Instance.aggro.HearingCheck(origin, volume, minotaurDist);
    }
}

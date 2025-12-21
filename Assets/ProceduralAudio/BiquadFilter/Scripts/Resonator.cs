using UnityEngine;

namespace ProceduralAudio.BiquadFilter
{
    public class Resonator : MonoBehaviour
    {
        public void OnCollisionEnter(Collision other)
        {
            if (other.gameObject.TryGetComponent(out BiquadProceduralSoundSource source))
            {
                var data = new ProceduralAudioData();
                data.velocityMagnitude = other.relativeVelocity.magnitude;
                source.PlayProceduralSound(data);
            }
        }
    }
}
using UnityEngine;
using UnityEngine.Audio;

namespace ProceduralAudio.BiquadFilter
{
    public class BiquadProceduralSoundSource : MonoBehaviour
    {
        [SerializeField] private AudioSource _audioSource;

        public void PlayProceduralSound(ProceduralAudioData data)
        {
            _audioSource.Play();
            var handle = _audioSource.generatorInstance;

            if (!ControlContext.builtIn.Exists(handle))
            {
                return;
            }
            
            ControlContext.builtIn.SendMessage(handle, ref data);
        }
    }
}
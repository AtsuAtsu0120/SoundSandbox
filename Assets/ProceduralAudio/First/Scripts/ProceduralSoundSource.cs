using UnityEngine;
using UnityEngine.Audio;

namespace ProceduralAudio.First
{
    public class ProceduralSoundSource : MonoBehaviour
    {
        [SerializeField] private AudioSource _audioSource;
        
        private void OnCollisionEnter(Collision other)
        {
            _audioSource.Play();
        }

        private void Update()
        {
            var handle = _audioSource.generatorInstance;

            if (!_audioSource.isPlaying || !ControlContext.builtIn.Exists(handle))
            {
                return;
            }
            
            var data = new ImpactAudioGenerator.WaveData(0.35f, 0.5f, 0.75f);
            ControlContext.builtIn.SendMessage(handle, ref data);
        }
    }
}

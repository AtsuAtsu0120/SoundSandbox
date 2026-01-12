using UnityEngine;
using UnityEngine.Audio;

namespace ProceduralAudio.MonoBehaviourAudioGenerator
{
    [RequireComponent(typeof(AudioSource))]
    public partial class ProceduralAudioSourceController : MonoBehaviour
    {
        [SerializeField] private AudioSource _audioSource;

        public void Awake()
        {
            _audioSource.generator = this;

            var context = _audioSource.generatorInstance;
            // ControlContext.builtIn.SendMessage(context, ref data);
        }
    }
}
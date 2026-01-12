using System;
using ProceduralAudio.ProceduralCore;
using UnityEngine;
using UnityEngine.Audio;

namespace ProceduralAudio.BiquadFilter
{
    [RequireComponent(typeof(AudioSource))]
    public abstract class ProceduralAudioObjectBase : MonoBehaviour
    {
        public bool IsPlaying => _source.isPlaying;
        
        [SerializeField] private AudioSource _source;
        [SerializeField] private bool _playOnAwake;
        
        private ProcessorInstance _processor;
        private ProceduralResonatorData _resonator;
        private ProceduralInputData _input;

        public void Awake()
        {
            OnAwake();
            if (_playOnAwake)
            {
                Play();
            }
        }
        
        public abstract void Play();

        protected abstract void OnAwake();
        protected abstract void OnUpdate();
        
        protected void PlayInternal(float[] input, ProceduralInputType type)
        {
            _input = new ProceduralInputData(input, type);
            _source.Play();
        }

        protected unsafe void SetResonator(ResonatorObject[] resonator, delegate*<float, float> postprocesser = null)
        {
            _resonator.Dispose();

            unsafe
            {
                _resonator = new ProceduralResonatorData(resonator, postprocesser);
            }
            SendMessage(ref _resonator);
        }

        protected void SetInput(float[] input, ProceduralInputType type)
        {
            _input.Dispose();
            
            _input = new ProceduralInputData(input, type);
            SendMessage(ref _input);
        }

        private void Start()
        {
            _processor = _source.generatorInstance;
        }

        private void Update()
        {
            OnUpdate();
            
            SendMessage(ref _input);
            SendMessage(ref _resonator);
        }
        
        private void OnDestroy()
        {
            _resonator.Dispose();
            _input.Dispose();
        }

        private void SendMessage<T>(ref T data) where T : unmanaged
        {
            if (IsEnabled == false)
            {
                return;
            }
            
            ControlContext.builtIn.SendMessage(_processor, ref data);
        }

        protected bool IsEnabled => ControlContext.builtIn.Exists(_processor);
        
#if UNITY_EDITOR
        private void OnValidate()
        {
            Reset();
        }
#endif
        
        private void Reset()
        {
            _source = GetComponent<AudioSource>();
            _source.playOnAwake = false;
            _source.spatialBlend = 1.0f;
        }
    }
}
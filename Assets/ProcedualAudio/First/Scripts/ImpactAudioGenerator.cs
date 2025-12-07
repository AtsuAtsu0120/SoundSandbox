using System;
using Unity.Burst;
using Unity.IntegerTime;
using UnityEngine;
using UnityEngine.Audio;
using Random = Unity.Mathematics.Random;

namespace ProceduralAudio.First
{
    [CreateAssetMenu(fileName = "ImpactAudioGenerator", menuName = "Procedual Audio/Impact Audio Generator")]
    public class ImpactAudioGenerator : ScriptableObject, IAudioGenerator
    {
        public bool isFinite => true;
        public bool isRealtime => true;
        public DiscreteTime? length => new DiscreteTime(3);
        
        private Processor _processor;
        
        public GeneratorInstance CreateInstance(ControlContext context, AudioFormat? nestedFormat,
            ProcessorInstance.CreationParameters creationParameters)
        {
            _processor = new Processor();
            return context.AllocateGenerator(_processor, new Processor.Control());        
        }

        private static float CalculateDecayCoefficient(float decayDurationSeconds)
        {
            // Approximately -ln(0.001) / decayDurationSeconds to reach 0.1% of original volume (-60dB)
            return 6.908f / decayDurationSeconds;
        }

        [BurstCompile]
        internal struct Processor : GeneratorInstance.IRealtime
        {
            public bool isFinite => true;
            public bool isRealtime => true;
            public DiscreteTime? length => new DiscreteTime(3);
            
            private GeneratorInstance.Setup _setup;
            
            private float _phase1;
            private float _phase2;
            private float _phase3;
            
            private float _deltaTime;
            
            private WaveData _waveData;
            private Random _random;
            private SimpleLowPassFilter _lowPassFilter;
            
            public void Update(ProcessorInstance.UpdatedDataContext context, ProcessorInstance.Pipe pipe)
            {
                var enumerator = pipe.GetAvailableData(context);

                foreach (var data in enumerator)
                {
                    if (data.TryGetData(out WaveData waveData))
                    {
                        _waveData = waveData;
                    }
                }
            }
            
            public GeneratorInstance.Result Process(in RealtimeContext context, ProcessorInstance.Pipe pipe, ChannelBuffer buffer, GeneratorInstance.Arguments args)
            {
                // 1サンプルあたりの時間（秒）
                var sampleDuration = 1.0f / _setup.sampleRate;
                
                for (var frame = 0; frame < buffer.frameCount; frame++)
                {
                    if (_deltaTime > 3.0f)
                    {
                        buffer.Clear();
                        return -1;
                    }
                    
                    for (var channel = 0; channel < buffer.channelCount; channel++)
                    {
                        var whiteNoise = _random.NextFloat(-1, 1);
                        var noiseEnv = Mathf.Exp(-_deltaTime * CalculateDecayCoefficient(0.04f));
                        
                        var noiseComponent = whiteNoise * noiseEnv * 4.0f;
                        
                        var osc1 = Mathf.Sin(_phase1 * (2f * Mathf.PI)) * Mathf.Exp(-_deltaTime * CalculateDecayCoefficient(_waveData.decay1));
                        var osc2 = Mathf.Sin(_phase2 * (2f * Mathf.PI)) * Mathf.Exp(-_deltaTime * CalculateDecayCoefficient(_waveData.decay2));
                        var osc3 = Mathf.Sin(_phase3 * (2f * Mathf.PI)) * Mathf.Exp(-_deltaTime * CalculateDecayCoefficient(_waveData.decay3));
                        
                        var toneComponent = osc1 + osc2 + osc3;
                        
                        var sound = toneComponent + _lowPassFilter.Process(noiseComponent);

                        buffer[frame, channel] = sound;
                    }
                    
                    _phase1 += 300f / _setup.sampleRate;
                    _phase2 += 400f / _setup.sampleRate;
                    _phase3 += 450f / _setup.sampleRate;
                    
                    _deltaTime += sampleDuration;
                }

                return buffer.frameCount;
            }
            
            internal struct Control : GeneratorInstance.IControl<Processor>
            {
                public void Dispose(ControlContext context, ref Processor processor)
                {
                }

                public void Update(ControlContext context, ProcessorInstance.Pipe pipe)
                {
                    
                }

                public ProcessorInstance.Response OnMessage(ControlContext context, ProcessorInstance.Pipe pipe, ProcessorInstance.Message message)
                {
                    var waveData = message.Get<WaveData>();
                    pipe.SendData(context, waveData);
                    
                    return ProcessorInstance.Response.Unhandled;
                }

                public void Configure(ControlContext context, ref Processor processor, in AudioFormat configuration, out GeneratorInstance.Setup setup,
                    ref GeneratorInstance.Properties properties)
                {
                    processor._random = new Random((uint)DateTime.Now.Millisecond);
                    processor._lowPassFilter = new SimpleLowPassFilter();
                    processor._lowPassFilter.UpdateCoefficients(500f, configuration.sampleRate);
                    
                    processor._setup = new GeneratorInstance.Setup(AudioSpeakerMode.Mono, configuration.sampleRate);
                    setup = processor._setup;
                }
            }
        }

        internal readonly struct WaveData
        {
            public WaveData(float decay1, float decay2, float decay3)
            {
                this.decay1 = decay1;
                this.decay2 = decay2;
                this.decay3 = decay3;
            }
            
            public float decay1 { get; }
            public float decay2 { get; }
            public float decay3 { get; }
        }
    }
}
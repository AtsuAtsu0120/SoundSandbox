using System;
using Unity.IntegerTime;
using UnityEngine;
using UnityEngine.Audio;
using Random = Unity.Mathematics.Random;

namespace ProceduralAudio.BiquadFilter
{
    [CreateAssetMenu(fileName = "BiquadImpactAudioGenerator", menuName = "Procedual Audio/Biquad Impact Audio Generator")]
    public class BiquadImpactAudioGenerator : ScriptableObject, IAudioGenerator
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

        // [BurstCompile]
        internal struct Processor : GeneratorInstance.IRealtime
        {
            public bool isFinite => true;
            public bool isRealtime => true;
            public DiscreteTime? length => new DiscreteTime(3);
            
            private GeneratorInstance.Setup _setup;
            private ResonatorData _resonatorData0;
            private ResonatorData _resonatorData1;
            private ResonatorData _resonatorData2;
            private bool _isTriggered;
            
            private ProceduralAudioData _proceduralData;
            
            private Random _random;
            private float _gain1;
            private float _gain2;
            private float _gain3;
            
            public void Update(ProcessorInstance.UpdatedDataContext context, ProcessorInstance.Pipe pipe)
            {
                if (!_isTriggered)
                {
                    return;
                }
                
                foreach (var element in pipe.GetAvailableData(context))
                {
                    if (element.TryGetData(out ProceduralAudioData data))
                    {
                        _proceduralData = data;
                    }
                }
                
                SetupResonators();
            }

            private void SetupResonators()
            {
                var baseQ = 5;
                        
                var intensity = Mathf.Clamp01(_proceduralData.velocityMagnitude / 10.0f);
                var baseGain = Mathf.Pow(intensity, 2.0f);
                        
                // [Resonator 1: 基音 (Low)]
                // どんな衝撃でも比較的鳴る
                var freq1 = 150f;
                _gain1 = 1.0f * baseGain; 
                float q1 = baseQ;

                // [Resonator 2: 倍音 (Mid)]
                var freq2 = 317.0f; 
                _gain2 = 0.8f * baseGain * (0.5f + intensity * 0.5f); // 少し速度依存
                var q2 = baseQ * 1.2f;

                // [Resonator 3: 高次倍音 (High)]
                // ★ここがポイント: 弱い衝突(intensity小)だと gain3 はほぼゼロになる
                // これにより「コトッ(弱)」と「カァァン(強)」の演じ分けができる
                var freq3 = 580.0f;
                _gain3 = 0.6f * baseGain * intensity; 
                var q3 = baseQ * 0.5f; // 高音は早く減衰させるのが自然
                        
                _resonatorData0.Setup(freq1, q1, _setup.sampleRate);
                _resonatorData1.Setup(freq2, q2, _setup.sampleRate);
                _resonatorData2.Setup(freq3, q3, _setup.sampleRate);
            }
            
            public GeneratorInstance.Result Process(in RealtimeContext context, ProcessorInstance.Pipe pipe, ChannelBuffer buffer, GeneratorInstance.Arguments args)
            {
                for (var frame = 0; frame < buffer.frameCount; frame++)
                {
                    var input = 0f;

                    if (_isTriggered)
                    {
                        _isTriggered = false;
                        input = _random.NextFloat(-1, 1);
                    }
                    
                    var result1 = _resonatorData0.Process(input) * _gain1;
                    var result2 = _resonatorData1.Process(input) * _gain2;
                    var result3 = _resonatorData2.Process(input) * _gain3;
                    var value = result1 + result2 + result3;

                    for (var i = 0; i < buffer.channelCount; i++)
                    {
                        buffer[frame, i] = value;
                    }
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
                    var proceduralData = message.Get<ProceduralAudioData>();
                    pipe.SendData(context, proceduralData);
                    
                    return ProcessorInstance.Response.Handled;
                }

                public void Configure(ControlContext context, ref Processor processor, in AudioFormat configuration, out GeneratorInstance.Setup setup,
                    ref GeneratorInstance.Properties properties)
                {
                    processor._random = new Random((uint)DateTime.Now.Millisecond);
                    processor._setup = new GeneratorInstance.Setup(AudioSpeakerMode.Mono, configuration.sampleRate);
                    processor._isTriggered = true;
                    
                    setup = processor._setup;
                }
            }
        }
    }
}
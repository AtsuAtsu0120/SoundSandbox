using ProceduralAudio.ProceduralCore;
using Unity.IntegerTime;
using UnityEngine;
using UnityEngine.Audio;

namespace ProceduralAudio.MonoBehaviourAudioGenerator
{
    public partial class ProceduralAudioSourceController : IAudioGenerator
    {
        public bool isFinite => true;
        public bool isRealtime => true;
        public DiscreteTime? length => new DiscreteTime(3);

        public GeneratorInstance CreateInstance(ControlContext context, AudioFormat? nestedFormat,
            ProcessorInstance.CreationParameters creationParameters)
        {
            return context.AllocateGenerator(new Processor(), new Processor.Control());
        }
        
        // [BurstCompile]
        internal struct Processor : GeneratorInstance.IRealtime
        {
            public bool isFinite => true;
            public bool isRealtime => true;
            public DiscreteTime? length => new DiscreteTime(3);
            
            private GeneratorInstance.Setup _setup;
            
            private ProceduralResonatorData _proceduralData;
            private ProceduralInputData _inputData;
            
            public void Update(ProcessorInstance.UpdatedDataContext context, ProcessorInstance.Pipe pipe)
            {
                foreach (var element in pipe.GetAvailableData(context))
                {
                    if (element.TryGetData(out ProceduralResonatorData data))
                    {
                        _proceduralData = data;
                    }
                    else if (element.TryGetData(out ProceduralInputData inputData))
                    {
                        _inputData = inputData;
                    }
                }
            }
            
            public GeneratorInstance.Result Process(in RealtimeContext context, ProcessorInstance.Pipe pipe, ChannelBuffer buffer, GeneratorInstance.Arguments args)
            {
                for (var frame = 0; frame < buffer.frameCount; frame++)
                {
                    var value = 0f;
                    for (var i = 0; i < _proceduralData.Resonators.Length; i++)
                    {
                        var input = 0f;
                        if (_inputData.Type != ProceduralInputType.None && _inputData.Inputs.Length > i)
                        {
                            Debug.Log("Input!");
                            if (_inputData.Inputs.IsCreated)
                            {
                                input = _inputData.Inputs[i];
                            }
                        }
                        else
                        {
                            Debug.Log(_inputData.Type == ProceduralInputType.None
                                ? "Input type is none"
                                : "Input array is too small");
                        }
                        
                        value += _proceduralData.Resonators[i].Process(input);
                    }

                    for (var i = 0; i < buffer.channelCount; i++)
                    {
                        buffer[frame, i] = value;
                    }
                }

                if (_inputData.Type == ProceduralInputType.OneShot)
                {
                    _inputData.Type = ProceduralInputType.None;
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
                    if (message.Is<ProceduralInputData>())
                    {
                        var inputData = message.Get<ProceduralInputData>();
                        pipe.SendData(context, inputData);
                        
                        return ProcessorInstance.Response.Handled;
                    }

                    if (message.Is<ProceduralResonatorData>())
                    {
                        var resonatorData = message.Get<ProceduralResonatorData>();
                        pipe.SendData(context, resonatorData);

                        return ProcessorInstance.Response.Handled;
                    }
                    
                    return ProcessorInstance.Response.Unhandled;
                }

                public void Configure(ControlContext context, ref Processor processor, in AudioFormat configuration, out GeneratorInstance.Setup setup,
                    ref GeneratorInstance.Properties properties)
                {
                    processor._setup = new GeneratorInstance.Setup(AudioSpeakerMode.Mono, configuration.sampleRate);
                    setup = processor._setup;
                }
            }
        }
    }
}
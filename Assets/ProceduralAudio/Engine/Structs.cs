using System;
using ProceduralAudio.BiquadFilter;
using Unity.Collections;

namespace ProceduralAudio.ProceduralCore
{
    public readonly unsafe struct ProceduralResonatorData : IDisposable
    {
        public ProceduralResonatorData(ResonatorObject[] resonators, delegate*<float, float> postprocesser)
        {
            this.postprocesser = postprocesser;
            Resonators = new NativeArray<ResonatorObject>(resonators, Allocator.Persistent);
        }

        public NativeArray<ResonatorObject> Resonators { get; }
        public readonly delegate*<float, float> postprocesser; 

        public void Dispose()
        {
            Resonators.Dispose();
        }
    }

    public struct ProceduralInputData : IDisposable
    {
        public ProceduralInputData(float[] inputs, ProceduralInputType type)
        {
            Inputs = new NativeArray<float>(inputs, Allocator.Persistent);
            Type = type;
        }

        public NativeArray<float> Inputs { get; }

        public ProceduralInputType Type;

        public void Dispose()
        {
            Inputs.Dispose();
        }
    }

    public struct UserData<T> where T : unmanaged
    {
        private T _userData;
    }
    
    public enum ProceduralInputType
    {
        None,
        OneShot,
        Continuous,
    }
}
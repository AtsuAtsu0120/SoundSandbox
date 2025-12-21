using ProceduralAudio.ProceduralCore;
using UnityEngine;

namespace ProceduralAudio.BiquadFilter
{
    public class ProceduralEngineObject : ProceduralAudioObjectBase
    {
        [SerializeField] private float _rpm = 1000f;
        
        private float _phase;
        
        public override void Play()
        {
            var oscillator = CalculateOscillator();
            PlayInternal(new []{oscillator, oscillator}, ProceduralInputType.Continuous);
            
            Debug.Log("Play Engine");
        }

        protected override void OnAwake()
        {
            Initialize();
        }
        
        private void Initialize()
        {
            var bodyRes = new ResonatorObject();
            bodyRes.Setup(120f, 4.0f, AudioSettings.outputSampleRate);
            bodyRes.SetCompensationGain(10000f);
            bodyRes.OutputScale = 0.7f;
            
            var exhaustRes = new ResonatorObject();
            exhaustRes.Setup(400f, 8.0f, AudioSettings.outputSampleRate);
            exhaustRes.SetCompensationGain(10000f);
            exhaustRes.OutputScale = 0.5f;

            unsafe
            {
                SetResonator(new[] {bodyRes, exhaustRes}, &Postprocess);
            }
        }

        protected override void OnUpdate()
        {
            if (IsPlaying == false)
            {
                return;
            }
            
            var oscillator = CalculateOscillator(); 
            SetInput(new []{oscillator, oscillator}, ProceduralInputType.Continuous);
        }

        private float CalculateOscillator()
        {
            var baseFreq = _rpm / 60f; 
    
            // ノコギリ波の生成
            _phase += baseFreq / AudioSettings.outputSampleRate;
            if (_phase > 1.0f)
            {
                _phase -= 1.0f;
            }
            var oscillator = (_phase * 2.0f) - 1.0f;

            return oscillator;
        }
        
        private static float Postprocess(float input)
        {
            var throttle = 0.5f;
            input *= (1.0f + throttle * 5.0f); // アクセル全開で入力過多にする
    
            // 簡易ディストーション (Soft Clipping)
            if (input > 0.8f) input = 0.8f + (input - 0.8f) * 0.5f;
            if (input < -0.8f) input = -0.8f + (input + 0.8f) * 0.5f;
            
            return input;
        }
    }
}
using System;
using UnityEngine;

namespace ProceduralAudio.BiquadFilter
{
    public struct ResonatorObject : IEquatable<ResonatorObject>
    {
        /// <summary>
        /// 共鳴周波数
        /// </summary>
        private float _frequency;
    
        /// <summary>
        /// 共鳴の強さ
        /// </summary>
        private float _q;
    
        // 内部計算用バッファ
        float b0, b1, b2, a1, a2; // 係数
        float x1, x2, y1, y2;     // 過去の入力/出力サンプル
        
        private float _compensationGain;
        private float _compensationGainScalar;
        public float OutputScale { get; set; }
        
        public void SetCompensationGain(float compensationGainScalar)
        {
            _compensationGainScalar = compensationGainScalar;
        }
        
        public void Setup(float freq, float q, float sampleRate)
        {
            if (_compensationGainScalar <= 0)
            {
                _compensationGainScalar = 100f;
            }
            
            _frequency = freq;
            _q = q;
        
            // --- フィルタ係数計算 (Bandpass Filter) ---
            // ※詳しい数式は "Audio EQ Cookbook" などを参照。ここでは概念的な記述です。
            var omega = 2.0f * Mathf.PI * _frequency / sampleRate;
            var alpha = Mathf.Sin(omega) / (2.0f * _q);
        
            b0 = alpha;
            b1 = 0.0f;
            b2 = -alpha;
            var a0 = 1.0f + alpha;
            a1 = -2.0f * Mathf.Cos(omega) / a0;
            a2 = (1.0f - alpha) / a0;

            _compensationGain = Mathf.Sqrt(_q) * _compensationGainScalar;
        
            // 入力側の係数を正規化
            b0 /= a0; b2 /= a0;
        }

        public float Process(float input)
        {
            Debug.Log($"input: {input}");
            
            // 差分方程式 (Direct Form I)
            float output = (b0 * input) + (b1 * x1) + (b2 * x2) 
                           - (a1 * y1) - (a2 * y2);
            
            // バッファ更新
            x2 = x1; x1 = input;
            y2 = y1; y1 = output;
        
            var lastOutput = output * _compensationGain;
            return lastOutput * OutputScale;
        }

        public bool Equals(ResonatorObject other)
        {
            return _frequency.Equals(other._frequency) && _q.Equals(other._q) && b0.Equals(other.b0) && b1.Equals(other.b1) && b2.Equals(other.b2) && a1.Equals(other.a1) && a2.Equals(other.a2) && x1.Equals(other.x1) && x2.Equals(other.x2) && y1.Equals(other.y1) && y2.Equals(other.y2) && _compensationGain.Equals(other._compensationGain);
        }

        public override bool Equals(object obj)
        {
            return obj is ResonatorObject other && Equals(other);
        }

        public override int GetHashCode()
        {
            var hashCode = new HashCode();
            hashCode.Add(_frequency);
            hashCode.Add(_q);
            hashCode.Add(b0);
            hashCode.Add(b1);
            hashCode.Add(b2);
            hashCode.Add(a1);
            hashCode.Add(a2);
            hashCode.Add(x1);
            hashCode.Add(x2);
            hashCode.Add(y1);
            hashCode.Add(y2);
            hashCode.Add(_compensationGain);
            return hashCode.ToHashCode();
        }
    }
}
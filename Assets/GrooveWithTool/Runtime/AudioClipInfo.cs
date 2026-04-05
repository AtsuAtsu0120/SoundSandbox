using System.Collections.Generic;
using UnityEngine;

namespace GrooveWithTool.Runtime
{
    public class AudioClipInfo : ScriptableObject
    {
        public AudioClip SourceClip => sourceClip;
        public IReadOnlyList<float> BeatTimestamps => beatTimestamps;
        
        [Tooltip("元になったオーディオクリップ")]
        [SerializeField] private AudioClip sourceClip;

        [Tooltip("検知されたビートのタイムスタンプ（秒）")]
        [SerializeField] private List<float> beatTimestamps;

        public void SetData(AudioClip sourceClip, List<float> beatTimestamps)
        {
            this.sourceClip = sourceClip;
            this.beatTimestamps = beatTimestamps;
        }
    }
}
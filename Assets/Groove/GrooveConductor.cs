using UnityEngine;

namespace Groove
{
    public class GrooveConductor : MonoBehaviour
    {
        public float SongPositionInBeats => _songPositionInBeats;
        
        [SerializeField] private float _bpm = 120f;
        [SerializeField] private AudioSource _source;
        
        private double _secondsPerBeat;
        private double _songPosition;
        private double _dspStartTime;

        // 拍管理用
        private float _songPositionInBeats;
        private int _lastReportedBeat;
        
        private void Start()
        {
            _secondsPerBeat = 60.0 / _bpm;
            _dspStartTime = AudioSettings.dspTime;
            _source.Play();
        }
        
        private void Update()
        {
            // 1. 正確な時間を取得
            _songPosition = AudioSettings.dspTime - _dspStartTime;

            // 2. 現在の拍数を計算 (浮動小数点数)
            _songPositionInBeats = (float)(_songPosition / _secondsPerBeat);

            // 3. 拍の切り替わりを検知 (整数部が繰り上がったか)
            if ((int)_songPositionInBeats > _lastReportedBeat)
            {
                _lastReportedBeat = (int)_songPositionInBeats;
                OnBeat(_lastReportedBeat);
            }
        }
        
        private void OnBeat(int beatIndex)
        {
            // ここにグルーヴ処理を書く
            Debug.Log($"Beat: {beatIndex}");

            // 例: 4拍ごとに大きなアクション、それ以外は小さなアクション
            if (beatIndex % 4 == 0) 
            {
                // Big Groove Action
                Debug.Log("Big Groove Action!");
            } 
            else 
            {
                // Small Groove Action
                Debug.Log("Small Groove Action!");
            }
        }
    }
}

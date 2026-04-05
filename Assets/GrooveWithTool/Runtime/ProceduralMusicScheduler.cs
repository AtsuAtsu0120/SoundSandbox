using UnityEngine;

namespace GrooveWithTool.Runtime
{
    [RequireComponent(typeof(MasterGrooveConductor))]
    public class ProceduralMusicScheduler : MonoBehaviour
    {
        [SerializeField]
        private MasterGrooveConductor _conductor;

        [Header("Audio Sources (2つ用意してください)")] 
        [SerializeField] private AudioSource _sourceA;
        [SerializeField] private AudioSource _sourceB;
        private bool useSourceA = true;
        
        [Header("プロシージャル素材リスト")] 
        [SerializeField] private AudioClipInfo[] _audioClipInfoList;

        private double _nextScheduleTime;
        private int _currentClipIndex;

        private void Start()
        {
            _conductor = GetComponent<MasterGrooveConductor>();

            // 最初の再生時間を「現在のdspTime + 1.0秒後」に設定し、初期化の余裕を持たせる
            _nextScheduleTime = AudioSettings.dspTime + 1.0;

            // 最初のクリップを予約
            ScheduleNextClip();
        }

        private void Update()
        {
            // 予約した時間が「残り1秒」に迫ったら、次のフレーズをスケジュールする
            if (AudioSettings.dspTime > _nextScheduleTime - 1.0)
            {
                ScheduleNextClip();
            }
        }

        private void ScheduleNextClip()
        {
            if (_audioClipInfoList.Length == 0) return;
            
            var nextData = _audioClipInfoList[_currentClipIndex];
            var nextClip = nextData.SourceClip;
            var activeSource = useSourceA ? _sourceA : _sourceB;

            // 【未来の絶対時間】に再生を予約
            activeSource.clip = nextClip;
            activeSource.PlayScheduled(_nextScheduleTime);

            // コンダクターに「この時間に、このビート予定を追加して！」と提出
            _conductor.ScheduleBeats(nextData, _nextScheduleTime);

            // 次の予約時間を更新（現在の予約時間 ＋ AudioClipの長さ）
            _nextScheduleTime += (double)nextClip.samples / nextClip.frequency;

            // 次回のためにSourceとIndexを切り替え
            useSourceA = !useSourceA;
            _currentClipIndex = (_currentClipIndex + 1) % _audioClipInfoList.Length;
        }

        private void Reset()
        {
            _conductor = GetComponent<MasterGrooveConductor>();
        }
    }
}
using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Audio;

/// <summary>
/// SoundManager (단일 인스턴스, 조기 초기화)
/// - 구조: 단일 BGM(2소스 크로스페이드) + SFX 풀(2D/3D) + 사운드 라이브러리(Inspector 매핑)
/// - 성능:
///   * SFX는 미리 생성된 풀(AudioSource)에서 PlayOneShot/Play 사용 → 생성/파괴 비용 없음
///   * BGM은 A/B 두 소스를 교대로 사용하여 크로스페이드 지원
/// - 사용법(요약):
///   * SFX(2D): SoundManager.Instance.Play(SoundId.ItemPickup);
///   * SFX(3D): SoundManager.Instance.PlayAt(SoundId.EnemyHit, worldPos);
///   * BGM:     SoundManager.Instance.PlayBGM(SoundId.Bgm_Day, 0.8f);
///   * 볼륨:    mixer 노출 파라미터명 "MasterVolume","BGMVolume","SFXVolume"
/// - 주의:
///   * 동일 씬/전역에 SoundManager는 **하나만** 존재해야 함(DontDestroyOnLoad 옵션 제공)
///   * Inspector의 library 배열에서 SoundId ↔ AudioClip 매핑을 반드시 해줄 것
/// </summary>
[DefaultExecutionOrder(-100)]
public class SoundManager : MonoBehaviour
{
    // =========================
    //  Singleton
    // =========================
    public static SoundManager Instance { get; private set; }

    [Header("Audio Mixer (옵션)")]
    [Tooltip("AudioMixer를 연결하면 볼륨 제어 API(SetMaster/BGM/SFX) 사용 가능. " +
             "노출 파라미터명은 반드시 'MasterVolume', 'BGMVolume', 'SFXVolume' 일치 필요.")]
    [SerializeField] private AudioMixer mixer;

    [Header("BGM Sources")]
    [Tooltip("BGM 루프 재생용 소스 A")]
    [SerializeField] private AudioSource bgmA; // loop 전용
    [Tooltip("BGM 크로스페이드 시 보조 소스 B")]
    [SerializeField] private AudioSource bgmB; // crossfade 보조

    [Header("SFX Pool")]
    [Tooltip("동시에 겹쳐 울릴 수 있는 2D SFX 채널 수(권장: 8~16)")]
    [SerializeField, Min(1)] private int sfxPoolSize = 12;   // 2D 풀 개수
    [Tooltip("동시에 겹쳐 울릴 수 있는 3D SFX 채널 수(권장: 4~8)")]
    [SerializeField, Min(1)] private int sfx3dPoolSize = 4;  // 3D 풀 개수
    [Tooltip("씬 전환에도 유지할지 여부")]
    [SerializeField] private bool dontDestroyOnLoad = true;

    [Header("Library (Inspector에서 매핑)")]
    [Tooltip("SoundId ↔ AudioClip 매핑 테이블. 각 항목별 기본 볼륨/피치변동/연타 제한 설정")]
    public SoundEntry[] library;

    /// <summary>
    /// 사운드 라이브러리 항목(Inspector 매핑 전용)
    /// </summary>
    [Serializable]
    public class SoundEntry
    {
        [Tooltip("식별용 ID (enum)")]
        public SoundId id;

        [Tooltip("재생할 AudioClip")]
        public AudioClip clip;

        [Tooltip("클립 기본 재생 볼륨(0~1)")]
        [Range(0f, 1f)] public float volume = 1f;

        [Tooltip("재생 시 피치 랜덤 변동폭(0~0.5). SFX 반복에 자연스러움 부여")]
        [Range(0f, 0.5f)] public float pitchVariance = 0.05f;

        [Tooltip("같은 사운드 연타 제한(초). 0이면 제한 없음")]
        public float minInterval = 0f;

        [Tooltip("기본적으로 3D로 쓸지 정보(자동 재생에 참고용). " +
                 "PlayAuto 사용 시 true면 3D, false면 2D로 결정")]
        public bool spatial = false;
    }

    /// <summary>
    /// 게임에서 사용할 사운드 ID 열거형
    /// 필요에 따라 자유롭게 추가/정리 가능
    /// </summary>
    public enum SoundId
    {
        // ---- SFX ----
        Click,
        ItemPickup,
        Fire,
        Walk,
        PlayerHit,
        EnemyHit,
        Shield,
        Tooltip,
        WarpUp,
        GoalIn,
        StageClear,
        GameOver,

        // ---- BGM / Ambience ----
        Bgm_Day,
        Bgm_Night,
        Bgm_Result,
        AmbientAirport
    }

    // =========================
    //  Internal State
    // =========================
    private Dictionary<SoundId, SoundEntry> _db; // SoundId → Entry
    private List<AudioSource> _sfxPool2D;        // 2D SFX 풀
    private List<AudioSource> _sfxPool3D;        // 3D SFX 풀
    private int _sfxIndex2D;                     // 라운드 로빈 인덱스(2D)
    private int _sfxIndex3D;                     // 라운드 로빈 인덱스(3D)
    private readonly Dictionary<SoundId, float> _lastPlay = new(); // 연타 제한 타임스탬프

    // BGM 페이드 중복 방지 플래그
    private bool _fading;

    // 현재 활성/대기 BGM 소스 선택자
    private AudioSource ActiveBgm => bgmA.isPlaying ? bgmA : bgmB;
    private AudioSource IdleBgm => bgmA.isPlaying ? bgmB : bgmA;

    // =========================
    //  Unity Lifecycle
    // =========================
    private void Awake()
    {
        // 싱글톤 보장
        if (Instance != null && Instance != this) { Destroy(gameObject); return; }
        Instance = this;
        if (dontDestroyOnLoad) DontDestroyOnLoad(gameObject);

        BuildDatabase(); // Inspector 라이브러리를 사전으로 구성
        BuildPools();    // SFX 풀 생성
        EnsureBgmSources(); // BGM 소스 초기화
    }

    // =========================
    //  Build Helpers
    // =========================
    /// <summary>
    /// Inspector의 library 배열을 Dictionary로 전개.
    /// 동일 ID가 중복되면 마지막 항목으로 덮어씌워짐.
    /// </summary>
    private void BuildDatabase()
    {
        _db = new Dictionary<SoundId, SoundEntry>(library?.Length ?? 0);
        if (library == null) return;

        foreach (var e in library)
        {
            if (e == null || e.clip == null) continue;
            _db[e.id] = e;
        }
    }

    /// <summary>
    /// 2D/3D SFX 재생용 AudioSource 풀 생성.
    /// 런타임 중 생성/파괴를 피하여 GC/성능 이슈 최소화.
    /// </summary>
    private void BuildPools()
    {
        // 2D 풀
        _sfxPool2D = new List<AudioSource>(Mathf.Max(1, sfxPoolSize));
        for (int i = 0; i < Mathf.Max(1, sfxPoolSize); i++)
        {
            var src = gameObject.AddComponent<AudioSource>();
            src.playOnAwake = false;
            src.loop = false;
            src.spatialBlend = 0f; // 2D
            _sfxPool2D.Add(src);
        }

        // 3D 풀
        _sfxPool3D = new List<AudioSource>(Mathf.Max(1, sfx3dPoolSize));
        for (int i = 0; i < Mathf.Max(1, sfx3dPoolSize); i++)
        {
            var go = new GameObject($"SFX3D_{i}");
            go.transform.SetParent(transform, false);

            var src = go.AddComponent<AudioSource>();
            src.playOnAwake = false;
            src.loop = false;
            src.spatialBlend = 1f; // 3D
            // 감쇠(rolloff)와 거리 설정은 프로젝트 스케일에 맞춰 조정 권장
            src.rolloffMode = AudioRolloffMode.Linear;
            src.minDistance = 2f;
            src.maxDistance = 25f;

            _sfxPool3D.Add(src);
        }
    }

    /// <summary>
    /// BGM용 AudioSource 2개(A/B) 확보 및 기본 설정.
    /// 초기에는 A 볼륨=1, B 볼륨=0 (교대 재생)
    /// </summary>
    private void EnsureBgmSources()
    {
        if (bgmA == null) bgmA = gameObject.AddComponent<AudioSource>();
        if (bgmB == null) bgmB = gameObject.AddComponent<AudioSource>();

        bgmA.loop = true; bgmB.loop = true;
        bgmA.playOnAwake = false; bgmB.playOnAwake = false;
        bgmA.spatialBlend = 0f; bgmB.spatialBlend = 0f;
        bgmA.volume = 1f; bgmB.volume = 0f;
    }

    // =========================
    //  Public API (SFX)
    // =========================

    /// <summary>
    /// SFX(2D) 간편 재생. 위치와 무관하게 화면 중앙에서 재생되는 느낌.
    /// </summary>
    /// <param name="id">라이브러리에 매핑된 SoundId</param>
    /// <param name="volumeScale">개별 호출 시 볼륨 가중치(0~1)</param>
    public void Play(SoundId id, float volumeScale = 1f)
    {
        if (!TryGetEntry(id, out var e)) return;
        if (BlockedByInterval(id, e.minInterval)) return;

        var src = _sfxPool2D[_sfxIndex2D = (_sfxIndex2D + 1) % _sfxPool2D.Count];
        src.pitch = 1f + UnityEngine.Random.Range(-e.pitchVariance, e.pitchVariance);
        src.volume = 1f; // PlayOneShot에 볼륨을 전달하므로 여기선 기본값 유지
        src.PlayOneShot(e.clip, Mathf.Clamp01(e.volume * volumeScale));
    }

    /// <summary>
    /// SFX(3D) 월드 좌표 재생. 공간감/거리 감쇠가 필요한 효과음에 사용.
    /// </summary>
    /// <param name="id">라이브러리에 매핑된 SoundId</param>
    /// <param name="worldPos">재생할 월드 위치</param>
    /// <param name="volumeScale">개별 호출 시 볼륨 가중치(0~1)</param>
    public void PlayAt(SoundId id, Vector3 worldPos, float volumeScale = 1f)
    {
        if (!TryGetEntry(id, out var e)) return;
        if (BlockedByInterval(id, e.minInterval)) return;

        var src = _sfxPool3D[_sfxIndex3D = (_sfxIndex3D + 1) % _sfxPool3D.Count];
        src.transform.position = worldPos;
        src.pitch = 1f + UnityEngine.Random.Range(-e.pitchVariance, e.pitchVariance);
        src.clip = e.clip;
        src.volume = Mathf.Clamp01(e.volume * volumeScale);

        // 3D 소스는 PlayOneShot보다 clip+Play가 제어(위치/감쇠 세팅) 관점에서 안전
        src.Stop(); // 이전 잔재생 방지
        src.Play();
    }

    /// <summary>
    /// SFX 자동 재생(2D/3D). 라이브러리의 spatial 플래그를 참조해 모드 선택.
    /// - spatial == true → 3D로 재생(월드 좌표 필수)
    /// - spatial == false → 2D로 재생
    /// </summary>
    /// <param name="id">SoundId</param>
    /// <param name="worldPos">3D 재생 시 사용할 위치</param>
    /// <param name="volumeScale">볼륨 가중치</param>
    public void PlayAuto(SoundId id, Vector3 worldPos, float volumeScale = 1f)
    {
        if (!TryGetEntry(id, out var e)) return;
        if (e.spatial) PlayAt(id, worldPos, volumeScale);
        else Play(id, volumeScale);
    }

    // =========================
    //  Public API (BGM)
    // =========================

    /// <summary>
    /// BGM 전환. 페이드 시간(fadeSeconds) > 0이면 A↔B 소스 간 크로스페이드.
    /// </summary>
    public void PlayBGM(SoundId bgmId, float fadeSeconds = 0.8f)
    {
        if (!TryGetEntry(bgmId, out var e)) return;

        if (fadeSeconds <= 0f)
        {
            // 즉시 교체
            ActiveBgm.Stop();
            var to = IdleBgm;
            to.clip = e.clip;
            to.volume = 1f;
            to.Play();
        }
        else
        {
            // 크로스페이드
            StartCoroutine(CrossfadeBGM(e.clip, fadeSeconds));
        }
    }

    /// <summary>
    /// 현재 재생 중인 모든 BGM을 페이드아웃(또는 즉시 정지).
    /// </summary>
    public void StopBGM(float fadeSeconds = 0.5f)
    {
        if (fadeSeconds <= 0f) { bgmA.Stop(); bgmB.Stop(); return; }
        StartCoroutine(FadeOutBoth(fadeSeconds));
    }

    // =========================
    //  Mixer Helpers (옵션)
    // =========================

    /// <summary>
    /// Master 볼륨(0~1)을 dB(-80~0) 범위로 맵핑하여 Mixer에 적용
    /// </summary>
    public void SetMasterVolume01(float v) => SetDb("MasterVolume", v);

    /// <summary>
    /// BGM 볼륨(0~1)을 dB(-80~0) 범위로 맵핑하여 Mixer에 적용
    /// </summary>
    public void SetBGMVolume01(float v) => SetDb("BGMVolume", v);

    /// <summary>
    /// SFX 볼륨(0~1)을 dB(-80~0) 범위로 맵핑하여 Mixer에 적용
    /// </summary>
    public void SetSFXVolume01(float v) => SetDb("SFXVolume", v);

    // =========================
    //  Legacy Wrapper (호환)
    //  - 기존 프로젝트의 함수명 변경 최소화 목적
    // =========================

    public void Click() => Play(SoundId.Click);

    public void StageClear()
    {
        // 효과음 + 결과 BGM으로의 자연스러운 전환
        Play(SoundId.StageClear);
        PlayBGM(SoundId.Bgm_Result, 0.7f);
    }

    public void GameOver()
    {
        // 효과음 + BGM 페이드아웃
        Play(SoundId.GameOver);
        StopBGM(0.8f);
    }

    public void EnemyFire() => Play(SoundId.Fire);
    public void Playerwalks() => Play(SoundId.Walk, 0.6f);

    /// <summary>
    /// 레벨 숫자 기반 BGM 매핑(기존 API 호환)
    /// </summary>
    public void PlayBackgroundMusic(int level)
    {
        var id = level switch
        {
            1 => SoundId.Bgm_Day,
            2 => SoundId.Bgm_Night,
            _ => SoundId.Bgm_Day
        };
        PlayBGM(id, 0.8f);
    }

    // =========================
    //  Internal Utils
    // =========================

    /// <summary>
    /// 라이브러리에서 SoundId에 해당하는 항목을 얻음.
    /// Editor 환경에서는 매핑 누락 시 경고 로그 출력.
    /// </summary>
    private bool TryGetEntry(SoundId id, out SoundEntry entry)
    {
        if (_db != null && _db.TryGetValue(id, out entry) && entry.clip != null) return true;
        entry = null;
#if UNITY_EDITOR
        Debug.LogWarning($"[SoundManager] 라이브러리에 '{id}'가 매핑되지 않았습니다.");
#endif
        return false;
    }

    /// <summary>
    /// 동일 사운드의 과도한 연타를 제한(피로감/소음 방지)
    /// </summary>
    private bool BlockedByInterval(SoundId id, float minInterval)
    {
        if (minInterval <= 0f) return false;

        float now = Time.unscaledTime; // 타임스케일 무시(일시정지 중에도 간격 유지)
        if (_lastPlay.TryGetValue(id, out float last) && now - last < minInterval)
            return true;

        _lastPlay[id] = now;
        return false;
    }

    /// <summary>
    /// BGM 크로스페이드: Active → Idle로 전환
    /// </summary>
    private IEnumerator CrossfadeBGM(AudioClip next, float duration)
    {
        if (next == null) yield break;
        if (_fading) yield break; // 중복 페이드 방지
        _fading = true;

        var from = ActiveBgm;
        var to = IdleBgm;

        to.clip = next;
        to.volume = 0f;
        to.Play();

        float t = 0f;
        while (t < duration)
        {
            t += Time.unscaledDeltaTime; // 게임 일시정지 중에도 페이드 진행
            float a = Mathf.Clamp01(t / duration);
            to.volume = a;
            from.volume = 1f - a;
            yield return null;
        }

        from.Stop();
        to.volume = 1f;
        _fading = false;
    }

    /// <summary>
    /// 양쪽 BGM 소스 모두 같은 속도로 페이드아웃
    /// </summary>
    private IEnumerator FadeOutBoth(float duration)
    {
        float t = 0f, a0 = bgmA.volume, b0 = bgmB.volume;
        while (t < duration)
        {
            t += Time.unscaledDeltaTime;
            float k = 1f - Mathf.Clamp01(t / duration);
            bgmA.volume = a0 * k;
            bgmB.volume = b0 * k;
            yield return null;
        }
        bgmA.Stop();
        bgmB.Stop();
    }

    /// <summary>
    /// 0~1 값을 Mixer dB(-80~0)로 맵핑하여 적용
    /// </summary>
    private void SetDb(string param, float v01)
    {
        if (mixer == null) return;

        v01 = Mathf.Clamp01(v01);
        // 0 → -80dB(사실상 mute), 1 → 0dB
        float db = (v01 <= 0.0001f) ? -80f : Mathf.Lerp(-30f, 0f, v01);
        mixer.SetFloat(param, db);
    }
}

/*
===========================================
연결 예시 (프로젝트 내 다른 스크립트)
===========================================

1) 아이템 획득 시 (예: Item.cs, Player.cs 등)
-------------------------------------------------
SoundManager.Instance?.Play(SoundManager.SoundId.ItemPickup);

2) 적과 충돌 시 (예: Enemy.cs)
-------------------------------------------------
SoundManager.Instance?.Play(SoundManager.SoundId.PlayerHit);
// 3D로 현장감 주고 싶다면:
SoundManager.Instance?.PlayAt(SoundManager.SoundId.EnemyHit, transform.position);

3) 클리어/게임오버 (예: GameManager.cs)
-------------------------------------------------
void GameClear() {
    // ... UI, 타임스케일 처리 ...
    SoundManager.Instance?.StageClear();
}

void GameOver() {
    // ... UI, 타임스케일 처리 ...
    SoundManager.Instance?.GameOver();
}

4) 레벨 시작 시 BGM
-------------------------------------------------
SoundManager.Instance?.PlayBackgroundMusic(level); // 1: Day, 2: Night, 기타: Day

===========================================
임포트/믹싱 권장(모바일)
===========================================
- SFX:
  * Load Type = Decompress On Load
  * Force To Mono
  * Compression: Vorbis q≈0.6~0.8
- BGM:
  * Load Type = Streaming
  * Preload Audio Data OFF
  * Compression: Vorbis q≈0.5
- AudioMixer:
  * 노출 파라미터명 정확히: "MasterVolume", "BGMVolume", "SFXVolume"
===========================================
*/

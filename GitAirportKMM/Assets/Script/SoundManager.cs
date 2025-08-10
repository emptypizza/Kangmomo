using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Audio;

[DefaultExecutionOrder(-100)]
public class SoundManager : MonoBehaviour
{
    public static SoundManager Instance { get; private set; }

    [Header("Audio Mixer (옵션)")]
    [SerializeField] private AudioMixer mixer; // 노출 파라미터명: "MasterVolume","BGMVolume","SFXVolume"

    [Header("BGM Sources")]
    [SerializeField] private AudioSource bgmA; // loop 전용
    [SerializeField] private AudioSource bgmB; // 크로스페이드용 보조

    [Header("SFX Pool")]
    [SerializeField, Min(1)] private int sfxPoolSize = 12;
    [SerializeField] private bool dontDestroyOnLoad = true;

    [Header("Library")]
    public SoundEntry[] library; // 인스펙터에서 클립만 꽂으면 됨

    [Serializable]
    public class SoundEntry
    {
        public SoundId id;
        public AudioClip clip;
        [Range(0f, 1f)] public float volume = 1f;
        [Range(0f, 0.5f)] public float pitchVariance = 0.05f;
        [Tooltip("같은 사운드 연타 제한(초). 0이면 무제한")]
        public float minInterval = 0f;
        [Tooltip("3D로 재생(위치재생)할지 여부")]
        public bool spatial = false;
    }

    public enum SoundId
    {
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
        // BGM
        Bgm_Day,
        Bgm_Night,
        Bgm_Result,
        AmbientAirport
    }

    // ---------------- private ----------------
    Dictionary<SoundId, SoundEntry> _db;
    List<AudioSource> _sfxPool2D;
    List<AudioSource> _sfxPool3D;
    int _sfxIndex2D, _sfxIndex3D;
    Dictionary<SoundId, float> _lastPlay = new();

    bool _fading;
    AudioSource ActiveBgm => bgmA.isPlaying ? bgmA : bgmB;
    AudioSource IdleBgm   => bgmA.isPlaying ? bgmB : bgmA;

    void Awake()
    {
        if (Instance != null && Instance != this) { Destroy(gameObject); return; }
        Instance = this;
        if (dontDestroyOnLoad) DontDestroyOnLoad(gameObject);

        // 라이브러리 로드
        _db = new Dictionary<SoundId, SoundEntry>(library?.Length ?? 0);
        if (library != null)
        {
            foreach (var e in library)
            {
                if (e != null && e.clip != null) _db[e.id] = e;
            }
        }

        // 풀 생성
        _sfxPool2D = new List<AudioSource>(sfxPoolSize);
        _sfxPool3D = new List<AudioSource>(Mathf.Max(4, sfxPoolSize / 3));
        for (int i = 0; i < sfxPoolSize; i++)
        {
            var src = gameObject.AddComponent<AudioSource>();
            src.playOnAwake = false; src.loop = false; src.spatialBlend = 0f;
            _sfxPool2D.Add(src);
        }
        for (int i = 0; i < _sfxPool3D.Capacity; i++)
        {
            var go = new GameObject($"SFX3D_{i}");
            go.transform.SetParent(transform, false);
            var src = go.AddComponent<AudioSource>();
            src.playOnAwake = false; src.loop = false; src.spatialBlend = 1f;
            src.rolloffMode = AudioRolloffMode.Linear;
            src.minDistance = 2f; src.maxDistance = 25f;
            _sfxPool3D.Add(src);
        }

        // BGM 소스 기본셋
        if (bgmA == null) bgmA = gameObject.AddComponent<AudioSource>();
        if (bgmB == null) bgmB = gameObject.AddComponent<AudioSource>();
        bgmA.loop = true; bgmB.loop = true;
        bgmA.playOnAwake = false; bgmB.playOnAwake = false;
        bgmA.spatialBlend = 0f; bgmB.spatialBlend = 0f;
        bgmA.volume = 1f; bgmB.volume = 0f;
    }

    // ==================== Public API ====================

    /// <summary>간편 SFX 재생(2D)</summary>
    public void Play(SoundId id, float volumeScale = 1f)
    {
        if (!_db.TryGetValue(id, out var e) || e.clip == null) return;
        if (BlockedByInterval(id, e.minInterval)) return;

        var src = _sfxPool2D[_sfxIndex2D = (_sfxIndex2D + 1) % _sfxPool2D.Count];
        src.pitch = 1f + UnityEngine.Random.Range(-e.pitchVariance, e.pitchVariance);
        src.PlayOneShot(e.clip, e.volume * volumeScale);
    }

    /// <summary>월드 좌표에서 SFX(3D) 재생</summary>
    public void PlayAt(SoundId id, Vector3 worldPos, float volumeScale = 1f)
    {
        if (!_db.TryGetValue(id, out var e) || e.clip == null) return;
        if (BlockedByInterval(id, e.minInterval)) return;

        var src = _sfxPool3D[_sfxIndex3D = (_sfxIndex3D + 1) % _sfxPool3D.Count];
        src.transform.position = worldPos;
        src.pitch = 1f + UnityEngine.Random.Range(-e.pitchVariance, e.pitchVariance);
        src.clip = e.clip; src.volume = e.volume * volumeScale;
        src.Stop(); src.Play();
    }

    /// <summary>BGM 즉시 교체 또는 크로스페이드</summary>
    public void PlayBGM(SoundId bgmId, float fadeSeconds = 0.8f)
    {
        if (!_db.TryGetValue(bgmId, out var e) || e.clip == null) return;

        if (fadeSeconds <= 0f)
        {
            ActiveBgm.Stop();
            IdleBgm.clip = e.clip; IdleBgm.volume = 1f; IdleBgm.Play();
        }
        else
        {
            StartCoroutine(CrossfadeBGM(e.clip, fadeSeconds));
        }
    }

    public void StopBGM(float fadeSeconds = 0.5f)
    {
        if (fadeSeconds <= 0f) { bgmA.Stop(); bgmB.Stop(); return; }
        StartCoroutine(FadeOutBoth(fadeSeconds));
    }

    // ===== Mixer Helpers (옵션) =====
    public void SetMasterVolume01(float v) => SetDb("MasterVolume", v);
    public void SetBGMVolume01(float v)    => SetDb("BGMVolume", v);
    public void SetSFXVolume01(float v)    => SetDb("SFXVolume", v);

    // ================== Legacy Wrapper (호환용) ==================
    // 기존 코드에서 호출하던 함수명 유지
    public void Click()        => Play(SoundId.Click);
    public void StageClear()   { Play(SoundId.StageClear); PlayBGM(SoundId.Bgm_Result, 0.7f); }
    public void GameOver()     { Play(SoundId.GameOver);  StopBGM(0.8f); }
    public void EnemyFire()    => Play(SoundId.Fire);
    public void Playerwalks()  => Play(SoundId.Walk, 0.6f);

    // 레벨 숫자에 따른 BGM(기존 API)
    public void PlayBackgroundMusic(int level)
    {
        var id = level switch { 1 => SoundId.Bgm_Day, 2 => SoundId.Bgm_Night, _ => SoundId.Bgm_Day };
        PlayBGM(id, 0.8f);
    }

    // ================== internal ==================
    bool BlockedByInterval(SoundId id, float minInterval)
    {
        if (minInterval <= 0f) return false;
        var now = Time.unscaledTime;
        if (_lastPlay.TryGetValue(id, out var last) && now - last < minInterval) return true;
        _lastPlay[id] = now; return false;
    }

    IEnumerator CrossfadeBGM(AudioClip next, float duration)
    {
        if (_fading) yield break;
        _fading = true;

        var from = ActiveBgm; var to = IdleBgm;
        to.clip = next; to.volume = 0f; to.Play();
        float t = 0f;
        while (t < duration)
        {
            t += Time.unscaledDeltaTime;
            float a = t / duration;
            to.volume = a; from.volume = 1f - a;
            yield return null;
        }
        from.Stop(); to.volume = 1f;
        _fading = false;
    }

    IEnumerator FadeOutBoth(float duration)
    {
        float t = 0f, a0 = bgmA.volume, b0 = bgmB.volume;
        while (t < duration)
        {
            t += Time.unscaledDeltaTime;
            float k = 1f - (t / duration);
            bgmA.volume = a0 * k; bgmB.volume = b0 * k;
            yield return null;
        }
        bgmA.Stop(); bgmB.Stop();
    }

    void SetDb(string param, float v01)
    {
        if (mixer == null) return;
        v01 = Mathf.Clamp01(v01);
        // -80dB ~ 0dB 매핑
        float db = v01 <= 0.0001f ? -80f : Mathf.Lerp(-30f, 0f, v01);
        mixer.SetFloat(param, db);
    }
}


using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

[DisallowMultipleComponent]
public sealed class AudioController : MonoBehaviour
{
    private const string MusicEnabledKey = "audio_music_enabled";
    private const string SfxEnabledKey = "audio_sfx_enabled";
    private const string MusicVolumeKey = "audio_music_volume";
    private const string SfxVolumeKey = "audio_sfx_volume";
    private const float MutedThreshold = 0.0001f;
    private const float DefaultMusicVolume = 0.5f;
    private const float DefaultSfxVolume = 1f;

    internal static AudioController Instance { get; private set; }

    [Header("User Volume Controls")]
    [SerializeField] private Slider musicSlider;
    [SerializeField] private Slider soundSlider;

    [Header("Primary Audio Sources (created automatically if empty)")]
    [SerializeField] private AudioSource backgroundMusicSource;
    [SerializeField] private AudioSource uiSource;
    [SerializeField] private AudioSource reelSource;
    [SerializeField] private AudioSource featureSource;
    [Tooltip("Dedicated source so the Ultra prize-wheel spinning clip can be stopped without cutting off other feature sounds.")]
    [SerializeField] private AudioSource bonusReelSpinningSource;
    [SerializeField] private AudioSource reserveSource;

    [Header("Background")]
    [SerializeField] private AudioClip backgroundMusicClip;
    [Tooltip(
        "Looping music used while the three-reel Ultra Wheel slot is active. " +
        "Assign this clip from the Inspector.")]
    [SerializeField] private AudioClip ultraWheelSlotBackgroundMusicClip;

    [Header("UI and Bet")]
    [SerializeField] private AudioClip uiButtonClip;
    [SerializeField] private AudioClip infoPanelArrowButtonClip;
    [SerializeField] private AudioClip betButtonClip;
    [SerializeField] private AudioClip maxBetClip;
    [SerializeField] private AudioClip spinButtonClip;
    [SerializeField] private AudioClip turboRocketClip;

    [Header("Reels and Wins")]
    [SerializeField] private AudioClip reelStopHitClip;
    [SerializeField] private AudioClip allReelsStoppedClip;
    [SerializeField] private AudioClip hatAppearInReelClip;
    [SerializeField] private AudioClip queenClip;
    [SerializeField] private AudioClip magicalReelLineClip;
    [SerializeField] private AudioClip totalWinClip;

    [Header("Scatter and Ultra Features")]
    [UnityEngine.Serialization.FormerlySerializedAs("richesWheelClip")]
    [SerializeField] private AudioClip scatterWheelClip;
    [UnityEngine.Serialization.FormerlySerializedAs("ultraWheelBonusClip")]
    [Tooltip("Played only while the Ultra entry leaves transition is running.")]
    [SerializeField] private AudioClip leavesFallingClip;
    [SerializeField] private AudioClip ultraWheelAllThreeClip;
    [SerializeField] private AudioClip bonusReelSpinningClip;
    [SerializeField] private AudioClip bonusReelThreeNumberIconClip;
    [SerializeField] private AudioClip bonusGoingDownClip;

    [Header("Unclassified")]
    [Tooltip("The file is named 'extra sound.wav', so it is available without guessing where it belongs.")]
    [SerializeField] private AudioClip extraSoundClip;

    private readonly Dictionary<AudioSource, bool> preFocusMuteState =
        new Dictionary<AudioSource, bool>();

    private bool isForceMuted;
    private bool externalMuteRequested;
    private bool applicationHasFocus = true;
    private bool applicationPaused;
    private bool musicEnabled = true;
    private bool sfxEnabled = true;
    private float musicVolume = DefaultMusicVolume;
    private float sfxVolume = DefaultSfxVolume;

    internal bool MusicEnabled => musicEnabled;
    internal bool SfxEnabled => sfxEnabled;
    internal float MusicVolume => musicVolume;
    internal float SfxVolume => sfxVolume;

    private void Awake()
    {
        if (Instance != null && Instance != this)
        {
            Debug.LogWarning(
                "[AudioController] A second controller was disabled. " +
                "Keep one AudioController active in the scene.");
            enabled = false;
            return;
        }

        Instance = this;
        EnsurePrimarySources();
        LoadSavedSettings();
        ApplyMusicVolume();
        ApplySfxVolume();
    }

    private void Start()
    {
        PlayBackgroundMusic();
    }

    private void OnEnable()
    {
        if (musicSlider != null)
        {
            musicSlider.onValueChanged.AddListener(OnMusicVolumeChanged);
        }

        if (soundSlider != null)
        {
            soundSlider.onValueChanged.AddListener(OnSoundVolumeChanged);
        }
    }

    private void OnDisable()
    {
        if (musicSlider != null)
        {
            musicSlider.onValueChanged.RemoveListener(
                OnMusicVolumeChanged);
        }

        if (soundSlider != null)
        {
            soundSlider.onValueChanged.RemoveListener(
                OnSoundVolumeChanged);
        }

        externalMuteRequested = false;
        applicationHasFocus = true;
        applicationPaused = false;
        ApplyForcedMute(false);
    }

    private void OnDestroy()
    {
        if (Instance == this)
        {
            Instance = null;
        }
    }

    internal void SetMusicEnabled(bool enabledValue)
    {
        musicEnabled = enabledValue;
        PlayerPrefs.SetInt(MusicEnabledKey, musicEnabled ? 1 : 0);
        PlayerPrefs.Save();
        ApplyMusicVolume();
    }

    internal void SetSfxEnabled(bool enabledValue)
    {
        sfxEnabled = enabledValue;
        PlayerPrefs.SetInt(SfxEnabledKey, sfxEnabled ? 1 : 0);
        PlayerPrefs.Save();
        ApplySfxVolume();
    }

    internal void SetMusicVolume(float value)
    {
        musicVolume = Mathf.Clamp01(value);
        PlayerPrefs.SetFloat(MusicVolumeKey, musicVolume);
        PlayerPrefs.Save();
        ApplyMusicVolume();
    }

    internal void SetSfxVolume(float value)
    {
        sfxVolume = Mathf.Clamp01(value);
        PlayerPrefs.SetFloat(SfxVolumeKey, sfxVolume);
        PlayerPrefs.Save();
        ApplySfxVolume();
    }

    internal void SetMuteAll(bool forceMute)
    {
        externalMuteRequested = forceMute;
        RefreshForcedMute();
    }

    private void ApplyForcedMute(bool forceMute)
    {
        if (forceMute == isForceMuted)
        {
            return;
        }

        isForceMuted = forceMute;

        if (forceMute)
        {
            preFocusMuteState.Clear();
            AudioSource[] sources = FindSceneAudioSources();
            foreach (AudioSource source in sources)
            {
                if (source == null)
                {
                    continue;
                }

                preFocusMuteState[source] = source.mute;
                source.mute = true;
            }

            return;
        }

        RestorePreFocusMuteState();
    }

    internal void PlayBackgroundMusic()
    {
        if (backgroundMusicSource == null || backgroundMusicClip == null)
        {
            return;
        }

        if (backgroundMusicSource.isPlaying &&
            backgroundMusicSource.clip == backgroundMusicClip)
        {
            return;
        }

        backgroundMusicSource.clip = backgroundMusicClip;
        backgroundMusicSource.loop = true;
        backgroundMusicSource.Play();
    }

    internal void StopBackgroundMusic()
    {
        StopSource(backgroundMusicSource);
    }

    internal void PlayUltraWheelSlotBackgroundMusic()
    {
        if (backgroundMusicSource == null ||
            ultraWheelSlotBackgroundMusicClip == null)
        {
            return;
        }

        if (backgroundMusicSource.isPlaying &&
            backgroundMusicSource.clip ==
                ultraWheelSlotBackgroundMusicClip)
        {
            return;
        }

        backgroundMusicSource.clip =
            ultraWheelSlotBackgroundMusicClip;
        backgroundMusicSource.loop = true;
        backgroundMusicSource.Play();
    }

    internal void StopUltraWheelSlotBackgroundMusic()
    {
        if (backgroundMusicSource == null ||
            backgroundMusicSource.clip !=
                ultraWheelSlotBackgroundMusicClip)
        {
            return;
        }

        StopSource(backgroundMusicSource);
        PlayBackgroundMusic();
    }

    internal void PlayUiButton()
    {
        PlaySfx(uiSource, uiButtonClip);
    }

    internal void PlayInfoPanelArrowButton()
    {
        PlaySfx(uiSource, infoPanelArrowButtonClip);
    }

    internal void PlayBetButton()
    {
        PlaySfx(uiSource, betButtonClip);
    }

    internal void PlayMaxBet()
    {
        // Max Bet replaces any still-playing normal bet click so the two
        // sounds can never overlap.
        uiSource?.Stop();
        PlaySfx(uiSource, maxBetClip);
    }

    internal void PlaySpinButton()
    {
        PlaySfx(uiSource, spinButtonClip);
    }

    internal void PlayTurboRocket()
    {
        PlaySfx(uiSource, turboRocketClip);
    }

    internal void PlayReelStopHit()
    {
        PlaySfx(reelSource, reelStopHitClip);
    }

    internal void PlayAllReelsStopped()
    {
        PlaySfx(reelSource, allReelsStoppedClip);
    }

    internal void PlayHatAppearInReel()
    {
        PlaySfx(reelSource, hatAppearInReelClip);
    }

    internal void PlayQueen()
    {
        PlaySfx(featureSource, queenClip);
    }

    internal float PlayMagicalReelLine()
    {
        PlaySfx(featureSource, magicalReelLineClip);
        return magicalReelLineClip != null
            ? Mathf.Max(0f, magicalReelLineClip.length)
            : 0f;
    }

    internal void PlayTotalWin()
    {
        PlaySfx(featureSource, totalWinClip);
    }

    internal void PlayScatterWheel()
    {
        PlaySfx(featureSource, scatterWheelClip);
    }

    internal void PlayLeavesFalling()
    {
        PlaySfx(featureSource, leavesFallingClip);
    }

    internal void PlayUltraWheelAllThree()
    {
        PlaySfx(featureSource, ultraWheelAllThreeClip);
    }

    internal void PlayBonusReelSpinning()
    {
        PlaySfx(bonusReelSpinningSource, bonusReelSpinningClip);
    }

    internal void StopBonusReelSpinning()
    {
        bonusReelSpinningSource?.Stop();
    }

    internal void PlayBonusReelThreeNumberIcon()
    {
        PlaySfx(featureSource, bonusReelThreeNumberIconClip);
    }

    internal void PlayBonusGoingDown()
    {
        PlaySfx(featureSource, bonusGoingDownClip);
    }

    internal void PlayExtraSound()
    {
        PlaySfx(reserveSource, extraSoundClip);
    }

    private void OnApplicationFocus(bool focus)
    {
        applicationHasFocus = focus;
        RefreshForcedMute();
    }

    private void OnApplicationPause(bool paused)
    {
        applicationPaused = paused;
        RefreshForcedMute();
    }

    private void OnMusicVolumeChanged(float value)
    {
        ReleaseForcedMuteForUserInteraction();
        musicEnabled = value > MutedThreshold;
        PlayerPrefs.SetInt(MusicEnabledKey, musicEnabled ? 1 : 0);
        SetMusicVolume(value);
    }

    private void OnSoundVolumeChanged(float value)
    {
        ReleaseForcedMuteForUserInteraction();
        sfxEnabled = value > MutedThreshold;
        PlayerPrefs.SetInt(SfxEnabledKey, sfxEnabled ? 1 : 0);
        SetSfxVolume(value);
    }

    private void LoadSavedSettings()
    {
        musicEnabled = PlayerPrefs.GetInt(MusicEnabledKey, 1) == 1;
        sfxEnabled = PlayerPrefs.GetInt(SfxEnabledKey, 1) == 1;
        musicVolume = Mathf.Clamp01(PlayerPrefs.GetFloat(
            MusicVolumeKey,
            DefaultMusicVolume));
        sfxVolume = Mathf.Clamp01(PlayerPrefs.GetFloat(
            SfxVolumeKey,
            DefaultSfxVolume));

        if (musicSlider != null)
        {
            musicSlider.SetValueWithoutNotify(
                musicEnabled ? musicVolume : 0f);
        }

        if (soundSlider != null)
        {
            soundSlider.SetValueWithoutNotify(
                sfxEnabled ? sfxVolume : 0f);
        }
    }

    private void EnsurePrimarySources()
    {
        backgroundMusicSource = EnsureSource(
            backgroundMusicSource,
            true);
        uiSource = EnsureSource(uiSource, false);
        reelSource = EnsureSource(reelSource, false);
        featureSource = EnsureSource(featureSource, false);
        bonusReelSpinningSource = EnsureSource(
            bonusReelSpinningSource,
            false);
        reserveSource = EnsureSource(reserveSource, false);
    }

    private AudioSource EnsureSource(AudioSource source, bool loop)
    {
        if (source == null)
        {
            source = gameObject.AddComponent<AudioSource>();
        }

        source.playOnAwake = false;
        source.loop = loop;
        source.spatialBlend = 0f;
        return source;
    }

    private void ApplyMusicVolume()
    {
        float value = musicEnabled ? musicVolume : 0f;
        ApplySourceVolume(backgroundMusicSource, value);
    }

    private void ApplySfxVolume()
    {
        float value = sfxEnabled ? sfxVolume : 0f;
        ApplySourceVolume(uiSource, value);
        ApplySourceVolume(reelSource, value);
        ApplySourceVolume(featureSource, value);
        ApplySourceVolume(bonusReelSpinningSource, value);
        ApplySourceVolume(reserveSource, value);
    }

    private void PlaySfx(AudioSource source, AudioClip clip)
    {
        if (!sfxEnabled || source == null || clip == null)
        {
            return;
        }

        source.PlayOneShot(clip);
    }

    private static void StopSource(AudioSource source)
    {
        if (source == null)
        {
            return;
        }

        source.Stop();
        source.loop = false;
    }

    private void ReleaseForcedMuteForUserInteraction()
    {
        if (!isForceMuted)
        {
            return;
        }

        externalMuteRequested = false;
        RefreshForcedMute();
    }

    private void RefreshForcedMute()
    {
        ApplyForcedMute(
            externalMuteRequested ||
            !applicationHasFocus ||
            applicationPaused);
    }

    private void RestorePreFocusMuteState()
    {
        foreach (KeyValuePair<AudioSource, bool> entry in
                 preFocusMuteState)
        {
            if (entry.Key != null)
            {
                entry.Key.mute = entry.Value;
            }
        }

        preFocusMuteState.Clear();
    }

    private static void ApplySourceVolume(AudioSource source, float value)
    {
        if (source == null)
        {
            return;
        }

        float volume = Mathf.Clamp01(value);
        source.volume = volume;
        source.mute = volume <= MutedThreshold;
    }

    private static AudioSource[] FindSceneAudioSources()
    {
        return FindObjectsByType<AudioSource>(
            FindObjectsInactive.Include,
            FindObjectsSortMode.None);
    }

#if UNITY_EDITOR
    private void Reset()
    {
        AutoAssignAudioClipsFromFolder();
    }

    private void OnValidate()
    {
        AutoAssignAudioClipsFromFolder();
    }

    [ContextMenu("Auto Assign Clips From Assets/Arts/Audio")]
    private void AutoAssignAudioClipsFromFolder()
    {
        bool changed = false;

        changed |= AssignClipIfEmpty(
            ref backgroundMusicClip,
            "bg music.mp3");
        changed |= AssignClipIfEmpty(ref uiButtonClip, "ui button.mp3");
        changed |= AssignClipIfEmpty(
            ref infoPanelArrowButtonClip,
            "info panel arrow button.mp3");
        changed |= AssignClipIfEmpty(ref betButtonClip, "bet button.mp3");
        changed |= AssignClipIfEmpty(ref maxBetClip, "max bet.mp3");
        changed |= AssignClipIfEmpty(ref spinButtonClip, "spin button.mp3");
        changed |= AssignClipIfEmpty(ref turboRocketClip, "turbo rocket.mp3");
        changed |= AssignClipIfEmpty(
            ref reelStopHitClip,
            "reel stop hit.mp3");
        changed |= AssignClipIfEmpty(
            ref allReelsStoppedClip,
            "all reel stop done.mp3");
        changed |= AssignClipIfEmpty(
            ref hatAppearInReelClip,
            "hat apperar in reel.mp3");
        changed |= AssignClipIfEmpty(ref queenClip, "Q .mp3");
        changed |= AssignClipIfEmpty(
            ref magicalReelLineClip,
            "magical reel line.mp3");
        changed |= AssignClipIfEmpty(
            ref leavesFallingClip,
            "Leaves fallling.mp3");
        changed |= AssignClipIfEmpty(
            ref leavesFallingClip,
            "Leaves falling.mp3");
        changed |= AssignClipIfEmpty(
            ref ultraWheelAllThreeClip,
            "ultra wheel all 3.mp3");
        changed |= AssignClipIfEmpty(
            ref bonusReelSpinningClip,
            "bonus reel spinning.mp3");
        changed |= AssignClipIfEmpty(
            ref bonusReelThreeNumberIconClip,
            "bonus reel 3 number icon.mp3");
        changed |= AssignClipIfEmpty(
            ref bonusGoingDownClip,
            "bonus going down.mp3");
        changed |= AssignClipIfEmpty(ref extraSoundClip, "extra sound.wav");

        if (changed)
        {
            UnityEditor.EditorUtility.SetDirty(this);
        }
    }

    private static bool AssignClipIfEmpty(
        ref AudioClip target,
        string fileName)
    {
        if (target != null)
        {
            return false;
        }

        target = UnityEditor.AssetDatabase.LoadAssetAtPath<AudioClip>(
            "Assets/Arts/Audio/" + fileName);
        return target != null;
    }
#endif
}

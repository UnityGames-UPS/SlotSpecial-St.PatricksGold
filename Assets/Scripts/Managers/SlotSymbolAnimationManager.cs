using System.Collections;
using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

[DisallowMultipleComponent]
public class SlotSymbolAnimationManager : MonoBehaviour
{
    [Header("Low Symbol Sequence Frames")]
    [Tooltip("Add every Ace animation frame in playback order.")]
    [SerializeField] private Sprite[] aceFrames = new Sprite[0];
    [SerializeField] private Sprite[] kingFrames = new Sprite[0];
    [SerializeField] private Sprite[] queenFrames = new Sprite[0];
    [SerializeField] private Sprite[] jackFrames = new Sprite[0];
    [SerializeField] private Sprite[] tenFrames = new Sprite[0];

    [Header("High Symbol Sequence Frames")]
    [SerializeField] private Sprite[] beerGlassFrames = new Sprite[0];
    [SerializeField] private Sprite[] greenHatFrames = new Sprite[0];
    [SerializeField] private Sprite[] magnetFrames = new Sprite[0];
    [SerializeField] private Sprite[] cigarFrames = new Sprite[0];

    [Header("Special Symbol Sequence Frames")]
    [SerializeField] private Sprite[] wildFrames = new Sprite[0];
    [SerializeField] private Sprite[] ultraWheelFrames = new Sprite[0];
    [SerializeField] private Sprite[] templeRichesFrames = new Sprite[0];

    [Header("Scatter Wheel Intro Sequences")]
    [Tooltip("Main Scatter Wheel animation frames in playback order.")]
    [UnityEngine.Serialization.FormerlySerializedAs("scatterWheelFrames")]
    [SerializeField] private Sprite[] scatterWheelMainFrames =
        new Sprite[0];
    [Tooltip(
        "Background FX frames played in sync with the Main Scatter Wheel " +
        "sequence. Add this complete sequence here, not on the Scatter " +
        "presentation component.")]
    [SerializeField] private Sprite[] scatterWheelBackgroundFxFrames =
        new Sprite[0];
    [Tooltip(
        "Total playback duration of the Main Scatter Wheel sequence.")]
    [SerializeField, Min(0.01f)]
    private float scatterWheelIntroDuration = 1.6f;
    [Tooltip(
        "Playback-speed multiplier for the Scatter Background FX. " +
        "For example, 0.4 plays the FX at 40% speed so it remains visible " +
        "longer than the Main Scatter Wheel sequence.")]
    [SerializeField, Range(0.01f, 1f)]
    private float scatterWheelBackgroundFxSpeedMultiplier = 0.4f;
    [Tooltip(
        "Time used to fade out the Scatter Background FX after its " +
        "single forward pass finishes.")]
    [SerializeField, Min(0.01f)]
    private float scatterWheelBackgroundFxFadeDuration = 0.2f;
    [Tooltip(
        "Scale used when CanRotate, WheelRim, and Leaf first appear after " +
        "the Main Scatter Wheel sequence.")]
    [SerializeField, Range(0.01f, 2f)]
    private float scatterWheelHandoffStartScale = 1f;
    [Tooltip(
        "Final size multiplier for CanRotate, WheelRim, and Leaf. " +
        "For example, 1.1 makes them 10% larger.")]
    [SerializeField, Range(0.01f, 2f)]
    private float scatterWheelHandoffEndScale = 1.1f;
    [Tooltip(
        "Time for CanRotate, WheelRim, and Leaf to enlarge to their " +
        "authored sizes after the Main Scatter Wheel sequence.")]
    [SerializeField, Min(0.01f)]
    private float scatterWheelHandoffScaleDuration = 1f;
    [Tooltip(
        "Time for the Scatter server-award text to move vertically to the " +
        "Leaf Y position after the wheel stops.")]
    [SerializeField, Min(0.01f)]
    private float scatterWheelResultTextMoveDuration = 0.8f;

    [Header("Anticipation Sequence Frames")]
    [Tooltip("Frames for the reel-border anticipation animation, in playback order.")]
    [SerializeField] private Sprite[] anticipationFrames = new Sprite[0];
    [Tooltip("Duration in seconds of one complete anticipation loop. Lower values play faster.")]
    [SerializeField, Min(0.05f)] private float anticipationLoopDuration = 0.8f;

    [Header("Ultra Entry Transition")]
    [Tooltip(
        "Full-screen transition Image. Keep it outside the panels that are " +
        "deactivated during the swap.")]
    [SerializeField] private Image ultraEntryTransitionImage;
    [Tooltip(
        "Ultra Wheel Bonus text shown over the entry transition. It grows " +
        "from zero to its authored RectTransform scale, then shrinks after " +
        "the transition finishes.")]
    [SerializeField] private RectTransform ultraWheelBonusText;
    [Tooltip("Time used to grow and shrink the Ultra Wheel Bonus text.")]
    [SerializeField, Min(0.01f)]
    private float ultraWheelBonusTextScaleDuration = 0.3f;
    [Tooltip(
        "Drag every Ultra entry transition sprite here in playback order. " +
        "The panels swap halfway through this sequence.")]
    [SerializeField] private Sprite[] ultraEntryTransitionFrames =
        new Sprite[0];
    [Tooltip("Duration of each Ultra entry transition frame. 0.03333 is 30 FPS.")]
    [SerializeField, Min(0.001f)]
    private float ultraEntryTransitionFrameDuration = 1f / 30f;

    [Header("Ultra Result Winning Symbol Animations")]
    [Tooltip("Winning Green Ultra wheel-symbol frames in playback order.")]
    [SerializeField] private Sprite[] greenUltraWinningSymbolFrames =
        new Sprite[0];
    [Tooltip("Winning Blue Ultra wheel-symbol frames in playback order.")]
    [SerializeField] private Sprite[] blueUltraWinningSymbolFrames =
        new Sprite[0];
    [Tooltip("Winning Red Ultra wheel-symbol frames in playback order.")]
    [SerializeField] private Sprite[] redUltraWinningSymbolFrames =
        new Sprite[0];
    [Tooltip("Duration of each Ultra winning-symbol frame. 0.03333 is 30 FPS.")]
    [SerializeField, Min(0.001f)]
    private float ultraWinningSymbolFrameDuration = 1f / 30f;
    [Tooltip(
        "Number of complete Ultra winning-symbol animation loops to play " +
        "before opening the next panel.")]
    [SerializeField, Min(1)]
    private int ultraWinningSymbolLoopCount = 3;

    [Header("Ultra Wheel Stop Result Animations")]
    [Tooltip(
        "Image used for the one-shot animation after the Blue Ultra wheel " +
        "stops on its server result.")]
    [SerializeField] private Image blueUltraWheelStopResultImage;
    [Tooltip(
        "Blue Ultra wheel stop-result frames in playback order. These play " +
        "once; they do not loop.")]
    [SerializeField] private Sprite[] blueUltraWheelStopResultFrames =
        new Sprite[0];
    [Tooltip(
        "Image used for the one-shot animation after the Red Ultra wheel " +
        "stops on its server result.")]
    [SerializeField] private Image redUltraWheelStopResultImage;
    [Tooltip(
        "Red Ultra wheel stop-result frames in playback order. These play " +
        "once; they do not loop.")]
    [SerializeField] private Sprite[] redUltraWheelStopResultFrames =
        new Sprite[0];
    [Tooltip(
        "Duration of each Blue/Red Ultra wheel stop-result frame. " +
        "0.03333 is 30 FPS.")]
    [SerializeField, Min(0.001f)]
    private float ultraWheelStopResultFrameDuration = 1f / 30f;

    [Header("Win Animation Timing")]
    [Tooltip("Duration in seconds of one complete winning-symbol loop at 1x speed. Lower values play faster.")]
    [SerializeField, Min(0.1f)] private float winSymbolLoopDuration = 1f;
    [UnityEngine.Serialization.FormerlySerializedAs(
        "wildWinLoopsBeforeNextStage")]
    [SerializeField, HideInInspector, Min(1)]
    private int winLoopsBeforeNextStage = 2;

    [SerializeField, HideInInspector, Min(0.001f)]
    private float frameDuration = 1f / 30f;
    [SerializeField, HideInInspector, Min(0f)] private float startDelay;
    [SerializeField, HideInInspector, Min(0)] private int loopCount;
    [SerializeField, HideInInspector, Min(0f)] private float delayBetweenLoops;
    [SerializeField, HideInInspector] private bool useUnscaledTime;

    private readonly Dictionary<Image, ActiveSymbolAnimation> activeAnimations =
        new Dictionary<Image, ActiveSymbolAnimation>();
    private readonly Dictionary<RectTransform, Coroutine>
        activeScatterWheelHandoffs =
            new Dictionary<RectTransform, Coroutine>();
    private readonly Dictionary<RectTransform, Coroutine>
        activeScatterWheelResultTextMoves =
            new Dictionary<RectTransform, Coroutine>();
    private readonly Dictionary<Image, Coroutine> activeAnticipations =
        new Dictionary<Image, Coroutine>();
    private readonly List<UltraWinningSymbolAnimationTarget>
        activeUltraWinningTargets =
            new List<UltraWinningSymbolAnimationTarget>();

    private Coroutine ultraEntryTransitionCoroutine;
    private Coroutine ultraWheelBonusTextScaleCoroutine;
    private Coroutine ultraWinningSymbolCoroutine;
    private Coroutine blueUltraWheelStopResultCoroutine;
    private Coroutine redUltraWheelStopResultCoroutine;
    private Vector3 ultraWheelBonusTextOriginalScale = Vector3.one;

    private sealed class ActiveSymbolAnimation
    {
        internal Image BaseImage;
        internal Image OverlayImage;
        internal Image SecondaryOverlayImage;
        internal Sprite[] SecondaryFrames;
        internal int SecondaryOriginalSiblingIndex = -1;
        internal Coroutine PlaybackRoutine;
        internal float MinimumVisualExtent;
        internal float MaximumVisualExtent;
        internal float CurrentVisualSize01;
    }

    private void Awake()
    {
        if (ultraWheelBonusText != null)
        {
            ultraWheelBonusTextOriginalScale =
                ultraWheelBonusText.localScale;
            if (ultraWheelBonusTextOriginalScale == Vector3.zero)
            {
                ultraWheelBonusTextOriginalScale = Vector3.one;
            }

            HideUltraWheelBonusTextImmediate();
        }

        StopUltraWheelStopResultAnimations();
    }

    private void OnDisable()
    {
        StopUltraEntryTransition();
        StopUltraWinningSymbolAnimations();
        StopUltraWheelStopResultAnimations();
        StopAllAnimations();
        StopAllScatterWheelHandoffs();
        StopAllScatterWheelResultTextMoves();
        StopAllAnticipations();
    }

    private void OnDestroy()
    {
        StopUltraEntryTransition();
        StopUltraWinningSymbolAnimations();
        StopUltraWheelStopResultAnimations();
        StopAllAnimations();
        StopAllScatterWheelHandoffs();
        StopAllScatterWheelResultTextMoves();
        StopAllAnticipations();
    }

    internal float GetWinSymbolLoopDuration()
    {
        return Mathf.Max(0.1f, winSymbolLoopDuration);
    }

    internal float GetScatterWheelIntroDuration()
    {
        return Mathf.Max(0.01f, scatterWheelIntroDuration);
    }

    internal int GetScatterWheelMainFrameCount()
    {
        return scatterWheelMainFrames?.Length ?? 0;
    }

    internal int GetWinLoopsBeforeNextStage()
    {
        return Mathf.Max(1, winLoopsBeforeNextStage);
    }

    internal bool PlayAnimation(
        int symbolId,
        Image baseImage,
        Image overlayImage,
        float synchronizedLoopDuration = 0f)
    {
        if (baseImage == null || overlayImage == null)
        {
            return false;
        }

        AlignOverlayWithoutResizing(overlayImage);

        if (!TryGetFrames(symbolId, out Sprite[] frames))
        {
            StopAnimation(baseImage, overlayImage);
            Debug.LogWarning(
                $"No sequence frames are configured for symbol '{StPatricksGoldSymbolIds.GetName(symbolId)}'.",
                this);
            return false;
        }

        StopAnimation(baseImage, overlayImage);
        overlayImage.sprite = frames[0];
        SetAlpha(baseImage, 1f);
        SetAlpha(overlayImage, 0f);
        GetVisualExtentRange(
            frames,
            out float minimumVisualExtent,
            out float maximumVisualExtent);

        ActiveSymbolAnimation activeAnimation = new ActiveSymbolAnimation
        {
            BaseImage = baseImage,
            OverlayImage = overlayImage,
            MinimumVisualExtent = minimumVisualExtent,
            MaximumVisualExtent = maximumVisualExtent,
            CurrentVisualSize01 = GetNormalizedVisualSize(
                frames[0],
                minimumVisualExtent,
                maximumVisualExtent)
        };

        activeAnimations[overlayImage] = activeAnimation;
        activeAnimation.PlaybackRoutine =
            StartCoroutine(
                PlayFrames(
                    activeAnimation,
                    frames,
                    GetLoopDuration(frames, synchronizedLoopDuration)));
        return true;
    }

    internal bool PlayAnimationOnce(
        int symbolId,
        Image baseImage,
        Image overlayImage,
        int frameCount,
        float playbackDuration,
        Action onComplete)
    {
        if (baseImage == null || overlayImage == null)
        {
            return false;
        }

        AlignOverlayWithoutResizing(overlayImage);

        if (!TryGetFrames(symbolId, out Sprite[] frames))
        {
            StopAnimation(baseImage, overlayImage);
            Debug.LogWarning(
                $"No sequence frames are configured for symbol " +
                $"'{StPatricksGoldSymbolIds.GetName(symbolId)}'.",
                this);
            return false;
        }

        int framesToPlay = Mathf.Clamp(frameCount, 1, frames.Length);
        float duration = Mathf.Max(0.01f, playbackDuration);

        StopAnimation(baseImage, overlayImage);
        overlayImage.sprite = frames[0];
        SetAlpha(baseImage, 1f);
        SetAlpha(overlayImage, 0f);
        GetVisualExtentRange(
            frames,
            out float minimumVisualExtent,
            out float maximumVisualExtent);

        ActiveSymbolAnimation activeAnimation = new ActiveSymbolAnimation
        {
            BaseImage = baseImage,
            OverlayImage = overlayImage,
            MinimumVisualExtent = minimumVisualExtent,
            MaximumVisualExtent = maximumVisualExtent,
            CurrentVisualSize01 = GetNormalizedVisualSize(
                frames[0],
                minimumVisualExtent,
                maximumVisualExtent)
        };

        activeAnimations[overlayImage] = activeAnimation;
        activeAnimation.PlaybackRoutine =
            StartCoroutine(
                PlayFramesOnce(
                    activeAnimation,
                    frames,
                    framesToPlay,
                    duration,
                    onComplete));
        return true;
    }

    internal bool PlayScatterWheelIntro(
        Image baseImage,
        Image mainAnimationImage,
        Image backgroundFxImage,
        Action onComplete,
        Action onBackgroundFxComplete = null)
    {
        if (baseImage == null || mainAnimationImage == null)
        {
            return false;
        }

        AlignOverlayWithoutResizing(mainAnimationImage);
        if (scatterWheelMainFrames == null ||
            scatterWheelMainFrames.Length == 0)
        {
            StopScatterWheelIntro(
                baseImage,
                mainAnimationImage,
                backgroundFxImage);
            Debug.LogWarning(
                "[SlotSymbolAnimationManager] No Main Scatter Wheel " +
                "sequence frames are configured.",
                this);
            return false;
        }

        StopScatterWheelIntro(
            baseImage,
            mainAnimationImage,
            backgroundFxImage);

        Sprite[] backgroundFrames =
            backgroundFxImage != null &&
            scatterWheelBackgroundFxFrames != null &&
            scatterWheelBackgroundFxFrames.Length > 0
                ? scatterWheelBackgroundFxFrames
                : null;
        if (backgroundFxImage == null)
        {
            Debug.LogWarning(
                "[SlotSymbolAnimationManager] The Scatter Wheel Background " +
                "FX Image could not be found on the symbol.",
                this);
        }
        else if (backgroundFrames == null)
        {
            Debug.LogWarning(
                "[SlotSymbolAnimationManager] No Scatter Wheel Background " +
                "FX sequence frames are configured.",
                this);
        }

        mainAnimationImage.sprite = scatterWheelMainFrames[0];
        SetAlpha(baseImage, 1f);
        SetAlpha(mainAnimationImage, 0f);
        GetVisualExtentRange(
            scatterWheelMainFrames,
            out float minimumVisualExtent,
            out float maximumVisualExtent);

        var activeAnimation = new ActiveSymbolAnimation
        {
            BaseImage = baseImage,
            OverlayImage = mainAnimationImage,
            SecondaryOverlayImage = backgroundFxImage,
            SecondaryFrames = backgroundFrames,
            SecondaryOriginalSiblingIndex =
                backgroundFxImage != null
                    ? backgroundFxImage.transform.GetSiblingIndex()
                    : -1,
            MinimumVisualExtent = minimumVisualExtent,
            MaximumVisualExtent = maximumVisualExtent,
            CurrentVisualSize01 = GetNormalizedVisualSize(
                scatterWheelMainFrames[0],
                minimumVisualExtent,
                maximumVisualExtent)
        };

        if (backgroundFxImage != null)
        {
            AlignOverlayWithoutResizing(backgroundFxImage);
            if (backgroundFrames != null)
            {
                backgroundFxImage.sprite = backgroundFrames[0];
            }
            SetAlpha(backgroundFxImage, 0f);
        }

        activeAnimations[mainAnimationImage] = activeAnimation;
        activeAnimation.PlaybackRoutine =
            StartCoroutine(
                PlayScatterWheelIntroFrames(
                    activeAnimation,
                    onComplete,
                    onBackgroundFxComplete));
        return true;
    }

    internal void StopScatterWheelIntro(
        Image baseImage,
        Image mainAnimationImage,
        Image backgroundFxImage)
    {
        StopAnimation(baseImage, mainAnimationImage);
        SetAlpha(backgroundFxImage, 0f);
        if (backgroundFxImage != null)
        {
            backgroundFxImage.gameObject.SetActive(false);
        }
    }

    internal void PlayScatterWheelHandoff(
        RectTransform rotatingWheel,
        Vector3 rotatingWheelTargetScale,
        RectTransform wheelRim,
        Vector3 wheelRimTargetScale,
        RectTransform leaf,
        Vector3 leafTargetScale)
    {
        if (rotatingWheel == null)
        {
            return;
        }

        StopScatterWheelHandoff(rotatingWheel);
        SetActive(rotatingWheel, true);
        SetActive(wheelRim, true);
        SetActive(leaf, true);

        float startScale =
            Mathf.Max(0.01f, scatterWheelHandoffStartScale);
        ApplyScaleMultiplier(
            rotatingWheel,
            rotatingWheelTargetScale,
            startScale);
        ApplyScaleMultiplier(
            wheelRim,
            wheelRimTargetScale,
            startScale);
        ApplyScaleMultiplier(
            leaf,
            leafTargetScale,
            startScale);

        Coroutine handoffRoutine = StartCoroutine(
            PlayScatterWheelHandoffScale(
                rotatingWheel,
                rotatingWheelTargetScale,
                wheelRim,
                wheelRimTargetScale,
                leaf,
                leafTargetScale));
        activeScatterWheelHandoffs[rotatingWheel] =
            handoffRoutine;
    }

    internal void StopScatterWheelHandoff(
        RectTransform rotatingWheel)
    {
        if (rotatingWheel == null ||
            !activeScatterWheelHandoffs.TryGetValue(
                rotatingWheel,
                out Coroutine handoffRoutine))
        {
            return;
        }

        if (handoffRoutine != null)
        {
            StopCoroutine(handoffRoutine);
        }

        activeScatterWheelHandoffs.Remove(rotatingWheel);
    }

    private void StopAllScatterWheelHandoffs()
    {
        if (activeScatterWheelHandoffs.Count == 0)
        {
            return;
        }

        var handoffRoutines =
            new List<Coroutine>(
                activeScatterWheelHandoffs.Values);
        activeScatterWheelHandoffs.Clear();
        for (int index = 0;
             index < handoffRoutines.Count;
             index++)
        {
            if (handoffRoutines[index] != null)
            {
                StopCoroutine(handoffRoutines[index]);
            }
        }
    }

    internal bool PlayScatterWheelResultTextMove(
        RectTransform resultText,
        float targetAnchoredY,
        Action onComplete)
    {
        if (resultText == null)
        {
            return false;
        }

        StopScatterWheelResultTextMove(resultText);
        Coroutine moveRoutine = StartCoroutine(
            PlayScatterWheelResultTextMoveFrames(
                resultText,
                targetAnchoredY,
                onComplete));
        activeScatterWheelResultTextMoves[resultText] =
            moveRoutine;
        return true;
    }

    internal void StopScatterWheelResultTextMove(
        RectTransform resultText)
    {
        if (resultText == null ||
            !activeScatterWheelResultTextMoves.TryGetValue(
                resultText,
                out Coroutine moveRoutine))
        {
            return;
        }

        if (moveRoutine != null)
        {
            StopCoroutine(moveRoutine);
        }

        activeScatterWheelResultTextMoves.Remove(resultText);
    }

    private void StopAllScatterWheelResultTextMoves()
    {
        if (activeScatterWheelResultTextMoves.Count == 0)
        {
            return;
        }

        var moveRoutines =
            new List<Coroutine>(
                activeScatterWheelResultTextMoves.Values);
        activeScatterWheelResultTextMoves.Clear();
        for (int index = 0;
             index < moveRoutines.Count;
             index++)
        {
            if (moveRoutines[index] != null)
            {
                StopCoroutine(moveRoutines[index]);
            }
        }
    }

    internal bool TryGetAnimationVisualSize(
        Image overlayImage,
        out float normalizedVisualSize)
    {
        if (overlayImage != null &&
            activeAnimations.TryGetValue(
                overlayImage,
                out ActiveSymbolAnimation activeAnimation))
        {
            normalizedVisualSize =
                Mathf.Clamp01(activeAnimation.CurrentVisualSize01);
            return true;
        }

        normalizedVisualSize = 0f;
        return false;
    }

    internal void StopAnimation(Image baseImage, Image overlayImage)
    {
        ActiveSymbolAnimation stoppedAnimation = null;
        if (overlayImage != null &&
            activeAnimations.TryGetValue(overlayImage, out ActiveSymbolAnimation activeAnimation))
        {
            if (activeAnimation.PlaybackRoutine != null)
            {
                StopCoroutine(activeAnimation.PlaybackRoutine);
            }

            activeAnimations.Remove(overlayImage);
            stoppedAnimation = activeAnimation;
        }

        if (stoppedAnimation != null)
        {
            RestoreAnimationLayers(stoppedAnimation);
        }
        else
        {
            SetAlpha(baseImage, 1f);
            SetAlpha(overlayImage, 0f);
        }
    }

    internal void StopAllAnimations()
    {
        if (activeAnimations.Count == 0)
        {
            return;
        }

        List<ActiveSymbolAnimation> animations =
            new List<ActiveSymbolAnimation>(activeAnimations.Values);
        activeAnimations.Clear();

        for (int i = 0; i < animations.Count; i++)
        {
            ActiveSymbolAnimation animation = animations[i];
            if (animation.PlaybackRoutine != null)
            {
                StopCoroutine(animation.PlaybackRoutine);
            }

            RestoreAnimationLayers(animation);
        }
    }

    internal bool PlayAnticipation(Image targetImage)
    {
        if (targetImage == null)
        {
            return false;
        }

        StopAnticipation(targetImage);
        if (anticipationFrames == null || anticipationFrames.Length == 0)
        {
            SetAlpha(targetImage, 0f);
            targetImage.gameObject.SetActive(false);
            Debug.LogWarning(
                "[SlotSymbolAnimationManager] No anticipation frames are configured.",
                this);
            return false;
        }

        targetImage.gameObject.SetActive(true);
        targetImage.sprite = anticipationFrames[0];
        SetAlpha(targetImage, 1f);
        activeAnticipations[targetImage] = null;
        Coroutine playbackRoutine =
            StartCoroutine(PlayAnticipationFrames(targetImage));
        if (activeAnticipations.ContainsKey(targetImage))
        {
            activeAnticipations[targetImage] = playbackRoutine;
        }
        return true;
    }

    internal void StopAnticipation(Image targetImage)
    {
        if (targetImage == null)
        {
            return;
        }

        if (activeAnticipations.TryGetValue(
                targetImage,
                out Coroutine playbackRoutine) &&
            playbackRoutine != null)
        {
            StopCoroutine(playbackRoutine);
        }

        activeAnticipations.Remove(targetImage);
        SetAlpha(targetImage, 0f);
        targetImage.gameObject.SetActive(false);
    }

    internal void StopAllAnticipations()
    {
        if (activeAnticipations.Count == 0)
        {
            return;
        }

        var targets = new List<Image>(activeAnticipations.Keys);
        var routines = new List<Coroutine>(activeAnticipations.Values);
        activeAnticipations.Clear();

        for (int index = 0; index < routines.Count; index++)
        {
            if (routines[index] != null)
            {
                StopCoroutine(routines[index]);
            }
        }

        for (int index = 0; index < targets.Count; index++)
        {
            Image targetImage = targets[index];
            SetAlpha(targetImage, 0f);
            if (targetImage != null)
            {
                targetImage.gameObject.SetActive(false);
            }
        }
    }

    internal bool PlayUltraEntryTransition(
        Action onMidpoint,
        Action onComplete)
    {
        StopUltraEntryTransition();

        if (ultraEntryTransitionImage == null ||
            ultraEntryTransitionFrames == null ||
            ultraEntryTransitionFrames.Length == 0)
        {
            Debug.LogError(
                "[SlotSymbolAnimationManager] Assign the Ultra Entry " +
                "Transition Image and its ordered sprite frames.",
                this);
            return false;
        }

        ultraEntryTransitionCoroutine = StartCoroutine(
            PlayUltraEntryTransitionFrames(
                onMidpoint,
                onComplete));
        return true;
    }

    internal void StopUltraEntryTransition()
    {
        if (ultraEntryTransitionCoroutine != null)
        {
            StopCoroutine(ultraEntryTransitionCoroutine);
            ultraEntryTransitionCoroutine = null;
        }

        HideUltraWheelBonusTextImmediate();
        HideUltraEntryTransitionImage();
    }

    internal bool PlayUltraWinningSymbolAnimations(
        UltraSlotView ultraSlotView,
        Action onComplete = null)
    {
        StopUltraWinningSymbolAnimations();

        if (ultraSlotView == null ||
            !ultraSlotView.TryGetWinningSymbolAnimationTargets(
                out List<UltraWinningSymbolAnimationTarget> requestedTargets))
        {
            return false;
        }

        int maximumFrameCount = 0;
        var missingFrameSymbols = new HashSet<int>();
        for (int targetIndex = 0;
             targetIndex < requestedTargets.Count;
             targetIndex++)
        {
            UltraWinningSymbolAnimationTarget target =
                requestedTargets[targetIndex];
            if (target == null ||
                target.BaseImage == null ||
                target.AnimationImage == null)
            {
                continue;
            }

            if (!TryGetUltraWinningSymbolFrames(
                    target.SymbolId,
                    out Sprite[] frames))
            {
                missingFrameSymbols.Add(target.SymbolId);
                continue;
            }

            target.Frames = frames;
            activeUltraWinningTargets.Add(target);
            maximumFrameCount =
                Mathf.Max(maximumFrameCount, frames.Length);
        }

        foreach (int missingSymbolId in missingFrameSymbols)
        {
            Debug.LogWarning(
                "[SlotSymbolAnimationManager] Assign winning animation " +
                $"frames for the {GetUltraWheelColorName(missingSymbolId)} " +
                $"Ultra wheel symbol ({missingSymbolId}).",
                this);
        }

        if (activeUltraWinningTargets.Count == 0 ||
            maximumFrameCount <= 0)
        {
            activeUltraWinningTargets.Clear();
            return false;
        }

        ultraWinningSymbolCoroutine = StartCoroutine(
            PlayUltraWinningSymbolFrames(
                maximumFrameCount,
                onComplete));
        return true;
    }

    internal void StopUltraWinningSymbolAnimations()
    {
        if (ultraWinningSymbolCoroutine != null)
        {
            StopCoroutine(ultraWinningSymbolCoroutine);
            ultraWinningSymbolCoroutine = null;
        }

        RestoreUltraWinningSymbolTargets();
    }

    internal bool PlayUltraWheelStopResultAnimation(
        int wheelNumber,
        Action onComplete = null)
    {
        Image resultImage;
        Sprite[] resultFrames;

        switch (wheelNumber)
        {
            case UltraSlotView.BlueWheelSymbolId:
                resultImage = blueUltraWheelStopResultImage;
                resultFrames = blueUltraWheelStopResultFrames;
                break;
            case UltraSlotView.RedWheelSymbolId:
                resultImage = redUltraWheelStopResultImage;
                resultFrames = redUltraWheelStopResultFrames;
                break;
            default:
                return false;
        }

        StopUltraWheelStopResultAnimation(wheelNumber);
        if (resultImage == null ||
            resultFrames == null ||
            resultFrames.Length == 0)
        {
            Debug.LogWarning(
                "[SlotSymbolAnimationManager] Assign the " +
                $"{GetUltraWheelColorName(wheelNumber)} Ultra wheel " +
                "Stop Result Image and ordered frames.",
                this);
            return false;
        }

        Coroutine resultCoroutine = StartCoroutine(
            PlayUltraWheelStopResultFrames(
                wheelNumber,
                resultImage,
                resultFrames,
                onComplete));

        if (wheelNumber == UltraSlotView.BlueWheelSymbolId)
        {
            blueUltraWheelStopResultCoroutine = resultCoroutine;
        }
        else
        {
            redUltraWheelStopResultCoroutine = resultCoroutine;
        }

        return true;
    }

    internal void StopUltraWheelStopResultAnimations()
    {
        StopUltraWheelStopResultAnimation(
            UltraSlotView.BlueWheelSymbolId);
        StopUltraWheelStopResultAnimation(
            UltraSlotView.RedWheelSymbolId);
    }

    private bool TryGetFrames(int symbolId, out Sprite[] frames)
    {
        switch (symbolId)
        {
            case StPatricksGoldSymbolIds.Ace:
                frames = aceFrames;
                break;
            case StPatricksGoldSymbolIds.King:
                frames = kingFrames;
                break;
            case StPatricksGoldSymbolIds.Queen:
                frames = queenFrames;
                break;
            case StPatricksGoldSymbolIds.Jack:
                frames = jackFrames;
                break;
            case StPatricksGoldSymbolIds.Ten:
                frames = tenFrames;
                break;
            case StPatricksGoldSymbolIds.BeerGlass:
                frames = beerGlassFrames;
                break;
            case StPatricksGoldSymbolIds.GreenHat:
                frames = greenHatFrames;
                break;
            case StPatricksGoldSymbolIds.Magnet:
                frames = magnetFrames;
                break;
            case StPatricksGoldSymbolIds.Cigar:
                frames = cigarFrames;
                break;
            case StPatricksGoldSymbolIds.Wild:
                frames = wildFrames;
                break;
            case StPatricksGoldSymbolIds.ScatterWheel:
                frames = scatterWheelMainFrames;
                break;
            case StPatricksGoldSymbolIds.UltraWheel:
                frames = ultraWheelFrames;
                break;
            case StPatricksGoldSymbolIds.TempleRiches:
                frames = templeRichesFrames;
                break;
            default:
                frames = null;
                return false;
        }

        return frames != null && frames.Length > 0;
    }

    private bool TryGetUltraWinningSymbolFrames(
        int symbolId,
        out Sprite[] frames)
    {
        switch (symbolId)
        {
            case UltraSlotView.GreenWheelSymbolId:
                frames = greenUltraWinningSymbolFrames;
                break;
            case UltraSlotView.BlueWheelSymbolId:
                frames = blueUltraWinningSymbolFrames;
                break;
            case UltraSlotView.RedWheelSymbolId:
                frames = redUltraWinningSymbolFrames;
                break;
            default:
                frames = null;
                return false;
        }

        return frames != null && frames.Length > 0;
    }

    private IEnumerator PlayUltraEntryTransitionFrames(
        Action onMidpoint,
        Action onComplete)
    {
        ultraEntryTransitionImage.gameObject.SetActive(true);
        SetAlpha(ultraEntryTransitionImage, 1f);
        ShowUltraWheelBonusText();

        float frameDuration =
            Mathf.Max(0.001f, ultraEntryTransitionFrameDuration);
        int midpointFrameIndex = Mathf.Clamp(
            ultraEntryTransitionFrames.Length / 2,
            1,
            ultraEntryTransitionFrames.Length);
        bool midpointInvoked = false;

        for (int frameIndex = 0;
             frameIndex < ultraEntryTransitionFrames.Length;
             frameIndex++)
        {
            if (!midpointInvoked &&
                frameIndex >= midpointFrameIndex)
            {
                midpointInvoked = true;
                onMidpoint?.Invoke();
            }

            Sprite frame = ultraEntryTransitionFrames[frameIndex];
            if (frame != null)
            {
                ultraEntryTransitionImage.sprite = frame;
            }

            // Waiting after every frame keeps the last frame visible for its
            // complete duration instead of cutting the transition short.
            yield return new WaitForSecondsRealtime(frameDuration);
        }

        if (!midpointInvoked)
        {
            onMidpoint?.Invoke();
        }

        HideUltraEntryTransitionImage();
        yield return HideUltraWheelBonusTextAnimated();

        ultraEntryTransitionCoroutine = null;
        onComplete?.Invoke();
    }

    private IEnumerator PlayUltraWinningSymbolFrames(
        int maximumFrameCount,
        Action onComplete)
    {
        float frameDuration =
            Mathf.Max(0.001f, ultraWinningSymbolFrameDuration);

        for (int targetIndex = 0;
             targetIndex < activeUltraWinningTargets.Count;
             targetIndex++)
        {
            UltraWinningSymbolAnimationTarget target =
                activeUltraWinningTargets[targetIndex];
            AlignOverlayWithoutResizing(target.AnimationImage);
            target.AnimationImage.gameObject.SetActive(true);
            SetAlpha(target.BaseImage, 0f);
            SetAlpha(target.AnimationImage, 1f);
        }

        int loopsToPlay =
            Mathf.Max(1, ultraWinningSymbolLoopCount);
        for (int loopIndex = 0;
             loopIndex < loopsToPlay;
             loopIndex++)
        {
            for (int frameIndex = 0;
                 frameIndex < maximumFrameCount;
                 frameIndex++)
            {
                for (int targetIndex = 0;
                     targetIndex < activeUltraWinningTargets.Count;
                     targetIndex++)
                {
                    UltraWinningSymbolAnimationTarget target =
                        activeUltraWinningTargets[targetIndex];
                    int targetFrameIndex =
                        Mathf.Min(frameIndex, target.Frames.Length - 1);
                    Sprite frame = target.Frames[targetFrameIndex];
                    if (frame != null)
                    {
                        target.AnimationImage.sprite = frame;
                    }
                }

                yield return new WaitForSecondsRealtime(frameDuration);
            }
        }

        ultraWinningSymbolCoroutine = null;
        RestoreUltraWinningSymbolTargets();
        onComplete?.Invoke();
    }

    private IEnumerator PlayUltraWheelStopResultFrames(
        int wheelNumber,
        Image resultImage,
        Sprite[] resultFrames,
        Action onComplete)
    {
        resultImage.gameObject.SetActive(true);
        SetAlpha(resultImage, 1f);

        float frameDuration = Mathf.Max(
            0.001f,
            ultraWheelStopResultFrameDuration);
        for (int frameIndex = 0;
             frameIndex < resultFrames.Length;
             frameIndex++)
        {
            if (resultImage == null)
            {
                break;
            }

            Sprite frame = resultFrames[frameIndex];
            if (frame != null)
            {
                resultImage.sprite = frame;
            }

            yield return new WaitForSecondsRealtime(frameDuration);
        }

        HideUltraWheelStopResultImage(resultImage);
        SetUltraWheelStopResultCoroutine(wheelNumber, null);
        onComplete?.Invoke();
    }

    private void StopUltraWheelStopResultAnimation(int wheelNumber)
    {
        Coroutine resultCoroutine =
            wheelNumber == UltraSlotView.BlueWheelSymbolId
                ? blueUltraWheelStopResultCoroutine
                : redUltraWheelStopResultCoroutine;
        if (resultCoroutine != null)
        {
            StopCoroutine(resultCoroutine);
        }

        if (wheelNumber == UltraSlotView.BlueWheelSymbolId)
        {
            blueUltraWheelStopResultCoroutine = null;
            HideUltraWheelStopResultImage(
                blueUltraWheelStopResultImage);
        }
        else if (wheelNumber == UltraSlotView.RedWheelSymbolId)
        {
            redUltraWheelStopResultCoroutine = null;
            HideUltraWheelStopResultImage(
                redUltraWheelStopResultImage);
        }
    }

    private void SetUltraWheelStopResultCoroutine(
        int wheelNumber,
        Coroutine resultCoroutine)
    {
        if (wheelNumber == UltraSlotView.BlueWheelSymbolId)
        {
            blueUltraWheelStopResultCoroutine = resultCoroutine;
        }
        else if (wheelNumber == UltraSlotView.RedWheelSymbolId)
        {
            redUltraWheelStopResultCoroutine = resultCoroutine;
        }
    }

    private static void HideUltraWheelStopResultImage(Image resultImage)
    {
        SetAlpha(resultImage, 0f);
        if (resultImage != null)
        {
            resultImage.gameObject.SetActive(false);
        }
    }

    private void RestoreUltraWinningSymbolTargets()
    {
        for (int targetIndex = 0;
             targetIndex < activeUltraWinningTargets.Count;
             targetIndex++)
        {
            UltraWinningSymbolAnimationTarget target =
                activeUltraWinningTargets[targetIndex];
            if (target == null)
            {
                continue;
            }

            SetAlpha(target.BaseImage, 1f);
            SetAlpha(target.AnimationImage, 0f);
            SetAlpha(target.WinIndicatorImage, 0f);
            if (target.AnimationImage != null)
            {
                target.AnimationImage.gameObject.SetActive(false);
            }
            if (target.WinIndicatorImage != null)
            {
                target.WinIndicatorImage.gameObject.SetActive(false);
            }
        }

        activeUltraWinningTargets.Clear();
    }

    private void HideUltraEntryTransitionImage()
    {
        SetAlpha(ultraEntryTransitionImage, 0f);
        if (ultraEntryTransitionImage != null)
        {
            ultraEntryTransitionImage.gameObject.SetActive(false);
        }
    }

    private void ShowUltraWheelBonusText()
    {
        if (ultraWheelBonusText == null)
        {
            return;
        }

        StopUltraWheelBonusTextScaleAnimation();
        ultraWheelBonusText.gameObject.SetActive(true);
        ultraWheelBonusText.localScale = Vector3.zero;
        ultraWheelBonusTextScaleCoroutine = StartCoroutine(
            AnimateUltraWheelBonusTextScale(
                Vector3.zero,
                ultraWheelBonusTextOriginalScale));
    }

    private IEnumerator HideUltraWheelBonusTextAnimated()
    {
        if (ultraWheelBonusText == null)
        {
            yield break;
        }

        StopUltraWheelBonusTextScaleAnimation();
        ultraWheelBonusTextScaleCoroutine = StartCoroutine(
            AnimateUltraWheelBonusTextScale(
                ultraWheelBonusText.localScale,
                Vector3.zero));
        yield return ultraWheelBonusTextScaleCoroutine;

        if (ultraWheelBonusText != null)
        {
            ultraWheelBonusText.localScale = Vector3.zero;
            ultraWheelBonusText.gameObject.SetActive(false);
        }
    }

    private IEnumerator AnimateUltraWheelBonusTextScale(
        Vector3 startScale,
        Vector3 endScale)
    {
        float duration = Mathf.Max(
            0.01f,
            ultraWheelBonusTextScaleDuration);
        float elapsed = 0f;

        while (elapsed < duration && ultraWheelBonusText != null)
        {
            elapsed += Time.unscaledDeltaTime;
            float progress = Mathf.Clamp01(elapsed / duration);
            float easedProgress = Mathf.SmoothStep(0f, 1f, progress);
            ultraWheelBonusText.localScale = Vector3.LerpUnclamped(
                startScale,
                endScale,
                easedProgress);
            yield return null;
        }

        if (ultraWheelBonusText != null)
        {
            ultraWheelBonusText.localScale = endScale;
        }

        ultraWheelBonusTextScaleCoroutine = null;
    }

    private void HideUltraWheelBonusTextImmediate()
    {
        StopUltraWheelBonusTextScaleAnimation();
        if (ultraWheelBonusText == null)
        {
            return;
        }

        ultraWheelBonusText.localScale = Vector3.zero;
        ultraWheelBonusText.gameObject.SetActive(false);
    }

    private void StopUltraWheelBonusTextScaleAnimation()
    {
        if (ultraWheelBonusTextScaleCoroutine == null)
        {
            return;
        }

        StopCoroutine(ultraWheelBonusTextScaleCoroutine);
        ultraWheelBonusTextScaleCoroutine = null;
    }

    private static string GetUltraWheelColorName(int symbolId)
    {
        switch (symbolId)
        {
            case UltraSlotView.GreenWheelSymbolId:
                return "Green";
            case UltraSlotView.BlueWheelSymbolId:
                return "Blue";
            case UltraSlotView.RedWheelSymbolId:
                return "Red";
            default:
                return "Unknown";
        }
    }

    private IEnumerator PlayAnticipationFrames(Image targetImage)
    {
        float loopDuration = Mathf.Max(0.05f, anticipationLoopDuration);
        while (targetImage != null &&
               activeAnticipations.ContainsKey(targetImage))
        {
            float elapsed = 0f;
            int displayedFrameIndex = -1;
            while (elapsed < loopDuration &&
                   targetImage != null &&
                   activeAnticipations.ContainsKey(targetImage))
            {
                float normalizedTime = elapsed / loopDuration;
                int frameIndex = Mathf.Min(
                    Mathf.FloorToInt(normalizedTime * anticipationFrames.Length),
                    anticipationFrames.Length - 1);

                if (frameIndex != displayedFrameIndex)
                {
                    Sprite frame = anticipationFrames[frameIndex];
                    if (frame != null)
                    {
                        targetImage.sprite = frame;
                    }

                    displayedFrameIndex = frameIndex;
                }

                elapsed += GetDeltaTime();
                yield return null;
            }
        }
    }

    private IEnumerator PlayFrames(
        ActiveSymbolAnimation activeAnimation,
        Sprite[] frames,
        float synchronizedLoopDuration)
    {
        if (startDelay > 0f)
        {
            float delayElapsed = 0f;
            while (delayElapsed < startDelay)
            {
                delayElapsed += GetDeltaTime();
                yield return null;
            }
        }

        Image overlayImage = activeAnimation.OverlayImage;
        if (overlayImage == null ||
            !activeAnimations.TryGetValue(overlayImage, out ActiveSymbolAnimation current) ||
            current != activeAnimation)
        {
            yield break;
        }

        SetAlpha(activeAnimation.BaseImage, 0f);
        SetAlpha(overlayImage, 1f);

        int completedLoops = 0;
        while (loopCount == 0 || completedLoops < loopCount)
        {
            float loopElapsed = 0f;
            int displayedFrameIndex = -1;
            while (loopElapsed < synchronizedLoopDuration)
            {
                float normalizedTime = loopElapsed / synchronizedLoopDuration;
                int frameIndex = Mathf.Min(
                    Mathf.FloorToInt(normalizedTime * frames.Length),
                    frames.Length - 1);

                if (frameIndex != displayedFrameIndex)
                {
                    Sprite frame = frames[frameIndex];
                    if (frame != null)
                    {
                        overlayImage.sprite = frame;
                        activeAnimation.CurrentVisualSize01 =
                            GetNormalizedVisualSize(
                                frame,
                                activeAnimation.MinimumVisualExtent,
                                activeAnimation.MaximumVisualExtent);
                    }

                    displayedFrameIndex = frameIndex;
                }

                loopElapsed += GetDeltaTime();
                yield return null;
            }

            completedLoops++;
            if ((loopCount == 0 || completedLoops < loopCount) &&
                delayBetweenLoops > 0f)
            {
                float loopDelayElapsed = 0f;
                while (loopDelayElapsed < delayBetweenLoops)
                {
                    loopDelayElapsed += GetDeltaTime();
                    yield return null;
                }
            }
        }

        if (overlayImage != null &&
            activeAnimations.TryGetValue(overlayImage, out current) &&
            current == activeAnimation)
        {
            activeAnimations.Remove(overlayImage);
            RestoreAnimationLayers(activeAnimation);
        }
    }

    private IEnumerator PlayFramesOnce(
        ActiveSymbolAnimation activeAnimation,
        Sprite[] frames,
        int frameCount,
        float playbackDuration,
        Action onComplete)
    {
        if (startDelay > 0f)
        {
            float delayElapsed = 0f;
            while (delayElapsed < startDelay)
            {
                delayElapsed += GetDeltaTime();
                yield return null;
            }
        }

        Image overlayImage = activeAnimation.OverlayImage;
        if (overlayImage == null ||
            !activeAnimations.TryGetValue(
                overlayImage,
                out ActiveSymbolAnimation current) ||
            current != activeAnimation)
        {
            yield break;
        }

        SetAlpha(activeAnimation.BaseImage, 0f);
        SetAlpha(overlayImage, 1f);
        PrepareSecondaryAnimationLayer(activeAnimation);

        float elapsed = 0f;
        int displayedFrameIndex = -1;
        int displayedSecondaryFrameIndex = -1;
        while (elapsed < playbackDuration)
        {
            float normalizedTime = elapsed / playbackDuration;
            int frameIndex = Mathf.Min(
                Mathf.FloorToInt(normalizedTime * frameCount),
                frameCount - 1);

            if (frameIndex != displayedFrameIndex)
            {
                Sprite frame = frames[frameIndex];
                if (frame != null)
                {
                    overlayImage.sprite = frame;
                    activeAnimation.CurrentVisualSize01 =
                        GetNormalizedVisualSize(
                            frame,
                            activeAnimation.MinimumVisualExtent,
                            activeAnimation.MaximumVisualExtent);
                }

                displayedFrameIndex = frameIndex;
            }

            if (activeAnimation.SecondaryOverlayImage != null &&
                activeAnimation.SecondaryFrames != null &&
                activeAnimation.SecondaryFrames.Length > 0)
            {
                int secondaryFrameIndex = Mathf.Min(
                    Mathf.FloorToInt(
                        normalizedTime *
                        activeAnimation.SecondaryFrames.Length),
                    activeAnimation.SecondaryFrames.Length - 1);
                if (secondaryFrameIndex !=
                    displayedSecondaryFrameIndex)
                {
                    Sprite secondaryFrame =
                        activeAnimation.SecondaryFrames[
                            secondaryFrameIndex];
                    if (secondaryFrame != null)
                    {
                        activeAnimation.SecondaryOverlayImage.sprite =
                            secondaryFrame;
                    }

                    displayedSecondaryFrameIndex =
                        secondaryFrameIndex;
                }
            }

            elapsed += GetDeltaTime();
            yield return null;
        }

        if (overlayImage == null ||
            !activeAnimations.TryGetValue(
                overlayImage,
                out current) ||
            current != activeAnimation)
        {
            yield break;
        }

        activeAnimations.Remove(overlayImage);
        RestoreAnimationLayers(activeAnimation);
        onComplete?.Invoke();
    }

    private IEnumerator PlayScatterWheelResultTextMoveFrames(
        RectTransform resultText,
        float targetAnchoredY,
        Action onComplete)
    {
        Vector2 startPosition = resultText.anchoredPosition;
        float duration =
            Mathf.Max(0.01f, scatterWheelResultTextMoveDuration);
        float elapsed = 0f;

        while (elapsed < duration && resultText != null)
        {
            float normalizedTime =
                Mathf.Clamp01(elapsed / duration);
            float easedTime =
                1f - Mathf.Pow(1f - normalizedTime, 3f);
            Vector2 position = startPosition;
            position.y =
                Mathf.Lerp(
                    startPosition.y,
                    targetAnchoredY,
                    easedTime);
            resultText.anchoredPosition = position;

            elapsed += GetDeltaTime();
            yield return null;
        }

        if (resultText != null)
        {
            Vector2 finalPosition = resultText.anchoredPosition;
            finalPosition.y = targetAnchoredY;
            resultText.anchoredPosition = finalPosition;
            activeScatterWheelResultTextMoves.Remove(resultText);
        }

        onComplete?.Invoke();
    }

    private IEnumerator PlayScatterWheelHandoffScale(
        RectTransform rotatingWheel,
        Vector3 rotatingWheelTargetScale,
        RectTransform wheelRim,
        Vector3 wheelRimTargetScale,
        RectTransform leaf,
        Vector3 leafTargetScale)
    {
        float duration =
            Mathf.Max(0.01f, scatterWheelHandoffScaleDuration);
        float startScale =
            Mathf.Max(0.01f, scatterWheelHandoffStartScale);
        float endScale =
            Mathf.Max(0.01f, scatterWheelHandoffEndScale);
        float elapsed = 0f;

        while (elapsed < duration)
        {
            float normalizedTime =
                Mathf.Clamp01(elapsed / duration);
            float easedTime =
                normalizedTime *
                normalizedTime *
                (3f - 2f * normalizedTime);
            float scaleMultiplier =
                Mathf.Lerp(startScale, endScale, easedTime);

            ApplyScaleMultiplier(
                rotatingWheel,
                rotatingWheelTargetScale,
                scaleMultiplier);
            ApplyScaleMultiplier(
                wheelRim,
                wheelRimTargetScale,
                scaleMultiplier);
            ApplyScaleMultiplier(
                leaf,
                leafTargetScale,
                scaleMultiplier);

            elapsed += GetDeltaTime();
            yield return null;
        }

        ApplyScaleMultiplier(
            rotatingWheel,
            rotatingWheelTargetScale,
            endScale);
        ApplyScaleMultiplier(
            wheelRim,
            wheelRimTargetScale,
            endScale);
        ApplyScaleMultiplier(
            leaf,
            leafTargetScale,
            endScale);
        if (rotatingWheel != null)
        {
            activeScatterWheelHandoffs.Remove(rotatingWheel);
        }
    }

    private IEnumerator PlayScatterWheelIntroFrames(
        ActiveSymbolAnimation activeAnimation,
        Action onMainSequenceComplete,
        Action onBackgroundFxComplete)
    {
        if (startDelay > 0f)
        {
            float delayElapsed = 0f;
            while (delayElapsed < startDelay)
            {
                delayElapsed += GetDeltaTime();
                yield return null;
            }
        }

        Image overlayImage = activeAnimation.OverlayImage;
        if (!IsCurrentAnimation(activeAnimation))
        {
            yield break;
        }

        SetAlpha(activeAnimation.BaseImage, 0f);
        SetAlpha(overlayImage, 1f);
        PrepareSecondaryAnimationLayer(activeAnimation);

        bool hasBackgroundFx =
            activeAnimation.SecondaryOverlayImage != null &&
            activeAnimation.SecondaryFrames != null &&
            activeAnimation.SecondaryFrames.Length > 0;
        float mainDuration = GetScatterWheelIntroDuration();
        float backgroundFxDuration =
            hasBackgroundFx
                ? mainDuration /
                  Mathf.Max(
                      0.01f,
                      scatterWheelBackgroundFxSpeedMultiplier)
                : mainDuration;
        float totalDuration =
            Mathf.Max(mainDuration, backgroundFxDuration);
        float elapsed = 0f;
        int displayedMainFrameIndex = -1;
        int displayedBackgroundFrameIndex = -1;
        bool mainSequenceCompleted = false;

        while (elapsed < totalDuration)
        {
            if (!mainSequenceCompleted)
            {
                float mainNormalizedTime =
                    Mathf.Clamp01(elapsed / mainDuration);
                int mainFrameIndex = Mathf.Min(
                    Mathf.FloorToInt(
                        mainNormalizedTime *
                        scatterWheelMainFrames.Length),
                    scatterWheelMainFrames.Length - 1);
                if (mainFrameIndex != displayedMainFrameIndex)
                {
                    Sprite frame =
                        scatterWheelMainFrames[mainFrameIndex];
                    if (frame != null)
                    {
                        overlayImage.sprite = frame;
                        activeAnimation.CurrentVisualSize01 =
                            GetNormalizedVisualSize(
                                frame,
                                activeAnimation.MinimumVisualExtent,
                                activeAnimation.MaximumVisualExtent);
                    }

                    displayedMainFrameIndex = mainFrameIndex;
                }

                if (elapsed >= mainDuration)
                {
                    mainSequenceCompleted = true;
                    RestorePrimaryAnimationLayer(activeAnimation);
                    onMainSequenceComplete?.Invoke();

                    if (!IsCurrentAnimation(activeAnimation))
                    {
                        yield break;
                    }
                }
            }

            if (hasBackgroundFx)
            {
                float backgroundNormalizedTime =
                    Mathf.Clamp01(elapsed / backgroundFxDuration);
                int backgroundFrameIndex = Mathf.Min(
                    Mathf.FloorToInt(
                        backgroundNormalizedTime *
                        activeAnimation.SecondaryFrames.Length),
                    activeAnimation.SecondaryFrames.Length - 1);
                if (backgroundFrameIndex !=
                    displayedBackgroundFrameIndex)
                {
                    Sprite backgroundFrame =
                        activeAnimation.SecondaryFrames[
                            backgroundFrameIndex];
                    if (backgroundFrame != null)
                    {
                        activeAnimation.SecondaryOverlayImage.sprite =
                            backgroundFrame;
                    }

                    displayedBackgroundFrameIndex =
                        backgroundFrameIndex;
                }
            }

            elapsed += GetDeltaTime();
            yield return null;
        }

        if (!mainSequenceCompleted)
        {
            RestorePrimaryAnimationLayer(activeAnimation);
            onMainSequenceComplete?.Invoke();
            if (!IsCurrentAnimation(activeAnimation))
            {
                yield break;
            }
        }

        if (!IsCurrentAnimation(activeAnimation))
        {
            yield break;
        }

        if (!hasBackgroundFx)
        {
            activeAnimation.PlaybackRoutine = null;
            activeAnimations.Remove(overlayImage);
            RestoreAnimationLayers(activeAnimation);
            onBackgroundFxComplete?.Invoke();
            yield break;
        }

        Image backgroundFxImage = activeAnimation.SecondaryOverlayImage;
        float startingAlpha = backgroundFxImage.color.a;
        float fadeDuration = Mathf.Max(
            0.01f,
            scatterWheelBackgroundFxFadeDuration);
        float fadeElapsed = 0f;
        while (fadeElapsed < fadeDuration)
        {
            if (!IsCurrentAnimation(activeAnimation))
            {
                yield break;
            }

            fadeElapsed += GetDeltaTime();
            SetAlpha(
                backgroundFxImage,
                Mathf.Lerp(
                    startingAlpha,
                    0f,
                    Mathf.Clamp01(fadeElapsed / fadeDuration)));
            yield return null;
        }

        if (!IsCurrentAnimation(activeAnimation))
        {
            yield break;
        }

        activeAnimation.PlaybackRoutine = null;
        activeAnimations.Remove(overlayImage);
        RestoreAnimationLayers(activeAnimation);
        onBackgroundFxComplete?.Invoke();
    }

    private bool IsCurrentAnimation(
        ActiveSymbolAnimation activeAnimation)
    {
        Image overlayImage = activeAnimation?.OverlayImage;
        return overlayImage != null &&
               activeAnimations.TryGetValue(
                   overlayImage,
                   out ActiveSymbolAnimation current) &&
               current == activeAnimation;
    }

    private static void PrepareSecondaryAnimationLayer(
        ActiveSymbolAnimation activeAnimation)
    {
        Image secondaryImage =
            activeAnimation.SecondaryOverlayImage;
        if (secondaryImage == null ||
            activeAnimation.SecondaryFrames == null ||
            activeAnimation.SecondaryFrames.Length == 0)
        {
            return;
        }

        Transform secondaryTransform = secondaryImage.transform;
        Transform mainTransform =
            activeAnimation.OverlayImage != null
                ? activeAnimation.OverlayImage.transform
                : null;
        if (mainTransform != null &&
            secondaryTransform.parent == mainTransform.parent)
        {
            // The FX track is a background, so render it immediately behind
            // the main Scatter Wheel animation layer.
            secondaryTransform.SetSiblingIndex(
                mainTransform.GetSiblingIndex());
        }

        secondaryImage.gameObject.SetActive(true);
        SetAlpha(secondaryImage, 1f);
    }

    private static void RestorePrimaryAnimationLayer(
        ActiveSymbolAnimation activeAnimation)
    {
        if (activeAnimation == null)
        {
            return;
        }

        SetAlpha(activeAnimation.BaseImage, 1f);
        SetAlpha(activeAnimation.OverlayImage, 0f);
    }

    private static void RestoreAnimationLayers(
        ActiveSymbolAnimation activeAnimation)
    {
        if (activeAnimation == null)
        {
            return;
        }

        SetAlpha(activeAnimation.BaseImage, 1f);
        SetAlpha(activeAnimation.OverlayImage, 0f);

        Image secondaryImage =
            activeAnimation.SecondaryOverlayImage;
        SetAlpha(secondaryImage, 0f);
        if (secondaryImage == null)
        {
            return;
        }

        if (activeAnimation.SecondaryOriginalSiblingIndex >= 0)
        {
            secondaryImage.transform.SetSiblingIndex(
                activeAnimation.SecondaryOriginalSiblingIndex);
        }
        secondaryImage.gameObject.SetActive(false);
    }

    private static void SetActive(
        RectTransform target,
        bool isActive)
    {
        if (target != null)
        {
            target.gameObject.SetActive(isActive);
        }
    }

    private static void ApplyScaleMultiplier(
        RectTransform target,
        Vector3 targetScale,
        float multiplier)
    {
        if (target != null)
        {
            target.localScale = targetScale * multiplier;
        }
    }

    private float GetLoopDuration(Sprite[] frames, float synchronizedLoopDuration)
    {
        if (synchronizedLoopDuration > 0f)
        {
            return synchronizedLoopDuration;
        }

        return Mathf.Max(0.001f, frameDuration) * frames.Length;
    }

    private float GetDeltaTime()
    {
        return useUnscaledTime ? Time.unscaledDeltaTime : Time.deltaTime;
    }

    private static void GetVisualExtentRange(
        Sprite[] frames,
        out float minimumExtent,
        out float maximumExtent)
    {
        minimumExtent = float.MaxValue;
        maximumExtent = 0f;

        for (int frameIndex = 0; frameIndex < frames.Length; frameIndex++)
        {
            float extent = GetSpriteVisualExtent(frames[frameIndex]);
            if (extent <= 0f)
            {
                continue;
            }

            minimumExtent = Mathf.Min(minimumExtent, extent);
            maximumExtent = Mathf.Max(maximumExtent, extent);
        }

        if (minimumExtent == float.MaxValue)
        {
            minimumExtent = 0f;
        }
    }

    private static float GetNormalizedVisualSize(
        Sprite frame,
        float minimumExtent,
        float maximumExtent)
    {
        float extent = GetSpriteVisualExtent(frame);
        if (extent <= 0f ||
            maximumExtent - minimumExtent <= Mathf.Epsilon)
        {
            return 0f;
        }

        return Mathf.InverseLerp(
            minimumExtent,
            maximumExtent,
            extent);
    }

    private static float GetSpriteVisualExtent(Sprite sprite)
    {
        if (sprite == null)
        {
            return 0f;
        }

        Vector2[] vertices = sprite.vertices;
        if (vertices == null || vertices.Length == 0)
        {
            return Mathf.Max(sprite.bounds.size.x, sprite.bounds.size.y);
        }

        Vector2 minimum = vertices[0];
        Vector2 maximum = vertices[0];
        for (int vertexIndex = 1; vertexIndex < vertices.Length; vertexIndex++)
        {
            minimum = Vector2.Min(minimum, vertices[vertexIndex]);
            maximum = Vector2.Max(maximum, vertices[vertexIndex]);
        }

        Vector2 size = maximum - minimum;
        return Mathf.Max(size.x, size.y);
    }

    private static void AlignOverlayWithoutResizing(Image overlayImage)
    {
        RectTransform overlayRect = overlayImage.rectTransform;

        overlayRect.anchoredPosition = Vector2.zero;
        overlayRect.localRotation = Quaternion.identity;
    }

    private static void SetAlpha(Image image, float alpha)
    {
        if (image == null)
        {
            return;
        }

        Color color = image.color;
        image.color = new Color(color.r, color.g, color.b, alpha);
    }
}

internal sealed class UltraWinningSymbolAnimationTarget
{
    internal int SymbolId;
    internal Image BaseImage;
    internal Image AnimationImage;
    internal Image WinIndicatorImage;
    internal Sprite[] Frames;
}

using System.Collections;
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
    [SerializeField] private Sprite[] scatterWheelFrames = new Sprite[0];
    [SerializeField] private Sprite[] ultraWheelFrames = new Sprite[0];
    [SerializeField] private Sprite[] templeRichesFrames = new Sprite[0];

    [Header("Playback")]
    [SerializeField, Min(0.001f)] private float frameDuration = 1f / 30f;
    [SerializeField, Min(0f)] private float startDelay;
    [Tooltip("Zero loops continuously until the winning-symbol display is stopped.")]
    [SerializeField, Min(0)] private int loopCount;
    [SerializeField, Min(0f)] private float delayBetweenLoops;
    [SerializeField] private bool useUnscaledTime;

    private readonly Dictionary<Image, ActiveSymbolAnimation> activeAnimations =
        new Dictionary<Image, ActiveSymbolAnimation>();

    private sealed class ActiveSymbolAnimation
    {
        public Image BaseImage;
        public Image OverlayImage;
        public Coroutine PlaybackRoutine;
    }

    private void OnDisable()
    {
        StopAllAnimations();
    }

    private void OnDestroy()
    {
        StopAllAnimations();
    }

    public bool PlayAnimation(
        int symbolId,
        Image baseImage,
        Image overlayImage,
        float synchronizedLoopDuration = 0f)
    {
        if (baseImage == null || overlayImage == null)
        {
            return false;
        }

        MatchOverlayToBaseImage(baseImage, overlayImage);

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

        ActiveSymbolAnimation activeAnimation = new ActiveSymbolAnimation
        {
            BaseImage = baseImage,
            OverlayImage = overlayImage
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

    public void StopAnimation(Image baseImage, Image overlayImage)
    {
        if (overlayImage != null &&
            activeAnimations.TryGetValue(overlayImage, out ActiveSymbolAnimation activeAnimation))
        {
            if (activeAnimation.PlaybackRoutine != null)
            {
                StopCoroutine(activeAnimation.PlaybackRoutine);
            }

            activeAnimations.Remove(overlayImage);
        }

        SetAlpha(baseImage, 1f);
        SetAlpha(overlayImage, 0f);
    }

    public void StopAllAnimations()
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

            SetAlpha(animation.BaseImage, 1f);
            SetAlpha(animation.OverlayImage, 0f);
        }
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
                frames = scatterWheelFrames;
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
            SetAlpha(activeAnimation.BaseImage, 1f);
            SetAlpha(overlayImage, 0f);
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

    private static void MatchOverlayToBaseImage(Image baseImage, Image overlayImage)
    {
        RectTransform baseRect = baseImage.rectTransform;
        RectTransform overlayRect = overlayImage.rectTransform;

        overlayRect.SetSizeWithCurrentAnchors(
            RectTransform.Axis.Horizontal,
            baseRect.rect.width);
        overlayRect.SetSizeWithCurrentAnchors(
            RectTransform.Axis.Vertical,
            baseRect.rect.height);
        overlayRect.anchoredPosition = Vector2.zero;
        overlayRect.localRotation = Quaternion.identity;
        overlayRect.localScale = Vector3.one;
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

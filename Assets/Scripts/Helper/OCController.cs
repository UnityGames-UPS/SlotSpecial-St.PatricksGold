using System.Collections.Generic;
using DG.Tweening;
using UnityEngine;
using UnityEngine.UI;

public sealed class OCController : MonoBehaviour
{
    [Header("Required References")]
    [SerializeField] private OrientationChange orientationChange;
    [SerializeField] private CanvasScaler canvasScaler;
    [SerializeField] private Transform slotObject;
    [SerializeField] private List<RectTransform> resizedObjects =
        new List<RectTransform>();

    [Header("Panel Roots")]
    [SerializeField] private GameObject landscapePanelObject;
    [SerializeField] private GameObject portraitPanelObject;

    [Header("Backgrounds")]
    [SerializeField] private GameObject landscapeBackground;
    [SerializeField] private GameObject portraitBackground;

    [Header("Canvas Scaler Resolutions")]
    [SerializeField] private Vector2 landscapeReferenceResolution =
        new Vector2(1920f, 1080f);
    [SerializeField] private Vector2 portraitReferenceResolution =
        new Vector2(1080f, 1920f);

    [Header("General RectTransform Sizes")]
    [SerializeField] private Vector2 landscapeResizedObjectSize =
        new Vector2(1920f, 1080f);
    [SerializeField] private Vector2 portraitResizedObjectSize =
        new Vector2(1080f, 1920f);

    [Header("Slot Root Layout")]
    [SerializeField] private Vector3 landscapeSlotScale = Vector3.one;
    [SerializeField] private Vector3 portraitSlotScale =
        new Vector3(0.73f, 0.73f, 0.73f);
    [SerializeField] private Vector3 landscapeSlotPosition = Vector3.zero;
    [SerializeField] private Vector3 portraitSlotPosition =
        new Vector3(0f, -300f, 0f);

    [Header("Game Name Logo")]
    [Tooltip(
        "Assign the Name RectTransform containing the St. Patrick's Gold logo.")]
    [SerializeField] private RectTransform gameNameLogo;
    [Tooltip(
        "Additional local-position offset applied only in Mobile Portrait.")]
    [SerializeField] private Vector2 portraitGameNameLogoOffset =
        new Vector2(0f, 70f);
    [Tooltip(
        "Scale multiplier applied to the game name logo in Mobile Portrait.")]
    [SerializeField, Min(0f)]
    private float portraitGameNameLogoScaleMultiplier = 3f;

    [Header("Shared Sound Panel")]
    [SerializeField] private RectTransform soundPanel;
    [SerializeField] private float portraitSoundPanelRightOffset = 40f;
    [SerializeField] private float portraitSoundPanelDownOffset = 200f;

    [Header("Shared Page Content")]
    [SerializeField] private RectTransform infoPageScrollObject;
    [SerializeField] private RectTransform guideScrollObject;

    [Header("Animation")]
    [SerializeField, Min(0f)] private float transitionDuration = 0.2f;

    private readonly List<Tween> activeTweens = new List<Tween>();
    private bool isSubscribed;
    private Vector3 landscapeGameNameLogoPosition;
    private Vector3 landscapeGameNameLogoScale;
    private Vector2 landscapeSoundPanelAnchoredPosition;

    private void Awake()
    {
        if (gameNameLogo != null)
        {
            landscapeGameNameLogoPosition =
                gameNameLogo.localPosition;
            landscapeGameNameLogoScale =
                gameNameLogo.localScale;
        }

        if (soundPanel != null)
        {
            landscapeSoundPanelAnchoredPosition =
                soundPanel.anchoredPosition;
        }

        ValidateRequiredReferences(true);
    }

    private void OnEnable()
    {
        Subscribe();
    }

    private void Subscribe()
    {
        if (isSubscribed || orientationChange == null)
        {
            return;
        }

        orientationChange.OnOrientationChangedInstance +=
            HandleOrientationChange;
        isSubscribed = true;
    }

    private void Unsubscribe()
    {
        if (!isSubscribed)
        {
            return;
        }

        if (orientationChange != null)
        {
            orientationChange.OnOrientationChangedInstance -=
                HandleOrientationChange;
        }

        isSubscribed = false;
    }

    internal Vector2 ApplyReferenceResolution(
        OrientationChange.OrientationMode mode)
    {
        bool isMobilePortrait =
            mode == OrientationChange.OrientationMode.MobilePortrait;
        Vector2 targetReferenceResolution = isMobilePortrait
            ? portraitReferenceResolution
            : landscapeReferenceResolution;

        if (canvasScaler == null)
        {
            Debug.LogError(
                "[OCController] Cannot install the layout profile because " +
                "MainCanvas CanvasScaler is not assigned.",
                this);
            return targetReferenceResolution;
        }

        canvasScaler.referenceResolution = targetReferenceResolution;
        return targetReferenceResolution;
    }

    private void HandleOrientationChange(
        OrientationChange.OrientationMode mode,
        int width,
        int height)
    {
        if (!ValidateRequiredReferences(true))
        {
            return;
        }

        KillActiveTweens();

        bool isMobilePortrait =
            mode == OrientationChange.OrientationMode.MobilePortrait;

        landscapePanelObject.SetActive(!isMobilePortrait);
        portraitPanelObject.SetActive(isMobilePortrait);
        landscapeBackground.SetActive(!isMobilePortrait);
        portraitBackground.SetActive(isMobilePortrait);

        // OrientationChange already installs this synchronously before its
        // match calculation. Reapplying it here keeps this method complete
        // and idempotent.
        ApplyReferenceResolution(mode);

        Vector2 targetSize = isMobilePortrait
            ? portraitResizedObjectSize
            : landscapeResizedObjectSize;
        foreach (RectTransform rectTransform in resizedObjects)
        {
            TweenSize(rectTransform, targetSize);
        }

        Vector3 targetScale = isMobilePortrait
            ? portraitSlotScale
            : landscapeSlotScale;
        Vector3 targetPosition = isMobilePortrait
            ? portraitSlotPosition
            : landscapeSlotPosition;
        TweenScale(slotObject, targetScale);
        TweenPosition(slotObject, targetPosition);

        Vector3 gameNameLogoPosition =
            landscapeGameNameLogoPosition;
        if (isMobilePortrait)
        {
            gameNameLogoPosition += new Vector3(
                portraitGameNameLogoOffset.x,
                portraitGameNameLogoOffset.y,
                0f);
        }

        TweenPosition(
            gameNameLogo,
            gameNameLogoPosition);

        Vector3 gameNameLogoScale = isMobilePortrait
            ? landscapeGameNameLogoScale *
                portraitGameNameLogoScaleMultiplier
            : landscapeGameNameLogoScale;
        TweenScale(gameNameLogo, gameNameLogoScale);

        if (soundPanel != null)
        {
            Vector2 soundPanelPosition =
                landscapeSoundPanelAnchoredPosition;
            if (isMobilePortrait)
            {
                soundPanelPosition += new Vector2(
                    portraitSoundPanelRightOffset,
                    -portraitSoundPanelDownOffset);
            }

            TweenAnchoredPosition(soundPanel, soundPanelPosition);
        }

        float sharedPageHeight = isMobilePortrait ? 1920f : 1080f;
        TweenHeight(infoPageScrollObject, sharedPageHeight);
        TweenHeight(guideScrollObject, sharedPageHeight);

        Debug.Log(
            $"[OCController] Applied {mode} presentation once for " +
            $"{width}x{height}; landscapeUI={!isMobilePortrait}; " +
            $"portraitUI={isMobilePortrait}; slotScale={targetScale}; " +
            $"slotPosition={targetPosition}.");
    }

    private void TweenSize(RectTransform target, Vector2 size)
    {
        if (transitionDuration <= 0f)
        {
            target.sizeDelta = size;
            return;
        }

        activeTweens.Add(
            target.DOSizeDelta(size, transitionDuration)
                .SetEase(Ease.OutCubic)
                .SetUpdate(true));
    }

    private void TweenHeight(RectTransform target, float height)
    {
        Vector2 targetSize = new Vector2(target.sizeDelta.x, height);
        TweenSize(target, targetSize);
    }

    private void TweenScale(Transform target, Vector3 scale)
    {
        if (transitionDuration <= 0f)
        {
            target.localScale = scale;
            return;
        }

        activeTweens.Add(
            target.DOScale(scale, transitionDuration)
                .SetEase(Ease.OutCubic)
                .SetUpdate(true));
    }

    private void TweenPosition(Transform target, Vector3 position)
    {
        if (transitionDuration <= 0f)
        {
            target.localPosition = position;
            return;
        }

        activeTweens.Add(
            target.DOLocalMove(position, transitionDuration)
                .SetEase(Ease.OutCubic)
                .SetUpdate(true));
    }

    private void TweenAnchoredPosition(
        RectTransform target,
        Vector2 position)
    {
        if (transitionDuration <= 0f)
        {
            target.anchoredPosition = position;
            return;
        }

        activeTweens.Add(
            target.DOAnchorPos(position, transitionDuration)
                .SetEase(Ease.OutCubic)
                .SetUpdate(true));
    }

    private void KillActiveTweens()
    {
        foreach (Tween tween in activeTweens)
        {
            if (tween != null && tween.IsActive())
            {
                tween.Kill(false);
            }
        }

        activeTweens.Clear();
    }

    private bool ValidateRequiredReferences(bool logErrors)
    {
        bool valid = true;
        valid &= ValidateReference(
            orientationChange,
            nameof(orientationChange),
            "Assign the OrientationChange component on OC.",
            logErrors);
        valid &= ValidateReference(
            canvasScaler,
            nameof(canvasScaler),
            "Assign MainCanvas's CanvasScaler.",
            logErrors);
        valid &= ValidateReference(
            slotObject,
            nameof(slotObject),
            "Assign GameplayPresentationRoot, which contains SlotHolder and " +
            "the shared wheel/win presentation objects.",
            logErrors);
        valid &= ValidateReference(
            gameNameLogo,
            nameof(gameNameLogo),
            "Assign the Name RectTransform inside SlotHolder.",
            logErrors);
        valid &= ValidateReference(
            landscapePanelObject,
            nameof(landscapePanelObject),
            "Assign LandscapeUI.",
            logErrors);
        valid &= ValidateReference(
            portraitPanelObject,
            nameof(portraitPanelObject),
            "Assign PortraitUI.",
            logErrors);
        valid &= ValidateReference(
            landscapeBackground,
            nameof(landscapeBackground),
            "Assign LandscapeBg.",
            logErrors);
        valid &= ValidateReference(
            portraitBackground,
            nameof(portraitBackground),
            "Assign PortraitBg.",
            logErrors);
        valid &= ValidateReference(
            infoPageScrollObject,
            nameof(infoPageScrollObject),
            "Assign the inner Infopage RectTransform.",
            logErrors);
        valid &= ValidateReference(
            guideScrollObject,
            nameof(guideScrollObject),
            "Assign the inner GuidePage RectTransform.",
            logErrors);

        if (resizedObjects == null || resizedObjects.Count == 0)
        {
            if (logErrors)
            {
                Debug.LogError(
                    "[OCController] 'resizedObjects' must contain BG and " +
                    "the target project's full-screen shared containers.",
                    this);
            }

            valid = false;
        }
        else
        {
            for (int index = 0; index < resizedObjects.Count; index++)
            {
                if (resizedObjects[index] != null)
                {
                    continue;
                }

                if (logErrors)
                {
                    Debug.LogError(
                        $"[OCController] resizedObjects element {index} is " +
                        "unassigned.",
                        this);
                }

                valid = false;
            }
        }

        return valid;
    }

    private bool ValidateReference(
        UnityEngine.Object reference,
        string fieldName,
        string instruction,
        bool logErrors)
    {
        if (reference != null)
        {
            return true;
        }

        if (logErrors)
        {
            Debug.LogError(
                $"[OCController] Required field '{fieldName}' is not " +
                $"assigned on '{name}'. {instruction}",
                this);
        }

        return false;
    }

    private void OnDisable()
    {
        Unsubscribe();
        KillActiveTweens();
    }

    private void OnDestroy()
    {
        Unsubscribe();
        KillActiveTweens();
    }

#if UNITY_EDITOR
    private void OnValidate()
    {
        transitionDuration = Mathf.Max(0f, transitionDuration);
        portraitGameNameLogoScaleMultiplier =
            Mathf.Max(0f, portraitGameNameLogoScaleMultiplier);

        if (gameObject.scene.IsValid())
        {
            ValidateRequiredReferences(true);
        }
    }
#endif
}

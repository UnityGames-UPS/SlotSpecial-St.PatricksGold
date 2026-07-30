using System;
using System.Collections;
using System.Runtime.InteropServices;
using DG.Tweening;
using UnityEngine;
using UnityEngine.UI;
#if UNITY_EDITOR
using UnityEngine.InputSystem;
#endif

public sealed class OrientationChange : MonoBehaviour
{
    public enum OrientationMode
    {
        Landscape,
        DesktopPortrait,
        MobilePortrait
    }

    [Header("UI References")]
    [SerializeField] private RectTransform UIWrapper;
    [SerializeField] private CanvasScaler CanvasScaler;
    [SerializeField] private OCController presentationApplier;

    [Header("Transition Settings")]
    [SerializeField, Min(0f)] private float transitionDuration = 0.2f;
    [SerializeField, Min(0f)] private float waitForRotation = 0.2f;

    [Header("Device Detection Settings")]
    [SerializeField] private string mobileKeyword = "mobile";
    [SerializeField] private string currentDevice = "";

    public event Action<OrientationMode, int, int> OnOrientationChangedInstance;

    private Tween matchTween;
    private Tween rotationTween;
    private Coroutine rotationRoutine;
    private bool isLandscape;
    private bool hasStarted;
    private bool hostBridgeInitialized;
    private OrientationMode currentMode = OrientationMode.Landscape;
    private int lastWidth;
    private int lastHeight;

#if UNITY_WEBGL && !UNITY_EDITOR
    [DllImport("__Internal")]
    private static extern void InitializeOrientationChangeBridge(
        string receiverName);

    [DllImport("__Internal")]
    private static extern void ShutdownOrientationChangeBridge();
#endif

    public string CurrentDevice => currentDevice;
    public OrientationMode CurrentMode => currentMode;
    public bool IsLandscape => isLandscape;
    public bool IsMobile => IsMobileDevice();
    public int LastAcceptedWidth => lastWidth;
    public int LastAcceptedHeight => lastHeight;

    private void Awake()
    {
        ValidateRequiredReferences(true);
    }

    private void OnEnable()
    {
        if (!hasStarted)
        {
            return;
        }

        InitializeHostBridge();
        ApplyDimensions(
            lastWidth > 0 ? lastWidth : Screen.width,
            lastHeight > 0 ? lastHeight : Screen.height);
    }

    private void Start()
    {
        hasStarted = true;
        InitializeHostBridge();
        ApplyDimensions(Screen.width, Screen.height);
    }

    public void DeviceCheck(string device)
    {
        currentDevice = device ?? string.Empty;

        int width = lastWidth > 0 ? lastWidth : Screen.width;
        int height = lastHeight > 0 ? lastHeight : Screen.height;
        ApplyDimensions(width, height);
    }

    public bool IsMobileDevice()
    {
        if (!string.IsNullOrEmpty(currentDevice))
        {
            return !string.IsNullOrEmpty(mobileKeyword) &&
                   currentDevice.IndexOf(
                       mobileKeyword,
                       StringComparison.OrdinalIgnoreCase) >= 0;
        }

        return SystemInfo.deviceType == DeviceType.Handheld;
    }

    public void SwitchDisplay(string dimensions)
    {
        if (rotationRoutine != null)
        {
            StopCoroutine(rotationRoutine);
        }

        rotationRoutine = StartCoroutine(
            ApplyNewestDimensionsAfterDelay(dimensions));
    }

    private IEnumerator ApplyNewestDimensionsAfterDelay(string dimensions)
    {
        yield return new WaitForSecondsRealtime(waitForRotation);
        rotationRoutine = null;

        if (!TryParseDimensions(dimensions, out int width, out int height))
        {
            Debug.LogWarning(
                $"[OrientationChange] Ignored invalid SwitchDisplay payload " +
                $"'{dimensions ?? "<null>"}'. Expected two positive integers " +
                "formatted as 'width,height'. The last valid presentation was preserved.");
            yield break;
        }

        ApplyDimensions(width, height);
    }

    private static bool TryParseDimensions(
        string dimensions,
        out int width,
        out int height)
    {
        width = 0;
        height = 0;

        if (string.IsNullOrWhiteSpace(dimensions))
        {
            return false;
        }

        string[] parts = dimensions.Split(',');
        return parts.Length == 2 &&
               int.TryParse(parts[0].Trim(), out width) &&
               int.TryParse(parts[1].Trim(), out height) &&
               width > 0 &&
               height > 0;
    }

    private void ApplyDimensions(int width, int height)
    {
        if (width <= 0 || height <= 0)
        {
            Debug.LogWarning(
                $"[OrientationChange] Cannot apply non-positive dimensions " +
                $"{width}x{height}. The last valid presentation was preserved.");
            return;
        }

        if (!ValidateRequiredReferences(true))
        {
            return;
        }

        lastWidth = width;
        lastHeight = height;
        isLandscape = width > height;
        bool isMobile = IsMobileDevice();
        currentMode = ClassifyMode(width, height, isMobile);

        // The selected reference resolution is installed synchronously before
        // any CanvasScaler match calculation. This prevents the first
        // Landscape -> MobilePortrait transition from using stale 1920x1080
        // values.
        Vector2 referenceResolution =
            presentationApplier.ApplyReferenceResolution(currentMode);
        CanvasScaler.referenceResolution = referenceResolution;

        Quaternion targetRotation =
            currentMode == OrientationMode.DesktopPortrait
                ? Quaternion.Euler(0f, 0f, -90f)
                : Quaternion.identity;

        KillTween(ref rotationTween);
        if (transitionDuration > 0f)
        {
            rotationTween = UIWrapper
                .DOLocalRotateQuaternion(targetRotation, transitionDuration)
                .SetEase(Ease.OutCubic)
                .SetUpdate(true);
        }
        else
        {
            UIWrapper.localRotation = targetRotation;
        }

        float targetMatch = CalculateTargetMatch(
            currentMode,
            width,
            height,
            referenceResolution);

        KillTween(ref matchTween);
        if (transitionDuration > 0f)
        {
            matchTween = DOTween
                .To(
                    () => CanvasScaler.matchWidthOrHeight,
                    value => CanvasScaler.matchWidthOrHeight = value,
                    targetMatch,
                    transitionDuration)
                .SetEase(Ease.InOutQuad)
                .SetUpdate(true);
        }
        else
        {
            CanvasScaler.matchWidthOrHeight = targetMatch;
        }

        Debug.Log(
            $"[OrientationChange] Applied {width}x{height}; " +
            $"device='{currentDevice}'; isMobile={isMobile}; " +
            $"mode={currentMode}; reference={referenceResolution.x:0}x" +
            $"{referenceResolution.y:0}; wrapperZ=" +
            $"{(currentMode == OrientationMode.DesktopPortrait ? -90 : 0)}; " +
            $"match={targetMatch:0.####}.");

        // There is one event source and OCController subscribes to it once.
        OnOrientationChangedInstance?.Invoke(currentMode, width, height);
    }

    internal static OrientationMode ClassifyMode(
        int width,
        int height,
        bool isMobile)
    {
        if (width > height)
        {
            return OrientationMode.Landscape;
        }

        return isMobile
            ? OrientationMode.MobilePortrait
            : OrientationMode.DesktopPortrait;
    }

    internal static float CalculateTargetMatch(
        OrientationMode mode,
        int width,
        int height,
        Vector2 referenceResolution)
    {
        float referenceWidth = referenceResolution.x;
        float referenceHeight = referenceResolution.y;
        float widthScale = width / referenceWidth;
        float heightScale = height / referenceHeight;

        float targetScale;
        switch (mode)
        {
            case OrientationMode.DesktopPortrait:
                float portraitWidthScale = height / referenceWidth;
                float portraitHeightScale = width / referenceHeight;
                targetScale = Mathf.Min(
                    portraitWidthScale,
                    portraitHeightScale);
                break;

            case OrientationMode.MobilePortrait:
            case OrientationMode.Landscape:
            default:
                targetScale = Mathf.Min(widthScale, heightScale);
                break;
        }

        if (Mathf.Abs(heightScale - widthScale) < 0.0001f)
        {
            return 0.5f;
        }

        float logRatio = Mathf.Log(heightScale / widthScale);
        float targetMatch =
            Mathf.Log(targetScale / widthScale) / logRatio;
        return Mathf.Clamp01(targetMatch);
    }

    private bool ValidateRequiredReferences(bool logErrors)
    {
        bool valid = true;
        valid &= ValidateReference(
            UIWrapper,
            nameof(UIWrapper),
            "Assign the BG RectTransform.",
            logErrors);
        valid &= ValidateReference(
            CanvasScaler,
            nameof(CanvasScaler),
            "Assign MainCanvas's CanvasScaler.",
            logErrors);
        valid &= ValidateReference(
            presentationApplier,
            nameof(presentationApplier),
            "Assign the OCController presentation applier.",
            logErrors);

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
                $"[OrientationChange] Required field '{fieldName}' is not " +
                $"assigned on '{name}'. {instruction}",
                this);
        }

        return false;
    }

    private static void KillTween(ref Tween tween)
    {
        if (tween != null && tween.IsActive())
        {
            tween.Kill(false);
        }

        tween = null;
    }

    private void InitializeHostBridge()
    {
#if UNITY_WEBGL && !UNITY_EDITOR
        if (hostBridgeInitialized)
        {
            return;
        }

        InitializeOrientationChangeBridge(gameObject.name);
        hostBridgeInitialized = true;
#endif
    }

    private void ShutdownHostBridge()
    {
#if UNITY_WEBGL && !UNITY_EDITOR
        if (!hostBridgeInitialized)
        {
            return;
        }

        ShutdownOrientationChangeBridge();
        hostBridgeInitialized = false;
#endif
    }

    private void OnDisable()
    {
        if (rotationRoutine != null)
        {
            StopCoroutine(rotationRoutine);
            rotationRoutine = null;
        }

        KillTween(ref rotationTween);
        KillTween(ref matchTween);
        ShutdownHostBridge();
    }

    private void OnDestroy()
    {
        ShutdownHostBridge();
    }

#if UNITY_EDITOR
    private void Update()
    {
        Keyboard keyboard = Keyboard.current;
        if (keyboard == null)
        {
            return;
        }

        if (keyboard.spaceKey.wasPressedThisFrame)
        {
            int width = lastHeight > 0 ? lastHeight : Screen.height;
            int height = lastWidth > 0 ? lastWidth : Screen.width;
            SwitchDisplay($"{width},{height}");
        }

        if (keyboard.mKey.wasPressedThisFrame)
        {
            bool currentlyMobile =
                !string.IsNullOrEmpty(currentDevice) &&
                !string.IsNullOrEmpty(mobileKeyword) &&
                currentDevice.IndexOf(
                    mobileKeyword,
                    StringComparison.OrdinalIgnoreCase) >= 0;
            DeviceCheck(currentlyMobile ? "desktop" : "mobile");
        }
    }

    private void OnValidate()
    {
        transitionDuration = Mathf.Max(0f, transitionDuration);
        waitForRotation = Mathf.Max(0f, waitForRotation);

        if (gameObject.scene.IsValid())
        {
            ValidateRequiredReferences(true);
        }
    }
#endif
}

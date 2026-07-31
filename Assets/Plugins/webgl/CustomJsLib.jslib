mergeInto(LibraryManager.library, {
    SendLogToReactNative: function (messagePtr) {
        var message = UTF8ToString(messagePtr);
        if (window.ReactNativeWebView) {
          window.ReactNativeWebView.postMessage(message);
        } 
    },

    SendPostMessage: function(messagePtr) {
      var message = UTF8ToString(messagePtr);
      if(window.ReactNativeWebView){
        if(message == "authToken"){
          window.ReactNativeWebView.postMessage("if message is authtoken");
          var injectedObjectJson = window.ReactNativeWebView.injectedObjectJson();
          var injectedObj = JSON.parse(injectedObjectJson);

          window.ReactNativeWebView.postMessage('Injected obj : ' + injectedObjectJson);
          
          var combinedData = JSON.stringify({
              socketURL: injectedObj.socketURL.trim(),
              cookie: injectedObj.token.trim(),
              nameSpace: injectedObj.nameSpace ? injectedObj.nameSpace.trim() : ""
          });

          if (typeof SendMessage === 'function') {
            SendMessage('SocketManager', 'ReceiveAuthToken', combinedData);
          }
        }
        window.ReactNativeWebView.postMessage(message);
      }
      else if(window.parent){
        if(window.parent.dispatchReactUnityEvent){
          console.log("Inside window parent");
          window.parent.dispatchReactUnityEvent(message); 
        }
      }
    },

    RequestFullscreen: function () {
      console.log('[JS] RequestFullscreen called');
      var el = document.documentElement;
      var req = el.requestFullscreen
             || el.webkitRequestFullscreen
             || el.mozRequestFullScreen
             || el.msRequestFullscreen;
      if (req) {
        req.call(el).then(function() {
          console.log('[JS] Fullscreen request succeeded');
        }).catch(function(err) {
          console.warn('[JS] RequestFullscreen failed:', err);
        });
      } else {
        console.error('[JS] No fullscreen API available!');
      }
    },

    ExitFullscreen: function () {
      console.log('[JS] ExitFullscreen called');
      var exit = document.exitFullscreen
              || document.webkitExitFullscreen
              || document.mozCancelFullScreen
              || document.msExitFullscreen;
      if (exit) {
        exit.call(document).then(function() {
          console.log('[JS] Exit fullscreen succeeded');
        }).catch(function(err) {
          console.warn('[JS] ExitFullscreen failed:', err);
        });
      } else {
        console.error('[JS] No exit fullscreen API available!');
      }
    },

    RegisterFullscreenChangeListener: function(gameObjectNamePtr) {
        var gameObjectName = UTF8ToString(gameObjectNamePtr);
        console.log('[JS] RegisterFullscreenChangeListener called for GameObject:', gameObjectName);

        // Helper to check current fullscreen state
        function isCurrentlyFullscreen() {
            return !!(document.fullscreenElement || 
                      document.webkitFullscreenElement || 
                      document.mozFullScreenElement || 
                      document.msFullscreenElement);
        }

        // Helper to find the Unity instance
        function getUnityInstance() {
            if (typeof window.unityInstance !== 'undefined' && window.unityInstance && window.unityInstance.SendMessage) {
                return window.unityInstance;
            }
            if (typeof window.gameInstance !== 'undefined' && window.gameInstance && window.gameInstance.SendMessage) {
                return window.gameInstance;
            }
            if (typeof Module !== 'undefined' && Module && Module.SendMessage) {
                return Module;
            }
            if (typeof unityInstance !== 'undefined' && unityInstance && unityInstance.SendMessage) {
                return unityInstance;
            }
            if (window.parent && window.parent !== window) {
                if (window.parent.unityInstance && window.parent.unityInstance.SendMessage) {
                    return window.parent.unityInstance;
                }
                if (window.parent.gameInstance && window.parent.gameInstance.SendMessage) {
                    return window.parent.gameInstance;
                }
            }
            for (var key in window) {
                try {
                    if (window.hasOwnProperty(key)) {
                        var obj = window[key];
                        if (obj && typeof obj === 'object' && typeof obj.SendMessage === 'function') {
                            return obj;
                        }
                    }
                } catch(e) {}
            }
            return null;
        }

        // Send fullscreen state to Unity
        function sendToUnity(isFS) {
            try {
                if (typeof SendMessage === 'function') {
                    SendMessage(
                        gameObjectName,
                        'OnFullscreenChanged',
                        isFS ? '1' : '0');
                    console.log(
                        '[JS] Sent fullscreen state to Unity: ' +
                        (isFS ? 'EXPANDED' : 'SHRINK'));
                    return;
                }

                var instance = getUnityInstance();
                if (instance && instance.SendMessage) {
                    instance.SendMessage(gameObjectName, 'OnFullscreenChanged', isFS ? '1' : '0');
                    console.log('[JS] Sent fullscreen state to Unity: ' + (isFS ? 'EXPANDED' : 'SHRINK'));
                } else {
                    console.warn('[JS] Unity instance not available, cannot send');
                }
            } catch (err) {
                console.error('[JS] Error sending message to Unity:', err);
            }
        }

        // Remove the previously registered callback before replacing it.
        if (window._unityFullscreenCallback) {
            document.removeEventListener('fullscreenchange', window._unityFullscreenCallback);
            document.removeEventListener('webkitfullscreenchange', window._unityFullscreenCallback);
            document.removeEventListener('mozfullscreenchange', window._unityFullscreenCallback);
            document.removeEventListener('MSFullscreenChange', window._unityFullscreenCallback);
        }

        // Fullscreen change callback
        window._unityFullscreenCallback = function() {
            var isFS = isCurrentlyFullscreen();
            console.log('[JS] Fullscreen event fired. State:', isFS ? 'EXPANDED' : 'SHRINK');
            sendToUnity(isFS);
        };

        // Register listeners for all browser engines
        document.addEventListener('fullscreenchange',       window._unityFullscreenCallback);
        document.addEventListener('webkitfullscreenchange', window._unityFullscreenCallback);
        document.addEventListener('mozfullscreenchange',    window._unityFullscreenCallback);
        document.addEventListener('MSFullscreenChange',     window._unityFullscreenCallback);

        console.log('[JS] Fullscreen event listeners registered for:', gameObjectName);

        // Synchronize Unity with the browser's current state immediately.
        setTimeout(function() {
            sendToUnity(isCurrentlyFullscreen());
        }, 0);
    },

    InitializeOrientationChangeBridge: function(gameObjectNamePtr) {
        var receiverName = UTF8ToString(gameObjectNamePtr);

        if (window.__unityOrientationChangeBridge &&
            typeof window.__unityOrientationChangeBridge.cleanup === 'function') {
            window.__unityOrientationChangeBridge.cleanup();
        }

        var state = {
            receiverName: receiverName,
            debounceTimer: null,
            initialTimer: null,
            initialAttempts: 0,
            initialDeviceSent: false,
            initialDimensionsSent: false,
            resizeObserver: null,
            explicitDevice: null,
            destroyed: false
        };

        function getCanvas() {
            if (typeof Module !== 'undefined' && Module && Module.canvas) {
                return Module.canvas;
            }

            return document.querySelector('#unity-canvas') ||
                   document.querySelector('canvas');
        }

        function getUnityInstance() {
            if (window.unityInstance &&
                typeof window.unityInstance.SendMessage === 'function') {
                return window.unityInstance;
            }

            if (window.gameInstance &&
                typeof window.gameInstance.SendMessage === 'function') {
                return window.gameInstance;
            }

            if (typeof Module !== 'undefined' &&
                Module &&
                typeof Module.SendMessage === 'function') {
                return Module;
            }

            return null;
        }

        function sendToOC(methodName, payload) {
            if (state.destroyed) {
                return false;
            }

            try {
                // A .jslib call originates inside the loaded Unity runtime, so
                // the Emscripten SendMessage function is the primary route.
                if (typeof SendMessage === 'function') {
                    SendMessage(
                        state.receiverName,
                        methodName,
                        String(payload));
                    return true;
                }

                var unity = getUnityInstance();
                if (unity) {
                    unity.SendMessage(
                        state.receiverName,
                        methodName,
                        String(payload));
                    return true;
                }
            } catch (error) {
                console.warn(
                    '[OC] Could not send ' + methodName + ' to Unity:',
                    error);
            }

            return false;
        }

        function normalizeDevice(value) {
            if (typeof value === 'boolean') {
                return value ? 'mobile' : 'desktop';
            }

            if (value === null || typeof value === 'undefined') {
                return null;
            }

            var normalized = String(value).trim().toLowerCase();
            if (!normalized) {
                return null;
            }

            return /mobile|android|iphone|ipad|ipod|tablet/.test(normalized)
                ? 'mobile'
                : 'desktop';
        }

        function readInjectedDevice() {
            var explicitWindowDevice =
                normalizeDevice(window.unityOCDeviceType);
            if (explicitWindowDevice) {
                return explicitWindowDevice;
            }

            if (window.ReactNativeWebView &&
                typeof window.ReactNativeWebView.injectedObjectJson ===
                    'function') {
                try {
                    var injected = JSON.parse(
                        window.ReactNativeWebView.injectedObjectJson());
                    var injectedDevice = normalizeDevice(
                        injected.deviceType ||
                        injected.device ||
                        injected.isMobile);
                    if (injectedDevice) {
                        return injectedDevice;
                    }
                } catch (error) {
                    console.warn(
                        '[OC] Could not read injected device configuration:',
                        error);
                }
            }

            var canvas = getCanvas();
            if (canvas && canvas.dataset) {
                var canvasDevice = normalizeDevice(
                    canvas.dataset.ocDevice ||
                    canvas.dataset.device);
                if (canvasDevice) {
                    return canvasDevice;
                }
            }

            if (navigator.userAgentData &&
                typeof navigator.userAgentData.mobile === 'boolean') {
                return navigator.userAgentData.mobile
                    ? 'mobile'
                    : 'desktop';
            }

            return /android|iphone|ipad|ipod|mobile|tablet/i.test(
                navigator.userAgent || '')
                ? 'mobile'
                : 'desktop';
        }

        function classifyDevice() {
            return state.explicitDevice || readInjectedDevice();
        }

        function measureRenderSurface() {
            var canvas = getCanvas();
            if (!canvas) {
                return null;
            }

            // Unity Screen.width/height in WebGL follow the canvas backing
            // buffer. Use canvas.width/height rather than CSS pixels so both
            // sides use the same coordinate space at high devicePixelRatio.
            var width = Math.round(Number(canvas.width));
            var height = Math.round(Number(canvas.height));
            if (width <= 0 || height <= 0) {
                return null;
            }

            return { width: width, height: height };
        }

        function sendDimensionsNow() {
            var size = measureRenderSurface();
            if (!size) {
                return false;
            }

            return sendToOC(
                'SwitchDisplay',
                size.width + ',' + size.height);
        }

        function scheduleDimensions() {
            if (state.destroyed) {
                return;
            }

            if (state.debounceTimer !== null) {
                clearTimeout(state.debounceTimer);
            }

            state.debounceTimer = setTimeout(function() {
                state.debounceTimer = null;
                sendDimensionsNow();
            }, 100);
        }

        function sendInitialState() {
            if (state.destroyed) {
                return;
            }

            if (!state.initialDeviceSent) {
                state.initialDeviceSent = sendToOC(
                    'DeviceCheck',
                    classifyDevice());
            }

            if (!state.initialDimensionsSent) {
                state.initialDimensionsSent = sendDimensionsNow();
            }

            if ((!state.initialDeviceSent ||
                 !state.initialDimensionsSent) &&
                state.initialAttempts < 50) {
                state.initialAttempts++;
                state.initialTimer = setTimeout(sendInitialState, 100);
            }
        }

        function handleExplicitDevice(event) {
            var normalized = normalizeDevice(
                event && typeof event.detail !== 'undefined'
                    ? event.detail
                    : null);
            if (!normalized) {
                return;
            }

            state.explicitDevice = normalized;
            sendToOC('DeviceCheck', normalized);
            scheduleDimensions();
        }

        function cleanup() {
            if (state.destroyed) {
                return;
            }

            state.destroyed = true;
            if (state.debounceTimer !== null) {
                clearTimeout(state.debounceTimer);
            }
            if (state.initialTimer !== null) {
                clearTimeout(state.initialTimer);
            }

            window.removeEventListener('resize', scheduleDimensions);
            window.removeEventListener(
                'orientationchange',
                scheduleDimensions);
            window.removeEventListener(
                'unity-oc-resize',
                scheduleDimensions);
            window.removeEventListener(
                'unity-oc-device',
                handleExplicitDevice);
            document.removeEventListener(
                'fullscreenchange',
                scheduleDimensions);
            document.removeEventListener(
                'webkitfullscreenchange',
                scheduleDimensions);
            document.removeEventListener(
                'mozfullscreenchange',
                scheduleDimensions);
            document.removeEventListener(
                'MSFullscreenChange',
                scheduleDimensions);

            if (window.visualViewport) {
                window.visualViewport.removeEventListener(
                    'resize',
                    scheduleDimensions);
            }

            if (state.resizeObserver) {
                state.resizeObserver.disconnect();
                state.resizeObserver = null;
            }

            if (window.__unityOrientationChangeBridge === state) {
                window.__unityOrientationChangeBridge = null;
            }
        }

        state.cleanup = cleanup;
        window.__unityOrientationChangeBridge = state;

        window.addEventListener('resize', scheduleDimensions);
        window.addEventListener(
            'orientationchange',
            scheduleDimensions);
        window.addEventListener(
            'unity-oc-resize',
            scheduleDimensions);
        window.addEventListener(
            'unity-oc-device',
            handleExplicitDevice);
        document.addEventListener(
            'fullscreenchange',
            scheduleDimensions);
        document.addEventListener(
            'webkitfullscreenchange',
            scheduleDimensions);
        document.addEventListener(
            'mozfullscreenchange',
            scheduleDimensions);
        document.addEventListener(
            'MSFullscreenChange',
            scheduleDimensions);

        if (window.visualViewport) {
            window.visualViewport.addEventListener(
                'resize',
                scheduleDimensions);
        }

        var canvas = getCanvas();
        if (canvas && typeof ResizeObserver !== 'undefined') {
            state.resizeObserver =
                new ResizeObserver(scheduleDimensions);
            state.resizeObserver.observe(canvas);
            if (canvas.parentElement) {
                state.resizeObserver.observe(canvas.parentElement);
            }
        }

        // Defer until the current Unity frame and host layout have settled.
        state.initialTimer = setTimeout(sendInitialState, 0);
        console.log(
            '[OC] Orientation bridge initialized for Unity receiver:',
            receiverName);
    },

    ShutdownOrientationChangeBridge: function() {
        if (window.__unityOrientationChangeBridge &&
            typeof window.__unityOrientationChangeBridge.cleanup ===
                'function') {
            window.__unityOrientationChangeBridge.cleanup();
        }
    }
});

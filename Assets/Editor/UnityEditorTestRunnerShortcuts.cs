using System;
using System.Reflection;
using UnityEditor;
using UnityEditor.TestTools.TestRunner;
using UnityEditor.TestTools.TestRunner.TestRun;
using UnityEngine;

namespace EditorUtils {
    public class TestRunnerShortcuts {
        private const string RerunSelectedMenuTestName = "Test Runner/Run Selected Tests on Focus";
    private const string SkipDomainReloadMenuName = "Test Runner/Skip Domain Reload After Tests";
    private const string ForceReleaseDomainReloadMenuName = "Test Runner/Force Release Domain Reload Lock";

        private static bool rerunSelectedOnFocus {
            get => Menu.GetChecked(RerunSelectedMenuTestName);
            set => Menu.SetChecked(RerunSelectedMenuTestName, value);
        }

        private static bool skipDomainReload {
            get => EditorPrefs.GetBool("TestRunner.SkipDomainReload", true);
            set => EditorPrefs.SetBool("TestRunner.SkipDomainReload", value);
        }

        private static bool skipDomainReloadLockHeld;
        private static Type domainReloadControllerType;
        private static MethodInfo ensureSkipLockMethod;
        private static MethodInfo forceReleaseSkipLockMethod;
        private static bool domainReloadControllerReflectionInitialized;

        public static Action<bool> unityEditorFocusChanged {
            get {
                var fieldInfo = typeof(EditorApplication).GetField("focusChanged", System.Reflection.BindingFlags.Static | System.Reflection.BindingFlags.NonPublic);
                return (Action<bool>)fieldInfo.GetValue(null);
            }
            set {
                var fieldInfo = typeof(EditorApplication).GetField("focusChanged", System.Reflection.BindingFlags.Static | System.Reflection.BindingFlags.NonPublic);
                fieldInfo.SetValue(null, value);
            }
        }

        public static void OnRerunSelectedOnFocus(bool hasFocus) {
            if (hasFocus)
                RunSelected();
        }

        /// <summary>
        /// Helper method to get the currently selected test list GUI (EditMode/PlayMode/Player)
        /// </summary>
        private static object GetCurrentTestListGUI() {
            var window = EditorWindow.GetWindow<TestRunnerWindow>();

            // Get m_SelectedTestTypes which points to the currently active test list
            var selectedTestTypesField = window.GetType().GetField("m_SelectedTestTypes", BindingFlags.Instance | BindingFlags.NonPublic);
            return selectedTestTypesField.GetValue(window);
        }

        /// <summary>
        /// Helper method to invoke RunTests with a specific filter type
        /// </summary>
        private static void InvokeRunTests(object testListGUI, int runFilterType) {
            var isBusyMethod = testListGUI.GetType().GetMethod("IsBusy", BindingFlags.Static | BindingFlags.NonPublic);

            if (isBusyMethod != null && isBusyMethod.Invoke(null, null).Equals(true))
                return;

            // Get the RunTests method: private void RunTests(RunFilterType runFilter, params int[] specificTests)
            var runTestsMethod = testListGUI.GetType().GetMethod("RunTests", BindingFlags.Instance | BindingFlags.NonPublic);

            // Get the RunFilterType enum from the testListGUI type
            var runFilterTypeEnum = testListGUI.GetType().GetNestedType("RunFilterType", BindingFlags.NonPublic);
            var filterValue = Enum.ToObject(runFilterTypeEnum, runFilterType);

            // Invoke RunTests with the filter type
            runTestsMethod.Invoke(testListGUI, new object[] { filterValue, new int[0] });
        }

        /// <summary>
        /// Programmatically runs all tests in the currently selected tab (EditMode/PlayMode)
        /// Equivalent to clicking the "Run All" button
        /// </summary>
        [MenuItem("Test Runner/Run All Tests %#]")]
        public static void RunAll() {
            try {
                var testListGUI = GetCurrentTestListGUI();

                if (testListGUI == null) {
                    Debug.LogWarning("Test Runner window not properly initialized!");
                    return;
                }

                // RunFilterType.RunAll = 0
                InvokeRunTests(testListGUI, 0);
            }
            catch (Exception e) {
                Debug.LogError($"Failed to run all tests: {e.Message}");
            }
        }

        /// <summary>
        /// Programmatically runs selected tests in the currently selected tab (EditMode/PlayMode)
        /// Equivalent to clicking the "Run Selected" button
        /// </summary>
        [MenuItem("Test Runner/Run Selected Tests %]")]
        public static void RunSelected() {
            try {
                var testListGUI = GetCurrentTestListGUI();

                if (testListGUI == null) {
                    Debug.LogWarning("Test Runner window not properly initialized!");
                    return;
                }

                // RunFilterType.RunSelected = 1
                InvokeRunTests(testListGUI, 1);
            }
            catch (Exception e) {
                Debug.LogError($"Failed to run selected tests: {e.Message}");
            }
        }

        /// <summary>
        /// Programmatically reruns failed tests in the currently selected tab (EditMode/PlayMode)
        /// Equivalent to clicking the "Rerun Failed" button
        /// </summary>
        [MenuItem("Test Runner/Rerun Failed Tests")]
        public static void RerunFailed() {
            try {
                var testListGUI = GetCurrentTestListGUI();

                if (testListGUI == null) {
                    Debug.LogWarning("Test Runner window not properly initialized!");
                    return;
                }

                // RunFilterType.RunFailed = 2
                InvokeRunTests(testListGUI, 2);
            }
            catch (Exception e) {
                Debug.LogError($"Failed to rerun failed tests: {e.Message}");
            }
        }

        // Legacy method - kept for compatibility
        public static void RunSelectedTests() {
            RunSelected();
        }

        // Legacy method - modified to use current test list
        public static void RunAllTests() {
            RunAll();
        }

        [MenuItem(RerunSelectedMenuTestName)]
        public static void RunSelectedTestsOnFocus() {
            rerunSelectedOnFocus = !rerunSelectedOnFocus;

            if (rerunSelectedOnFocus)
                unityEditorFocusChanged += OnRerunSelectedOnFocus;
            else
                unityEditorFocusChanged -= OnRerunSelectedOnFocus;
        }

        [MenuItem(SkipDomainReloadMenuName)]
        public static void ToggleSkipDomainReload() {
            skipDomainReload = !skipDomainReload;
            Menu.SetChecked(SkipDomainReloadMenuName, skipDomainReload);
            SyncSkipDomainReloadLock();
            Debug.Log($"Skip Domain Reload After Tests: {(skipDomainReload ? "ENABLED" : "DISABLED")}");
        }

        [MenuItem(SkipDomainReloadMenuName, true)]
        public static bool ToggleSkipDomainReloadValidate() {
            Menu.SetChecked(SkipDomainReloadMenuName, skipDomainReload);
            SyncSkipDomainReloadLock();
            return true;
        }

        [MenuItem(ForceReleaseDomainReloadMenuName)]
        public static void ForceReleaseDomainReloadLock() {
            try {
                if (!TryResolveDomainReloadController())
                    return;

                forceReleaseSkipLockMethod.Invoke(null, null);
                skipDomainReloadLockHeld = false;
                Debug.Log("[TestRunner] Forced release of skip domain reload lock.");
            }
            catch (Exception e) {
                Debug.LogWarning($"[TestRunner] Failed to forcibly release assembly lock: {e.Message}");
            }
        }

        [MenuItem(ForceReleaseDomainReloadMenuName, true)]
        public static bool ForceReleaseDomainReloadLockValidate() {
            return TryResolveDomainReloadController(false);
        }

        [InitializeOnLoadMethod]
        public static void Initialize() {
            unityEditorFocusChanged -= OnRerunSelectedOnFocus;

            if (rerunSelectedOnFocus)
                unityEditorFocusChanged += OnRerunSelectedOnFocus;

            SyncSkipDomainReloadLock();
        }

        private static void SyncSkipDomainReloadLock() {
            if (skipDomainReload) {
                EnsureSkipDomainReloadLock();
            } else {
                ReleaseSkipDomainReloadLock();
            }
        }

        private static void EnsureSkipDomainReloadLock() {
            if (skipDomainReloadLockHeld)
                return;

            try {
                if (!TryResolveDomainReloadController())
                    return;

                ensureSkipLockMethod.Invoke(null, null);
                skipDomainReloadLockHeld = true;
            }
            catch (Exception e) {
                Debug.LogWarning($"[TestRunner] Failed to acquire assembly lock for skip preference: {e.Message}");
            }
        }

        private static void ReleaseSkipDomainReloadLock() {
            if (!skipDomainReloadLockHeld)
                return;

            try {
                if (!TryResolveDomainReloadController())
                    return;

                forceReleaseSkipLockMethod.Invoke(null, null);
                skipDomainReloadLockHeld = false;
            }
            catch (Exception e) {
                Debug.LogWarning($"[TestRunner] Failed to release assembly lock for skip preference: {e.Message}");
            }
        }

        // Lazily resolve the internal DomainReloadController and cache its lock helpers.
        private static bool TryResolveDomainReloadController(bool logOnFailure = true) {
            if (domainReloadControllerReflectionInitialized)
                return domainReloadControllerType != null && ensureSkipLockMethod != null && forceReleaseSkipLockMethod != null;

            domainReloadControllerReflectionInitialized = true;

            foreach (var assembly in AppDomain.CurrentDomain.GetAssemblies()) {
                var type = assembly.GetType("UnityEditor.TestTools.TestRunner.DomainReloadController");
                if (type == null)
                    continue;

                domainReloadControllerType = type;
                ensureSkipLockMethod = domainReloadControllerType.GetMethod("EnsureSkipLock", BindingFlags.Static | BindingFlags.NonPublic);
                forceReleaseSkipLockMethod = domainReloadControllerType.GetMethod("ForceReleaseSkipLock", BindingFlags.Static | BindingFlags.NonPublic);
                break;
            }

            if (domainReloadControllerType == null || ensureSkipLockMethod == null || forceReleaseSkipLockMethod == null) {
                if (logOnFailure)
                    Debug.LogWarning("[TestRunner] Unable to locate DomainReloadController lock methods via reflection. Skip Domain Reload toggle cannot manage the underlying lock.");
                return false;
            }

            return true;
        }

    }
}

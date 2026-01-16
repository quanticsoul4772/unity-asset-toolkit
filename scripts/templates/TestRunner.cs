using UnityEditor;
using UnityEditor.TestTools.TestRunner.Api;
using UnityEngine;
using System;
using System.Collections.Generic;

/// <summary>
/// Test runner script for Codebuff CLI integration.
/// Provides programmatic access to Unity Test Framework.
/// </summary>
public static class TestRunner
{
    private static TestRunnerApi _api;
    private static bool _testsRunning = false;
    private static int _passCount = 0;
    private static int _failCount = 0;
    private static List<string> _failedTests = new List<string>();

    /// <summary>
    /// Run all tests from CLI.
    /// Called via: -executeMethod TestRunner.RunAllTestsFromCLI
    /// </summary>
    public static void RunAllTestsFromCLI()
    {
        Debug.Log("[TestRunner] Starting test run...");
        
        _api = ScriptableObject.CreateInstance<TestRunnerApi>();
        
        var callbacks = new TestCallbacks();
        _api.RegisterCallbacks(callbacks);
        
        // Run EditMode tests (faster, no Play mode required)
        var filter = new Filter
        {
            testMode = TestMode.EditMode
        };
        
        _api.Execute(new ExecutionSettings(filter));
    }

    /// <summary>
    /// Run PlayMode tests from CLI.
    /// Called via: -executeMethod TestRunner.RunPlayModeTestsFromCLI
    /// </summary>
    public static void RunPlayModeTestsFromCLI()
    {
        Debug.Log("[TestRunner] Starting PlayMode test run...");
        
        _api = ScriptableObject.CreateInstance<TestRunnerApi>();
        
        var callbacks = new TestCallbacks();
        _api.RegisterCallbacks(callbacks);
        
        var filter = new Filter
        {
            testMode = TestMode.PlayMode
        };
        
        _api.Execute(new ExecutionSettings(filter));
    }

    /// <summary>
    /// Run tests matching a specific category.
    /// </summary>
    public static void RunTestsByCategory(string category, TestMode mode = TestMode.EditMode)
    {
        Debug.Log($"[TestRunner] Running tests with category: {category}");
        
        _api = ScriptableObject.CreateInstance<TestRunnerApi>();
        
        var callbacks = new TestCallbacks();
        _api.RegisterCallbacks(callbacks);
        
        var filter = new Filter
        {
            testMode = mode,
            categoryNames = new[] { category }
        };
        
        _api.Execute(new ExecutionSettings(filter));
    }

    /// <summary>
    /// Menu item to run EditMode tests.
    /// </summary>
    [MenuItem("Tests/Run EditMode Tests")]
    public static void RunEditModeTests()
    {
        RunAllTestsFromCLI();
    }

    /// <summary>
    /// Menu item to run PlayMode tests.
    /// </summary>
    [MenuItem("Tests/Run PlayMode Tests")]
    public static void RunPlayModeTests()
    {
        RunPlayModeTestsFromCLI();
    }

    /// <summary>
    /// Test callbacks for tracking results.
    /// </summary>
    private class TestCallbacks : ICallbacks
    {
        public void RunStarted(ITestAdaptor testsToRun)
        {
            _testsRunning = true;
            _passCount = 0;
            _failCount = 0;
            _failedTests.Clear();
            Debug.Log($"[TestRunner] Test run started. Total tests: {testsToRun.TestCaseCount}");
        }

        public void RunFinished(ITestResultAdaptor result)
        {
            _testsRunning = false;
            
            Debug.Log("[TestRunner] ========== TEST RESULTS ==========");
            Debug.Log($"[TestRunner] Total: {result.TestCaseCount}");
            Debug.Log($"[TestRunner] Passed: {result.PassCount}");
            Debug.Log($"[TestRunner] Failed: {result.FailCount}");
            Debug.Log($"[TestRunner] Skipped: {result.SkipCount}");
            Debug.Log($"[TestRunner] Duration: {result.Duration:F2}s");
            
            if (_failedTests.Count > 0)
            {
                Debug.LogError("[TestRunner] Failed tests:");
                foreach (var testName in _failedTests)
                {
                    Debug.LogError($"  - {testName}");
                }
                EditorApplication.Exit(1);
            }
            else
            {
                Debug.Log("[TestRunner] All tests PASSED!");
                EditorApplication.Exit(0);
            }
        }

        public void TestStarted(ITestAdaptor test)
        {
            // Optionally log test starts
        }

        public void TestFinished(ITestResultAdaptor result)
        {
            if (result.TestStatus == TestStatus.Passed)
            {
                _passCount++;
            }
            else if (result.TestStatus == TestStatus.Failed)
            {
                _failCount++;
                _failedTests.Add(result.Test.Name);
                Debug.LogError($"[TestRunner] FAILED: {result.Test.Name}");
                Debug.LogError($"  Message: {result.Message}");
            }
        }
    }
}

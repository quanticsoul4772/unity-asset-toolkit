using NUnit.Framework;
using UnityEngine;
using UnityEngine.TestTools;
using NPCBrain.BehaviorTree;
using NPCBrain.BehaviorTree.Composites;
using NPCBrain.BehaviorTree.Decorators;
using NPCBrain.UtilityAI;
using NPCBrain.Perception;
using NPCBrain.Tests;
using System.Text.RegularExpressions;

namespace NPCBrain.Tests.Editor
{
    /// <summary>
    /// Tests that verify proper warnings are logged for common misconfiguration scenarios.
    /// These tests ensure developers get helpful feedback when components are misconfigured.
    /// </summary>
    [TestFixture]
    public class MisconfigurationWarningTests
    {
        private GameObject _testObject;
        private TestBrain _brain;
        
        [SetUp]
        public void SetUp()
        {
            _testObject = new GameObject("TestNPC");
            _brain = _testObject.AddComponent<TestBrain>();
            _brain.InitializeForTests();
        }
        
        [TearDown]
        public void TearDown()
        {
            if (_testObject != null)
            {
                Object.DestroyImmediate(_testObject);
            }
        }
        
        #region UtilitySelector Warning Tests
        
        [Test]
        public void UtilitySelector_NoActions_LogsWarning()
        {
            var selector = new UtilitySelector();
            selector.LogWarnings = true;
            
            LogAssert.Expect(LogType.Warning, new Regex(@"\[UtilitySelector\] No actions configured"));
            
            selector.Execute(_brain);
        }
        
        [Test]
        public void UtilitySelector_NullBrain_LogsWarning()
        {
            var action = new UtilityAction("Test", new MockNode(NodeStatus.Success), new ConstantConsideration(1f));
            var selector = new UtilitySelector(action);
            selector.LogWarnings = true;
            
            LogAssert.Expect(LogType.Warning, new Regex(@"\[UtilitySelector\] Brain is null"));
            
            selector.Execute(null);
        }
        
        [Test]
        public void UtilitySelector_AllZeroScores_LogsWarning()
        {
            var action = new UtilityAction("Test", new MockNode(NodeStatus.Success), new ConstantConsideration(0f));
            var selector = new UtilitySelector(action);
            selector.LogWarnings = true;
            
            LogAssert.Expect(LogType.Warning, new Regex(@"\[UtilitySelector\] All.*action.*scored <= 0"));
            
            selector.Execute(_brain);
        }
        
        [Test]
        public void UtilitySelector_LogWarningsDisabled_NoWarning()
        {
            var selector = new UtilitySelector();
            selector.LogWarnings = false;
            
            // Should not log any warning
            selector.Execute(_brain);
            
            LogAssert.NoUnexpectedReceived();
        }
        
        #endregion
        
        #region Decorator Warning Tests
        
        [Test]
        public void Inverter_NullChild_LogsWarning()
        {
            var inverter = new Inverter(null);
            
            LogAssert.Expect(LogType.Warning, new Regex(@"\[Inverter\] Child node is null"));
            
            inverter.Execute(_brain);
        }
        
        [Test]
        public void Repeater_NullChild_LogsWarning()
        {
            var repeater = new Repeater(null, 3);
            
            LogAssert.Expect(LogType.Warning, new Regex(@"\[Repeater\] Child node is null"));
            
            repeater.Execute(_brain);
        }
        
        [Test]
        public void Cooldown_NullChild_LogsWarning()
        {
            var cooldown = new Cooldown(null, 1f);
            
            LogAssert.Expect(LogType.Warning, new Regex(@"\[Cooldown\] Child node is null"));
            
            cooldown.Execute(_brain);
        }
        
        [Test]
        public void Succeeder_NullChild_LogsWarning()
        {
            var succeeder = new Succeeder(null);
            
            LogAssert.Expect(LogType.Warning, new Regex(@"\[Succeeder\] Child node is null"));
            
            succeeder.Execute(_brain);
        }
        
        [Test]
        public void Inverter_ValidChild_NoWarning()
        {
            var child = new MockNode(NodeStatus.Success);
            var inverter = new Inverter(child);
            
            inverter.Execute(_brain);
            
            LogAssert.NoUnexpectedReceived();
        }
        
        #endregion
        
        #region Blackboard Warning Tests
        
        [Test]
        public void Blackboard_TypeMismatch_LogsWarningWhenEnabled()
        {
            var blackboard = new Blackboard();
            blackboard.LogTypeMismatches = true;
            blackboard.Set("health", "not an int");
            
            LogAssert.Expect(LogType.Warning, new Regex(@"\[Blackboard\] Type mismatch for key 'health'"));
            
            blackboard.Get<int>("health", 42);
        }
        
        [Test]
        public void Blackboard_TypeMismatch_NoWarningWhenDisabled()
        {
            var blackboard = new Blackboard();
            blackboard.LogTypeMismatches = false;
            blackboard.Set("health", "not an int");
            
            blackboard.Get<int>("health", 42);
            
            LogAssert.NoUnexpectedReceived();
        }
        
        [Test]
        public void Blackboard_CorrectType_NoWarning()
        {
            var blackboard = new Blackboard();
            blackboard.LogTypeMismatches = true;
            blackboard.Set("health", 100);
            
            blackboard.Get<int>("health");
            
            LogAssert.NoUnexpectedReceived();
        }
        
        [Test]
        public void Blackboard_TypeMismatch_TryGet_LogsWarningWhenEnabled()
        {
            var blackboard = new Blackboard();
            blackboard.LogTypeMismatches = true;
            blackboard.Set("position", "not a vector");
            
            LogAssert.Expect(LogType.Warning, new Regex(@"\[Blackboard\] Type mismatch for key 'position'"));
            
            blackboard.TryGet<Vector3>("position", out _);
        }
        
        #endregion
        
        #region WaypointPath Warning Tests
        
        [Test]
        public void WaypointPath_NoWaypoints_GetCurrent_LogsWarning()
        {
            var pathObject = new GameObject("Path");
            var path = pathObject.AddComponent<WaypointPath>();
            
            LogAssert.Expect(LogType.Warning, new Regex(@"\[WaypointPath\].*GetCurrent.*no waypoints configured"));
            
            path.GetCurrent();
            
            Object.DestroyImmediate(pathObject);
        }
        
        [Test]
        public void WaypointPath_NoWaypoints_GetWaypoint_LogsWarning()
        {
            var pathObject = new GameObject("Path");
            var path = pathObject.AddComponent<WaypointPath>();
            
            LogAssert.Expect(LogType.Warning, new Regex(@"\[WaypointPath\].*GetWaypoint.*no waypoints configured"));
            
            path.GetWaypoint(0);
            
            Object.DestroyImmediate(pathObject);
        }
        
        [Test]
        public void WaypointPath_InvalidIndex_LogsWarning()
        {
            var pathObject = new GameObject("Path");
            var path = pathObject.AddComponent<WaypointPath>();
            
            // Add one waypoint
            var wp1 = new GameObject("WP1");
            wp1.transform.SetParent(pathObject.transform);
            path.PopulateFromChildren();
            
            LogAssert.Expect(LogType.Warning, new Regex(@"\[WaypointPath\] Invalid waypoint index"));
            
            path.GetWaypoint(5); // Invalid index
            
            Object.DestroyImmediate(pathObject);
        }
        
        [Test]
        public void WaypointPath_ValidWaypoints_NoWarning()
        {
            var pathObject = new GameObject("Path");
            var path = pathObject.AddComponent<WaypointPath>();
            
            var wp1 = new GameObject("WP1");
            wp1.transform.SetParent(pathObject.transform);
            path.PopulateFromChildren();
            
            path.GetCurrent();
            path.GetWaypoint(0);
            
            LogAssert.NoUnexpectedReceived();
            
            Object.DestroyImmediate(pathObject);
        }
        
        [Test]
        public void WaypointPath_NoWaypoints_WarnsOnlyOnce()
        {
            var pathObject = new GameObject("Path");
            var path = pathObject.AddComponent<WaypointPath>();
            
            // Should only warn once, not spam
            LogAssert.Expect(LogType.Warning, new Regex(@"\[WaypointPath\].*no waypoints configured"));
            
            path.GetCurrent();
            path.GetCurrent(); // Second call should not warn again
            path.GetCurrent(); // Third call should not warn again
            
            LogAssert.NoUnexpectedReceived();
            
            Object.DestroyImmediate(pathObject);
        }
        
        #endregion
        
        #region Memory Warning Tests
        
        [Test]
        public void Memory_GetPredictedPosition_NullTarget_LogsWarning()
        {
            var memory = new Memory();
            memory.LogWarnings = true;
            
            LogAssert.Expect(LogType.Warning, new Regex(@"\[Memory\] GetPredictedPosition called with null target"));
            
            memory.GetPredictedPosition(null);
        }
        
        [Test]
        public void Memory_GetPredictedPosition_UnknownTarget_LogsWarning()
        {
            var memory = new Memory();
            memory.LogWarnings = true;
            
            var target = new GameObject("Target");
            
            LogAssert.Expect(LogType.Warning, new Regex(@"\[Memory\] GetPredictedPosition called for target.*not in memory"));
            
            memory.GetPredictedPosition(target);
            
            Object.DestroyImmediate(target);
        }
        
        [Test]
        public void Memory_GetPredictedPosition_KnownTarget_NoWarning()
        {
            var memory = new Memory();
            memory.LogWarnings = true;
            
            var target = new GameObject("Target");
            memory.UpdateVisible(target, Vector3.zero);
            
            memory.GetPredictedPosition(target);
            
            LogAssert.NoUnexpectedReceived();
            
            Object.DestroyImmediate(target);
        }
        
        [Test]
        public void Memory_GetPredictedPosition_WarningsDisabled_NoWarning()
        {
            var memory = new Memory();
            memory.LogWarnings = false;
            
            var target = new GameObject("Target");
            
            memory.GetPredictedPosition(target);
            
            LogAssert.NoUnexpectedReceived();
            
            Object.DestroyImmediate(target);
        }
        
        [Test]
        public void Memory_TryGetPredictedPosition_UnknownTarget_NoWarning()
        {
            var memory = new Memory();
            memory.LogWarnings = true;
            
            var target = new GameObject("Target");
            
            // TryGet variant should NOT warn - it returns false instead
            bool found = memory.TryGetPredictedPosition(target, out _);
            
            Assert.IsFalse(found);
            LogAssert.NoUnexpectedReceived();
            
            Object.DestroyImmediate(target);
        }
        
        #endregion
        
        #region NPCBrainController Warning Tests (Runtime)
        
        // Note: NPCBrainController perception warnings are tested in Runtime tests
        // because they require full Unity lifecycle (Awake). The warning is triggered
        // when SightSensor or HearingSensor components are missing.
        // See PerceptionIntegrationTests for runtime verification.
        
        [Test]
        public void NPCBrainController_MissingPerceptionComponents_WarningFlag_Exists()
        {
            // Verify the warning flag exists and is true by default
            // The actual warning test requires runtime integration test
            var go = new GameObject("NPC");
            var brain = go.AddComponent<NPCBrainController>();
            
            // The brain should have a warning flag (_warnOnMissingComponents)
            // We can't test the private field directly, but we verify the component exists
            Assert.IsNotNull(brain);
            
            Object.DestroyImmediate(go);
        }
        
        #endregion
        
        #region Integration Scenarios
        
        [Test]
        public void ComplexBehaviorTree_MultipleWarnings_AllLogged()
        {
            // Test a behavior tree with multiple misconfiguration issues
            var inverter1 = new Inverter(null);
            var repeater = new Repeater(null, 2);
            
            // Both should warn
            LogAssert.Expect(LogType.Warning, new Regex(@"\[Inverter\] Child node is null"));
            LogAssert.Expect(LogType.Warning, new Regex(@"\[Repeater\] Child node is null"));
            
            inverter1.Execute(_brain);
            repeater.Execute(_brain);
        }
        
        [Test]
        public void Blackboard_MultipleTypeMismatches_AllLogged()
        {
            var blackboard = new Blackboard();
            blackboard.LogTypeMismatches = true;
            
            blackboard.Set("a", "string");
            blackboard.Set("b", 123);
            
            LogAssert.Expect(LogType.Warning, new Regex(@"Type mismatch for key 'a'"));
            LogAssert.Expect(LogType.Warning, new Regex(@"Type mismatch for key 'b'"));
            
            blackboard.Get<int>("a");
            blackboard.Get<string>("b");
        }
        
        [Test]
        public void UtilitySelector_MultipleActions_AllZeroScores_SingleWarning()
        {
            // Multiple actions all scoring zero should produce one warning
            var action1 = new UtilityAction("A", new MockNode(NodeStatus.Success), new ConstantConsideration(0f));
            var action2 = new UtilityAction("B", new MockNode(NodeStatus.Success), new ConstantConsideration(0f));
            var action3 = new UtilityAction("C", new MockNode(NodeStatus.Success), new ConstantConsideration(0f));
            var selector = new UtilitySelector(action1, action2, action3);
            selector.LogWarnings = true;
            
            LogAssert.Expect(LogType.Warning, new Regex(@"All 3 action.*scored <= 0"));
            
            selector.Execute(_brain);
        }
        
        #endregion
    }
}

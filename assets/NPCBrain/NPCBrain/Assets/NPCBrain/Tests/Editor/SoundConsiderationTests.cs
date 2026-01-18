using NUnit.Framework;
using UnityEngine;
using NPCBrain.UtilityAI;
using NPCBrain.Perception;
using NPCBrain.Tests;

namespace NPCBrain.Tests.Editor
{
    /// <summary>
    /// Unit tests for SoundConsideration classes.
    /// </summary>
    [TestFixture]
    public class SoundConsiderationTests
    {
        private GameObject _testObject;
        private TestBrain _brain;
        
        [SetUp]
        public void SetUp()
        {
            _testObject = new GameObject("TestNPC");
            _brain = _testObject.AddComponent<TestBrain>();
            _brain.InitializeForTests();
            
            // Clear any existing sounds
            SoundManager.ClearAll();
        }
        
        [TearDown]
        public void TearDown()
        {
            SoundManager.ClearAll();
            if (_testObject != null)
            {
                Object.DestroyImmediate(_testObject);
            }
        }
        
        #region SoundConsideration Tests
        
        [Test]
        public void SoundConsideration_NoSoundHeard_ReturnsZero()
        {
            var consideration = new SoundConsideration("TestSound");
            
            float score = consideration.Score(_brain);
            
            Assert.AreEqual(0f, score, 0.001f);
        }
        
        [Test]
        public void SoundConsideration_FootstepHeard_ReturnsLowScore()
        {
            _brain.Blackboard.Set("lastSoundType", (int)SoundType.Footstep);
            var consideration = new SoundConsideration("TestSound");
            
            float score = consideration.Score(_brain);
            
            // Footstep is low priority (1/7 normalized)
            Assert.Less(score, 0.3f);
            Assert.Greater(score, 0f);
        }
        
        [Test]
        public void SoundConsideration_GunshotHeard_ReturnsHighScore()
        {
            _brain.Blackboard.Set("lastSoundType", (int)SoundType.Gunshot);
            var consideration = new SoundConsideration("TestSound");
            
            float score = consideration.Score(_brain);
            
            // Gunshot is high priority (5/7 normalized)
            Assert.Greater(score, 0.5f);
        }
        
        [Test]
        public void SoundConsideration_AlarmHeard_ReturnsHighScore()
        {
            _brain.Blackboard.Set("lastSoundType", (int)SoundType.Alarm);
            var consideration = new SoundConsideration("TestSound");
            
            float score = consideration.Score(_brain);
            
            // Alarm is high priority (6/7 normalized)
            Assert.Greater(score, 0.7f);
        }
        
        [Test]
        public void SoundConsideration_ExplosionHeard_ReturnsMaxScore()
        {
            _brain.Blackboard.Set("lastSoundType", (int)SoundType.Explosion);
            var consideration = new SoundConsideration("TestSound");
            
            float score = consideration.Score(_brain);
            
            // Explosion is highest priority (7/7 normalized)
            Assert.AreEqual(1f, score, 0.001f);
        }
        
        #endregion
        
        #region HasHeardSoundConsideration Tests
        
        [Test]
        public void HasHeardSoundConsideration_NoSoundHeard_ReturnsZero()
        {
            var consideration = new HasHeardSoundConsideration("HasSound", SoundType.Footstep);
            
            float score = consideration.Score(_brain);
            
            Assert.AreEqual(0f, score, 0.001f);
        }
        
        [Test]
        public void HasHeardSoundConsideration_ExactMatch_ReturnsOne()
        {
            _brain.Blackboard.Set("lastSoundType", (int)SoundType.Gunshot);
            var consideration = new HasHeardSoundConsideration("HasGunshot", SoundType.Gunshot);
            
            float score = consideration.Score(_brain);
            
            Assert.AreEqual(1f, score, 0.001f);
        }
        
        [Test]
        public void HasHeardSoundConsideration_HigherPriority_ReturnsOne()
        {
            // Heard alarm, checking for footstep (alarm >= footstep)
            _brain.Blackboard.Set("lastSoundType", (int)SoundType.Alarm);
            var consideration = new HasHeardSoundConsideration("HasFootstep", SoundType.Footstep);
            
            float score = consideration.Score(_brain);
            
            Assert.AreEqual(1f, score, 0.001f);
        }
        
        [Test]
        public void HasHeardSoundConsideration_LowerPriority_ReturnsZero()
        {
            // Heard footstep, checking for gunshot (footstep < gunshot)
            _brain.Blackboard.Set("lastSoundType", (int)SoundType.Footstep);
            var consideration = new HasHeardSoundConsideration("HasGunshot", SoundType.Gunshot);
            
            float score = consideration.Score(_brain);
            
            Assert.AreEqual(0f, score, 0.001f);
        }
        
        #endregion
        
        #region SoundDistanceConsideration Tests
        
        [Test]
        public void SoundDistanceConsideration_NoInvestigatePosition_ReturnsZero()
        {
            var consideration = new SoundDistanceConsideration("SoundDist", 50f, true);
            
            float score = consideration.Score(_brain);
            
            Assert.AreEqual(0f, score, 0.001f);
        }
        
        [Test]
        public void SoundDistanceConsideration_AtSoundPosition_Inverted_ReturnsOne()
        {
            _testObject.transform.position = new Vector3(10f, 0f, 10f);
            _brain.Blackboard.Set("investigatePosition", new Vector3(10f, 0f, 10f));
            
            var consideration = new SoundDistanceConsideration("SoundDist", 50f, true);
            
            float score = consideration.Score(_brain);
            
            Assert.AreEqual(1f, score, 0.001f);
        }
        
        [Test]
        public void SoundDistanceConsideration_AtMaxDistance_Inverted_ReturnsZero()
        {
            _testObject.transform.position = Vector3.zero;
            _brain.Blackboard.Set("investigatePosition", new Vector3(50f, 0f, 0f));
            
            var consideration = new SoundDistanceConsideration("SoundDist", 50f, true);
            
            float score = consideration.Score(_brain);
            
            Assert.AreEqual(0f, score, 0.01f);
        }
        
        [Test]
        public void SoundDistanceConsideration_HalfDistance_Inverted_ReturnsHalf()
        {
            _testObject.transform.position = Vector3.zero;
            _brain.Blackboard.Set("investigatePosition", new Vector3(25f, 0f, 0f));
            
            var consideration = new SoundDistanceConsideration("SoundDist", 50f, true);
            
            float score = consideration.Score(_brain);
            
            Assert.AreEqual(0.5f, score, 0.01f);
        }
        
        [Test]
        public void SoundDistanceConsideration_NotInverted_CloserIsLower()
        {
            _testObject.transform.position = Vector3.zero;
            _brain.Blackboard.Set("investigatePosition", new Vector3(10f, 0f, 0f));
            
            var consideration = new SoundDistanceConsideration("SoundDist", 50f, false);
            
            float score = consideration.Score(_brain);
            
            // 10/50 = 0.2
            Assert.AreEqual(0.2f, score, 0.01f);
        }
        
        #endregion
    }
}

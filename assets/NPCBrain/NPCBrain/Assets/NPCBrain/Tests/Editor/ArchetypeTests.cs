using NUnit.Framework;
using UnityEngine;
using NPCBrain.Archetypes;

namespace NPCBrain.Tests.Editor
{
    /// <summary>
    /// Unit tests for NPC archetype classes.
    /// </summary>
    [TestFixture]
    public class ArchetypeTests
    {
        [SetUp]
        public void SetUp()
        {
            // Clear registries before each test
            NPCRegistry<CopNPC>.Clear();
            NPCRegistry<RobberNPC>.Clear();
        }
        
        [TearDown]
        public void TearDown()
        {
            NPCRegistry<CopNPC>.Clear();
            NPCRegistry<RobberNPC>.Clear();
        }
        
        #region CopNPC Registry Tests
        
        [Test]
        public void CopNPC_AllInstances_InitiallyEmpty()
        {
            Assert.AreEqual(0, CopNPC.AllInstances.Count);
        }
        
        [Test]
        public void CopNPC_RegistryIntegration_RegistersOnAwake()
        {
            // Note: Full integration test would require play mode
            // This just verifies the static property exists
            Assert.IsNotNull(CopNPC.AllInstances);
        }
        
        #endregion
        
        #region RobberNPC Registry Tests
        
        [Test]
        public void RobberNPC_AllInstances_InitiallyEmpty()
        {
            Assert.AreEqual(0, RobberNPC.AllInstances.Count);
        }
        
        [Test]
        public void RobberNPC_RegistryIntegration_RegistersOnAwake()
        {
            // Note: Full integration test would require play mode
            // This just verifies the static property exists
            Assert.IsNotNull(RobberNPC.AllInstances);
        }
        
        #endregion
        
        #region Interface Tests
        
        [Test]
        public void IAlertableNPC_Interface_ExistsOnCopNPC()
        {
            // Verify CopNPC implements IAlertableNPC
            Assert.IsTrue(typeof(IAlertableNPC).IsAssignableFrom(typeof(CopNPC)));
        }
        
        [Test]
        public void INPCArchetype_Interface_ExistsOnRobberNPC()
        {
            // Verify RobberNPC implements INPCArchetype
            Assert.IsTrue(typeof(INPCArchetype).IsAssignableFrom(typeof(RobberNPC)));
        }
        
        [Test]
        public void INPCArchetype_Interface_ExistsOnCopNPC()
        {
            // Verify CopNPC implements INPCArchetype (via IAlertableNPC)
            Assert.IsTrue(typeof(INPCArchetype).IsAssignableFrom(typeof(CopNPC)));
        }
        
        #endregion
    }
}

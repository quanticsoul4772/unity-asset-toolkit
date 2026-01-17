using NUnit.Framework;

namespace NPCBrain.Tests.Editor
{
    [TestFixture]
    public class BlackboardTests
    {
        private Blackboard _blackboard;
        
        [SetUp]
        public void SetUp()
        {
            _blackboard = new Blackboard();
        }
        
        [Test]
        public void Set_And_Get_ReturnsValue()
        {
            _blackboard.Set("health", 100);
            
            int result = _blackboard.Get<int>("health");
            
            Assert.AreEqual(100, result);
        }
        
        [Test]
        public void Get_MissingKey_ReturnsDefault()
        {
            int result = _blackboard.Get<int>("missing", 42);
            
            Assert.AreEqual(42, result);
        }
        
        [Test]
        public void Get_WrongType_ReturnsDefault()
        {
            _blackboard.Set("health", "not an int");
            
            int result = _blackboard.Get<int>("health", 42);
            
            Assert.AreEqual(42, result);
        }
        
        [Test]
        public void Has_ExistingKey_ReturnsTrue()
        {
            _blackboard.Set("health", 100);
            
            bool result = _blackboard.Has("health");
            
            Assert.IsTrue(result);
        }
        
        [Test]
        public void Has_MissingKey_ReturnsFalse()
        {
            bool result = _blackboard.Has("missing");
            
            Assert.IsFalse(result);
        }
        
        [Test]
        public void Remove_ExistingKey_ReturnsTrue()
        {
            _blackboard.Set("health", 100);
            
            bool result = _blackboard.Remove("health");
            
            Assert.IsTrue(result);
            Assert.IsFalse(_blackboard.Has("health"));
        }
        
        [Test]
        public void Remove_MissingKey_ReturnsFalse()
        {
            bool result = _blackboard.Remove("missing");
            
            Assert.IsFalse(result);
        }
        
        [Test]
        public void Clear_RemovesAllKeys()
        {
            _blackboard.Set("a", 1);
            _blackboard.Set("b", 2);
            
            _blackboard.Clear();
            
            Assert.IsFalse(_blackboard.Has("a"));
            Assert.IsFalse(_blackboard.Has("b"));
        }
        
        [Test]
        public void OnValueChanged_FiresWhenSet()
        {
            string changedKey = null;
            object changedValue = null;
            _blackboard.OnValueChanged += (key, value) =>
            {
                changedKey = key;
                changedValue = value;
            };
            
            _blackboard.Set("health", 100);
            
            Assert.AreEqual("health", changedKey);
            Assert.AreEqual(100, changedValue);
        }
        
        [Test]
        public void Set_StringValue_Works()
        {
            _blackboard.Set("name", "Guard");
            
            string result = _blackboard.Get<string>("name");
            
            Assert.AreEqual("Guard", result);
        }
        
        [Test]
        public void Set_FloatValue_Works()
        {
            _blackboard.Set("speed", 5.5f);
            
            float result = _blackboard.Get<float>("speed");
            
            Assert.AreEqual(5.5f, result, 0.001f);
        }
        
        [Test]
        public void Set_BoolValue_Works()
        {
            _blackboard.Set("isAlert", true);
            
            bool result = _blackboard.Get<bool>("isAlert");
            
            Assert.IsTrue(result);
        }
        
        [Test]
        public void TryGet_ExistingKey_ReturnsTrue()
        {
            _blackboard.Set("health", 100);
            
            bool result = _blackboard.TryGet<int>("health", out int value);
            
            Assert.IsTrue(result);
            Assert.AreEqual(100, value);
        }
        
        [Test]
        public void TryGet_MissingKey_ReturnsFalse()
        {
            bool result = _blackboard.TryGet<int>("missing", out int value);
            
            Assert.IsFalse(result);
            Assert.AreEqual(0, value);
        }
        
        [Test]
        public void TryGet_WrongType_ReturnsFalse()
        {
            _blackboard.Set("health", "not an int");
            
            bool result = _blackboard.TryGet<int>("health", out int value);
            
            Assert.IsFalse(result);
        }
        
        [Test]
        public void SetWithTTL_OnValueChanged_Fires()
        {
            string changedKey = null;
            _blackboard.OnValueChanged += (key, value) => changedKey = key;
            
            _blackboard.SetWithTTL("temp", 100, 5f);
            
            Assert.AreEqual("temp", changedKey);
        }
    }
}

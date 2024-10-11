using System.Collections.Generic;

namespace KC
{
    public class MqttTopicCache
    {
        private Dictionary<int, string> _i2s;

        private Dictionary<string, int> _s2i;

        public MqttTopicCache()
        {
            _i2s = new Dictionary<int, string>();
            _s2i = new Dictionary<string, int>();
        }
        
        public void SetCache(int topicType, string topicName)
        {
            _i2s.TryAdd(topicType, topicName);
            _s2i.TryAdd(topicName, topicType);
        }

        public string Get(int topicType)
        {
            return _i2s.GetValueOrDefault(topicType);
        }

        public int Get(string topicName)
        {
            return _s2i.GetValueOrDefault(topicName);
        }
    }
}
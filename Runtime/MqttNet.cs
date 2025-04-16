using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using MQTTnet;

namespace KC
{
    public class MqttNet : Singleton<MqttNet>,ISingletonAwake<Root>
    {
        private Root _root;
        
        public MqttFactory MqttFactory { get; private set; }

        public MqttTopicCache MqttTopicCache { get; private set; }
        
        
        public void Awake(Root root)
        {
            MqttFactory = new MqttFactory();
            MqttTopicCache = new MqttTopicCache();
            _root = root;
        }

        public MqttClientComponent GetOrAddMqttClient(int type)
        {
            if (type == 0)
            {
                throw new Exception("不允许使用0作为类型");
            }
            return _root.GetComponent<MqttClientComponent>(type) ?? _root.AddComponentWithId<MqttClientComponent>(type);
        }

        public MqttClientComponent GetMqttClient(int type)
        {
            if (type == 0)
            {
                throw new Exception("不允许使用0作为类型");
            }
            return _root.GetComponent<MqttClientComponent>(type);
        }
        
        public MqttSubscribeComponent GetOrAddSubscribeTopic(int clientType, int topicType)
        {
            if (topicType == 0)
            {
                throw new Exception("不允许使用0作为类型");
            }
            var client = _root.GetComponent<MqttClientComponent>(clientType);
            if (client == null)
            {
                return null;
            }
            return client.GetComponent<MqttSubscribeComponent>(topicType) ?? client.AddComponentWithId<MqttSubscribeComponent>(topicType);
        }

        public MqttPublishComponent GetOrAddPublishTopic(int clientType, string publishTopic)
        {
            var client = _root.GetComponent<MqttClientComponent>(clientType);
            if (client == null)
            {
                return null;
            }
            return client.GetOrAddPublishTopic(publishTopic);
        }

        public void AddSubscribeTopics(int clientType, IEnumerable<int> topicTypes)
        {
            _root.GetComponent<MqttClientComponent>(clientType)?.AddSubscribeTopics(topicTypes);
        }

        public Task Subscribe(int clientType, int topicType)
        {
            return GetOrAddSubscribeTopic(clientType, topicType).Subscribe();
        }
        
        public Task Unsubscribe(int clientType, int topicType)
        {
            return GetOrAddSubscribeTopic(clientType, topicType).Unsubscribe();
        }
        
        public Task PublishAsync(int clientType,string publishTopic,string content)
        {
            return GetOrAddPublishTopic(clientType, publishTopic).PublishAsync(content);
        }

        public Task PublishAsync(int clientType, string publishTopic, ArraySegment<byte> content)
        {
            return GetOrAddPublishTopic(clientType, publishTopic).PublishAsync(content);
        }

        public override void Destroy()
        {
            base.Destroy();
            MqttFactory = null;
            MqttTopicCache = null;
            _root = null;
        }
    }
}
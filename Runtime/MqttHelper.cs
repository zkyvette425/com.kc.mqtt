using System;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace KC
{
    public static partial class MqttHelper
    {
        public static MqttSubscribeComponent GetOrAddSubscribeTopic(this MqttClientComponent component, int topicType)
        {
            if (topicType == 0)
            {
                throw new Exception("不允许使用0作为类型");
            }
            return component.GetComponent<MqttSubscribeComponent>(topicType) ?? component.AddComponentWithId<MqttSubscribeComponent>(topicType);
        }
        
        public static void AddSubscribeTopics(this MqttClientComponent component, IEnumerable<int> topicTypes)
        {
            foreach (var topicType in topicTypes)
            {
                if (topicType == 0)
                {
                    throw new Exception("不允许使用0作为类型");
                }
                component.AddComponentWithId<MqttSubscribeComponent>(topicType);
            }
        }
        
        public static MqttPublishComponent GetOrAddPublishTopic(this MqttClientComponent component, string topicType)
        {
            var publishComponents = component.GetComponents<MqttPublishComponent>();
            foreach (var publishComponent in publishComponents)
            {
                if (publishComponent.Topic.Equals(topicType))
                {
                    return publishComponent;
                }
            }
            return component.AddComponent<MqttPublishComponent, string>(topicType);
        }
        
        public static Task Subscribe(this MqttClientComponent component, int topicType)
        {
            if (topicType == 0)
            {
                throw new Exception("不允许使用0作为类型");
            }
            return component.GetComponent<MqttSubscribeComponent>(topicType).Subscribe();
        }
        
        public static Task Unsubscribe(this MqttClientComponent component, int topicType)
        {
            if (topicType == 0)
            {
                throw new Exception("不允许使用0作为类型");
            }
            return component.GetComponent<MqttSubscribeComponent>(topicType).Unsubscribe();
        }
    }
}
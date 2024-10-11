using System;
using System.Threading.Tasks;
using MQTTnet;

namespace KC
{
    public class MqttPublishComponent : Component,IAwake<string>
    {
        private MqttApplicationMessageBuilder _builder;
        
        public string Topic { get; private set; }
        
        public void Awake(string topic)
        {
            Topic = topic;
            _builder = new MqttApplicationMessageBuilder().WithTopic(topic);
        }

        public Task PublishAsync(string content)
        {
            return ((MqttClientComponent)Parent).MqttClient.PublishAsync(_builder.WithPayload(content).Build());
        }
        
        public Task PublishAsync(ArraySegment<byte> content)
        {
            return ((MqttClientComponent)Parent).MqttClient.PublishAsync(_builder.WithPayload(content).Build());
        }

        /// <summary>
        /// 清理引用。
        /// </summary>
        public override void OnRecycle()
        {
            base.OnRecycle();
            _builder = null;
            Topic = null;
        }
    }
}
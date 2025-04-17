using System;
using System.Threading;
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

        public async Task PublishAsync(string content,CancellationToken cancellationToken = default)
        {
            var client = ((MqttClientComponent)Parent);
            if (client.MqttClient is not { IsConnected: true })
            {
                return;
            }
            await client.MqttClient.PublishAsync(_builder.WithPayload(content).Build(), cancellationToken);
        }
        
        public async Task PublishAsync(ArraySegment<byte> content,CancellationToken cancellationToken = default)
        {
            var client = ((MqttClientComponent)Parent);
            if (client.MqttClient is not { IsConnected: true })
            {
                return;
            }
            await client.MqttClient.PublishAsync(_builder.WithPayload(content).Build(), cancellationToken);
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
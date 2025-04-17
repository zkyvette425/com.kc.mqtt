using System;
using System.Threading;
using System.Threading.Tasks;
using MQTTnet.Packets;
using UnityEngine;

namespace KC
{
    public class MqttSubscribeComponent : Component,IAwake
    {
        private MqttTopicFilter _mqttTopicFilter;
        
        public string TopicName { get; private set; }
        
        public bool IsSubscribing { get; internal set; }
        
        public void Awake()
        {
            TopicName = MqttNet.Instance.MqttTopicCache.Get((int)Id);
            _mqttTopicFilter = new MqttTopicFilter() { Topic = TopicName };
            IsSubscribing = false;
        }

        public async Task Subscribe(CancellationToken cancellationToken = default)
        {
            if (IsSubscribing)
            {
                return;
            }

            var options = MqttNet.Instance.MqttFactory.CreateSubscribeOptionsBuilder().WithTopicFilter(_mqttTopicFilter)
                .Build();
            var client = ((MqttClientComponent)Parent);
            await client.MqttClient.SubscribeAsync(options, cancellationToken);
            IsSubscribing = true;
            client.Logger.Info("成功监听主题: {Id} - {TopicName}", Id, TopicName);
        }

        public async Task Unsubscribe(CancellationToken cancellationToken = default)
        {
            if (!IsSubscribing)
            {
                return;
            }
            var client = ((MqttClientComponent)Parent);
            try
            {
                var options = MqttNet.Instance.MqttFactory.CreateUnsubscribeOptionsBuilder()
                    .WithTopicFilter(_mqttTopicFilter)
                    .Build();
                await client.MqttClient.UnsubscribeAsync(options, cancellationToken);
                client.Logger.Info("取消监听主题:{Id} - {TopicName}", Id, TopicName);
            }
            catch (Exception e)
            {
                client.Logger.Warn("异常的取消监听主题:{Id} - {TopicName},{exce}", Id, TopicName, e.Message);
            }
            finally
            {
                IsSubscribing = false;
            }
        }

        /// <summary>
        /// 清理引用。
        /// </summary>
        public override void OnRecycle()
        {
            base.OnRecycle();
            TopicName = null;
            IsSubscribing = false;
            _mqttTopicFilter = null;
        }
    }
}
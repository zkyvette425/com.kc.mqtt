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

        public async Task Subscribe()
        {
            if (IsSubscribing)
            {
                return;
            }

            var options = MqttNet.Instance.MqttFactory.CreateSubscribeOptionsBuilder().WithTopicFilter(_mqttTopicFilter)
                .Build();
            await ((MqttClientComponent)Parent).MqttClient.SubscribeAsync(options);
            IsSubscribing = true;
            Debug.Log($"成功监听主题: {Id} - {TopicName}");
        }

        public async Task Unsubscribe()
        {
            if (IsSubscribing)
            {
                return;
            }

            var options = MqttNet.Instance.MqttFactory.CreateUnsubscribeOptionsBuilder().WithTopicFilter(_mqttTopicFilter)
                .Build();
            await ((MqttClientComponent)Parent).MqttClient.UnsubscribeAsync(options);
            IsSubscribing = false;
            Debug.Log($"取消监听主题:{Id} - {TopicName} ");
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
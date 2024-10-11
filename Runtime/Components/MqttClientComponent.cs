﻿using System;
using System.Text;
using System.Threading.Tasks;
using MQTTnet.Client;
using UnityEngine;

namespace KC
{
    public class MqttClientComponent : Component,IAwake,IDestroy
    {
        private IMqttClient _mqttClient;
        private MqttClientOptions _mqttClientOptions;

        internal IMqttClient MqttClient => _mqttClient;
        
        public event EventHandler<MqttReceivePacket> MqttReceive;
        public MqttClientOptionsBuilder ClientOptionsBuilder { get; set; }
        
        public void Awake()
        {
            _mqttClient = MqttNet.Instance.MqttFactory.CreateMqttClient();
            ClientOptionsBuilder = MqttNet.Instance.MqttFactory.CreateClientOptionsBuilder();
            _mqttClient.ConnectingAsync += MqttClientOnConnectingAsync;
            _mqttClient.ConnectedAsync += MqttClientOnConnectedAsync;
            _mqttClient.DisconnectedAsync += MqttClientOnDisconnectedAsync;
            _mqttClient.ApplicationMessageReceivedAsync += ClientOnApplicationMessageReceivedAsync;
        }

        public async Task Connect(string uri,int port,string clientId,TimeSpan keepAlivePeriod)
        {
            if (_mqttClient.IsConnected)
            {
                return;
            }
            
            _mqttClientOptions = ClientOptionsBuilder.WithTcpServer(uri, port).WithClientId(clientId)
                .WithKeepAlivePeriod(keepAlivePeriod).Build();
            await _mqttClient.ConnectAsync(_mqttClientOptions);
        }

        public async Task Connect()
        {
            _mqttClientOptions = ClientOptionsBuilder.Build();
            await _mqttClient.ConnectAsync(_mqttClientOptions);
        }
        
        private Task MqttClientOnDisconnectedAsync(MqttClientDisconnectedEventArgs arg)
        {
            Log($"MQTT客户端:{_mqttClient.Options.ClientId} 已断开连接,断开原因:{arg.Exception}");
            return Task.FromResult(true);
        }

        private Task MqttClientOnConnectedAsync(MqttClientConnectedEventArgs arg)
        {
            Log($"MQTT客户端:{_mqttClient.Options.ClientId} 已连接");
            return Task.FromResult(true);
        }

        private Task MqttClientOnConnectingAsync(MqttClientConnectingEventArgs arg)
        {
            Log($"MQTT客户端:{arg.ClientOptions.ClientId} 连接中");
            return Task.FromResult(true);
        }
        
        private Task ClientOnApplicationMessageReceivedAsync(MqttApplicationMessageReceivedEventArgs arg)
        {
            var message = Encoding.UTF8.GetString(arg.ApplicationMessage.PayloadSegment);
            var type = MqttNet.Instance.MqttTopicCache.Get(arg.ApplicationMessage.Topic);
            var packet = ReferencePool.Acquire<MqttReceivePacket>();
            packet.TopicType = type;
            packet.Message = message;
            MqttReceive?.Invoke(this,packet);
            Log($"MQTT客户端:{arg.ClientId} 监听主题类型:{arg.ApplicationMessage.Topic} 消息:{message}");
            return Task.FromResult(true);
        }

        private void Log(string message)
        {
            if (MqttNet.Instance.Log)
            {
                Debug.Log(message);
            }
        }

        public void Destroy()
        {
            _mqttClient.ConnectingAsync -= MqttClientOnConnectingAsync;
            _mqttClient.ConnectedAsync -= MqttClientOnConnectedAsync;
            _mqttClient.DisconnectedAsync -= MqttClientOnDisconnectedAsync;
            _mqttClient.ApplicationMessageReceivedAsync -= ClientOnApplicationMessageReceivedAsync;
            _mqttClient.DisconnectAsync();
        }

        /// <summary>
        /// 清理引用。
        /// </summary>
        public override void OnRecycle()
        {
            base.OnRecycle();
            MqttReceive = null;
            _mqttClient = null;
            _mqttClientOptions = null;
            ClientOptionsBuilder = null;
        }
    }
}
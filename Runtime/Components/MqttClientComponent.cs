using System;
using System.Linq;
using System.Threading.Tasks;
using MQTTnet.Client;
using NLog;

namespace KC
{
    public delegate Task ReconnectEventHandler();
    
    public class MqttClientComponent : Component,IAwake,IDestroy
    {
        private IMqttClient _mqttClient;
        private ILogger _logger;

        private MqttClientOptions _mqttClientOptions;

        public IMqttClient MqttClient => _mqttClient;
        
        public event EventHandler<MqttReceivePacket> MqttReceive;
        
        public event ReconnectEventHandler ReconnectEvent;
        
        public MqttClientOptionsBuilder ClientOptionsBuilder { get; set; }
        

        public bool IsCloseReceivedLog;
        
        public void Awake()
        {
            IsCloseReceivedLog = false;
            _mqttClient = MqttNet.Instance.MqttFactory.CreateMqttClient();
            ClientOptionsBuilder = MqttNet.Instance.MqttFactory.CreateClientOptionsBuilder();
            _mqttClient.ConnectingAsync += MqttClientOnConnectingAsync;
            _mqttClient.ConnectedAsync += MqttClientOnConnectedAsync;
            _mqttClient.DisconnectedAsync += MqttClientOnDisconnectedAsync;
            _mqttClient.ApplicationMessageReceivedAsync += ClientOnApplicationMessageReceivedAsync;
        }

        public async Task Connect(string uri, int port, string clientId, TimeSpan keepAlivePeriod)
        {
            if (_mqttClient.IsConnected)
            {
                return;
            }

            var logName = "MQTT Client " + clientId;
            KC.Log.Instance?.RegisterLogger(logName);
            _logger = LogManager.GetLogger(logName);
            _mqttClientOptions = ClientOptionsBuilder.WithTcpServer(uri, port).WithClientId(clientId).WithCleanStart()
                .WithKeepAlivePeriod(keepAlivePeriod).Build();
            await _mqttClient.ConnectAsync(_mqttClientOptions);
            AddReconnect();
        }

        public async Task Connect()
        {
            _mqttClientOptions = ClientOptionsBuilder.Build();
            var logName = "MQTT Client " + _mqttClientOptions.ClientId;
            KC.Log.Instance?.RegisterLogger(logName);
            _logger = LogManager.GetLogger(logName);
            await _mqttClient.ConnectAsync(_mqttClientOptions);
            AddReconnect();
        }
        
        private async Task MqttClientOnDisconnectedAsync(MqttClientDisconnectedEventArgs arg)
        {
            Log($"MQTT客户端:{_mqttClient.Options.ClientId} 已断开连接,断开原因:{arg.Exception}");
            var subscribeComponents = this.GetComponents<MqttSubscribeComponent>();
            if (subscribeComponents!=null)
            {
                var tasks = subscribeComponents.Select(component => component.Unsubscribe()).ToList();
                await Task.WhenAll(tasks);
            }
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
            var type = MqttNet.Instance.MqttTopicCache.Get(arg.ApplicationMessage.Topic);
            var packet = new MqttReceivePacket(type, arg.ApplicationMessage.PayloadSegment);
            MqttReceive?.Invoke(this,packet);
            if (!IsCloseReceivedLog)
            {
                Log($"MQTT客户端:{arg.ClientId} 监听主题类型:{arg.ApplicationMessage.Topic} 消息:{packet.Message}");
            }
            return Task.FromResult(true);
        }
        
        private void AddReconnect()
        {
            _ = Task.Run(async () =>
            {
                while (true)
                {
                    try
                    {
                        if (!await _mqttClient.TryPingAsync())
                        {
                            if (ReconnectEvent != null)
                            {
                                await ReconnectEvent();
                            }
                            await _mqttClient.ConnectAsync(_mqttClientOptions);
                        }
                    }
                    catch (Exception e)
                    {
                        if (!IsCloseReceivedLog)
                        {
                            Log($"MQTT客户端:{_mqttClient.Options.ClientId} 重连处理失败,{e}");
                        }
                        throw;
                    }
                }
            });
        }

        private void Log(string message)
        {
            _logger.Trace(message);
        }

        public void Destroy()
        {
            _mqttClient.ConnectingAsync -= MqttClientOnConnectingAsync;
            _mqttClient.ConnectedAsync -= MqttClientOnConnectedAsync;
            _mqttClient.DisconnectedAsync -= MqttClientOnDisconnectedAsync;
            _mqttClient.ApplicationMessageReceivedAsync -= ClientOnApplicationMessageReceivedAsync;
            _mqttClient.DisconnectAsync();
            KC.Log.Instance?.Remove(_logger.Name);
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
            IsCloseReceivedLog = false;
        }
    }
}
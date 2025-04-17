using System;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using MQTTnet.Client;
using NLog;

namespace KC
{
    public delegate Task ReconnectEventHandler();
    
    public class MqttClientComponent : Component,IAwake,IDestroy
    {
        private IMqttClient _mqttClient;
        private CancellationTokenSource _reconnectCts;
        private Task _reconnectTask;
        private MqttClientOptions _mqttClientOptions;

        public IMqttClient MqttClient => _mqttClient;
        
        public event EventHandler<MqttReceivePacket> MqttReceive;
        
        public event ReconnectEventHandler ReconnectEvent;
        
        public MqttClientOptionsBuilder ClientOptionsBuilder { get; set; }
        
        internal ILogger Logger { get; private set; }

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
            _reconnectCts = new CancellationTokenSource();
        }

        public async Task<MqttClientConnectResult> Connect(string uri, int port, string clientId,
            TimeSpan keepAlivePeriod,CancellationToken cancellationToken = default)
        {
            var logName = "MQTT Client " + clientId;
            KC.Log.Instance?.RegisterLogger(logName);
            Logger = LogManager.GetLogger(logName);
            _mqttClientOptions = ClientOptionsBuilder.WithTcpServer(uri, port).WithClientId(clientId).WithCleanStart()
                .WithKeepAlivePeriod(keepAlivePeriod).Build();
            return await _mqttClient.ConnectAsync(_mqttClientOptions, cancellationToken);
        }

        public async Task<MqttClientConnectResult> Connect(CancellationToken cancellationToken = default)
        {
            _mqttClientOptions = ClientOptionsBuilder.Build();
            var logName = "MQTT Client " + _mqttClientOptions.ClientId;
            Log.Instance?.RegisterLogger(logName);
            Logger = LogManager.GetLogger(logName);
            return await _mqttClient.ConnectAsync(_mqttClientOptions, cancellationToken);
        }
        
        public async Task<MqttClientConnectResult> TryConnect(string uri, int port, string clientId, TimeSpan keepAlivePeriod,CancellationToken cancellationToken = default)
        {
            if (_mqttClient.IsConnected)
            {
                return null;
            }
            return await Connect(uri, port, clientId, keepAlivePeriod, cancellationToken);
        }
        
        public async Task<MqttClientConnectResult> TryConnect(CancellationToken cancellationToken = default)
        {
            if (_mqttClient.IsConnected)
            {
                return null;
            }
            return await Connect(cancellationToken);
        }

        public void AddReconnect()
        {
            if (_reconnectTask != null)
            {
                return;
            }
            _reconnectCts = new CancellationTokenSource();
            _reconnectTask = Reconnect();
        }

        public void RemoveReconnect()
        {
            if (_reconnectTask == null)
            {
                return;
            }
            _reconnectCts.Cancel();
            _reconnectCts.Dispose();
            _reconnectCts = null;
            _reconnectTask = null;
        }
        
        private async Task MqttClientOnDisconnectedAsync(MqttClientDisconnectedEventArgs arg)
        {
            Logger.Info($"MQTT客户端:{_mqttClient.Options.ClientId} 已断开连接,断开原因:{arg.Exception}");
            var subscribeComponents = this.GetComponents<MqttSubscribeComponent>();
            if (subscribeComponents!=null)
            {
                var tasks = subscribeComponents.Select(component => component.Unsubscribe()).ToList();
                await Task.WhenAll(tasks);
            }
        }

        private Task MqttClientOnConnectedAsync(MqttClientConnectedEventArgs arg)
        {
            Logger.Info($"MQTT客户端:{_mqttClient.Options.ClientId} 已连接");
            return Task.FromResult(true);
        }

        private Task MqttClientOnConnectingAsync(MqttClientConnectingEventArgs arg)
        {
            Logger.Info($"MQTT客户端:{arg.ClientOptions.ClientId} 连接中");
            return Task.FromResult(true);
        }
        
        private Task ClientOnApplicationMessageReceivedAsync(MqttApplicationMessageReceivedEventArgs arg)
        {
            var type = MqttNet.Instance.MqttTopicCache.Get(arg.ApplicationMessage.Topic);
            var packet = new MqttReceivePacket(type, arg.ApplicationMessage.PayloadSegment);
            MqttReceive?.Invoke(this,packet);
            if (!IsCloseReceivedLog)
            {
                Logger.Trace($"MQTT客户端:{arg.ClientId} 监听主题类型:{arg.ApplicationMessage.Topic} 消息:{packet.Message}");
            }
            return Task.FromResult(true);
        }
        
        private async Task Reconnect()
        {
            while (!_reconnectCts.Token.IsCancellationRequested)
            {
                try
                {
                    if (!await _mqttClient.TryPingAsync(cancellationToken: _reconnectCts.Token))
                    {
                        if (ReconnectEvent != null)
                        {
                            await ReconnectEvent();
                        }
                        await _mqttClient.ConnectAsync(_mqttClientOptions, _reconnectCts.Token);
                    }
                }
                catch (Exception e)
                {
                    if (!IsCloseReceivedLog)
                    {
                        Logger.Warn($"MQTT客户端:{_mqttClient.Options.ClientId} 重连处理失败,{e}");
                    }
                }
            }
        }
        
        public void Destroy()
        {
            RemoveReconnect();
            _mqttClient.ConnectingAsync -= MqttClientOnConnectingAsync;
            _mqttClient.ConnectedAsync -= MqttClientOnConnectedAsync;
            _mqttClient.DisconnectedAsync -= MqttClientOnDisconnectedAsync;
            _mqttClient.ApplicationMessageReceivedAsync -= ClientOnApplicationMessageReceivedAsync;
            _mqttClient.DisconnectAsync();
            KC.Log.Instance?.Remove(Logger.Name);
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
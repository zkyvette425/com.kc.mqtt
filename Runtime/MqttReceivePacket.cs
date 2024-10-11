namespace KC
{
    public class MqttReceivePacket : IReference
    {
        public int TopicType { get; set; }
        
        public string Message { get; set; }

        /// <summary>
        /// 清理引用。
        /// </summary>
        public void OnRecycle()
        {
            TopicType = default;
            Message = null;
        }
    }
}
using System;
using System.Text;

namespace KC
{
    public class MqttReceivePacket : IReference
    {
        public int TopicType { get; set; }
        
        public ArraySegment<byte> Buffer { get; set; }
        
        public string Message => Encoding.UTF8.GetString(Buffer);
        
        /// <summary>
        /// 清理引用。
        /// </summary>
        public void OnRecycle()
        {
            TopicType = 0;
            Buffer = default;
        }
    }
}
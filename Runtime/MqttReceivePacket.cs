using System;
using System.Text;

namespace KC
{
    public readonly struct MqttReceivePacket
    {
        public readonly int TopicType;

        public readonly ArraySegment<byte> Buffer;
        
        public string Message => Encoding.UTF8.GetString(Buffer);

        internal MqttReceivePacket(int topicType, ArraySegment<byte> buffer)
        {
            TopicType = topicType;
            Buffer = buffer;
        }
    }
}
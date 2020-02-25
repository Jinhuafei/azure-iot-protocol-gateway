using DotNetty.Codecs.Mqtt.Packets;
using DotNetty.Transport.Channels;
using System;
using System.Collections.Generic;
using System.IO;
using System.Text;
using System.Threading.Tasks;

namespace Microsoft.Azure.Devices.ProtocolGateway.Core.ChannelHandlers
{
    public class MqttLoggingHandler : ChannelHandlerAdapter
    {
        public override void ChannelRead(IChannelHandlerContext context, object message)
        {
            var packet = message as PublishPacket;
            if (packet != null)
            {
                var buffer = packet.Payload;
                Console.WriteLine(buffer.ReadString(buffer.ReadableBytes, Encoding.ASCII));
            }
            context.FireChannelRead(message);
        }

        public override Task WriteAsync(IChannelHandlerContext context, object message)
        {
            var packet = message as PublishPacket;
            if (packet != null)
            {
                var buffer = packet.Payload;
                Console.WriteLine(buffer.ReadString(buffer.ReadableBytes, Encoding.ASCII));
            }
            return context.WriteAsync(message);
        }
    }
}

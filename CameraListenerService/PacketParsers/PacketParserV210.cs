using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using CameraListenerService.Data;

namespace CameraListenerService.PacketParsers
{
    class PacketParserV210 : PacketParserCommon
    {
        public override bool Parse(DBWriter dbWriter, byte[] packetData, int dataLength, ref bool moreDataExpected)
        {
            throw new NotImplementedException();
        }

        public override bool Validate(byte[] packetData, int dataLength, ref bool moreDataExpected)
        {
            throw new NotImplementedException();
        }

        public override byte[] getPacketACK_NACK()
        {
            throw new NotImplementedException();
        }
    }
}

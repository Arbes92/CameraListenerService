using System;
using CameraListenerService.Data;

namespace CameraListenerService.PacketParsers
{
    /// <summary>
    /// Packet Parser Interface
    /// </summary>
    public interface IPacketParser
    {
        /// <summary>
        /// Is continuous
        /// </summary>
        bool IsContinuous { get; }

        /// <summary>
        /// Is multi-chunk
        /// </summary>
        bool IsMultiChunk { get; }

        /// <summary>
        /// Default  timeout
        /// </summary>
        TimeSpan DefaultTimeout { get; }

        /// <summary>
        /// Tracker ID
        /// </summary>
        String TrackerID { get; set; }

        /// <summary>
        /// Parse a packet
        /// </summary>
        bool Parse(DBWriter dbWriter, byte[] packetData, int dataLength, ref bool moreDataExpected);

        /// <summary>
        /// Simple validate that the data looks sensible but don't write it to the DB
        /// </summary>
        /// <param name="packetData"></param>
        /// <param name="dataLength"></param>
        /// <param name="moreDataExpected"></param>
        /// <returns></returns>
        bool Validate(byte[] packetData, int dataLength, ref bool moreDataExpected);

        /// <summary>
        /// Returns a byte array containing the ACK/NACK packet that should be sent back to the vehicle
        /// </summary>
        /// <returns></returns>
        byte[] getPacketACK_NACK();
    }
}

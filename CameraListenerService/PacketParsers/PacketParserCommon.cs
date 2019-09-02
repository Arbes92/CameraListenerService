using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using CameraListenerService.Data;
using CameraListenerService.Utils;

namespace CameraListenerService.PacketParsers
{
    abstract class PacketParserCommon : IPacketParser
    {
        #region Constants
        private const string EventLogPostfix = ".PacketParserCommon";
        public const int PacketParserResult_OK = 0;
        public const int PacketParserResult_ParseFail = 1;
        public const int PacketParserResult_WriteFail = 2;
        #endregion

        #region Variables
        private readonly TimeSpan _defaultTimeout = new TimeSpan(0, 3, 0);
        public virtual TimeSpan DefaultTimeout { get { return _defaultTimeout; } }
        public String TrackerID { get; set; }
        public virtual bool IsContinuous
        {
            get { return false; }
        }
        public virtual bool IsMultiChunk
        {
            get { return false; }
        }

        #endregion

        #region Abstraction
        public abstract bool Parse(DBWriter dbWriter, byte[] packetData, int dataLength, ref bool moreDataExpected);

        public abstract bool Validate(byte[] packetData, int dataLength, ref bool moreDataExpected);

        public abstract byte[] getPacketACK_NACK();
        #endregion

        public PacketParserCommon()
        {
        }

        public static IPacketParser selectPacketParser(byte[] dataBuffer, int iRx)
        {
            IPacketParser packetParser = null;

            try
            {
                // Is the packet of a min sensible length, exact number to be defined.
                if (iRx >= 10)
                {
                    var start = 0;
                    var payloadStr = getString(dataBuffer, ref start, dataBuffer.Length);

                    if(string.IsNullOrEmpty(payloadStr))
                        return null; //it's not a string


                    var split = payloadStr.Split(',');
                    if (split.Length < 4)
                        return null; //too short packet or malformed

                    // If we understand how to parse this packet then create a parser for it.
                    switch (split[2].ToUpper())
                    {
                        case "V100":
                            return new PacketParserV100();
                        case "V101":
                            return new PacketParserV101();
                        case "V109":
                            return new PacketParserV109();
                        case "V114":
                            return new PacketParserV114();
                        case "V141":
                            return new PacketParserV141();
                        case "V210":
                            return new PacketParserV210();
                        case "V204":
                            return new PacketParserV204();
                        case "V201":
                            return new PacketParserV201();
                        default:
                            return null;
                    }
                }
            }
            catch (Exception e)
            {
                Logger.GetInstance().Exception("selectPacketParser ", e, EventLogPostfix);
            }

            return packetParser;
        }

        #region Parsing helpers
        protected byte[] getByteArrayFromString(String text)
        {
            int length = text.Length;
            byte[] ret = null;
            int i;

            try
            {
                ret = new Byte[length];

                for (i = 0; i < length; i++)
                {
                    ret[i] = (byte)text[i];
                }
            }
            catch (Exception e)
            {
                Logger.GetInstance().Exception("getByteArrayFromString ", e, EventLogPostfix);
            }

            return ret;
        }

        /// <summary>
        /// gets a double from an input field with exception handling
        /// </summary>
        /// <param name="field"></param>
        /// <returns></returns>
        protected double getDouble(String field)
        {
            double ret = 0.0;

            field = field.Trim();
            try
            {
                if (field.Length > 0)
                    ret = double.Parse(field);
            }
            catch (Exception e)
            {
                Logger.GetInstance().Exception("getDouble: " + field + " ", e, EventLogPostfix);
            }

            return ret;
        }

        /// <summary>
        /// Gets an int from an input field with exception handling
        /// </summary>
        /// <param name="field"></param>
        /// <returns></returns>
        protected int getInt(String field)
        {
            int ret = 0;

            field = field.Trim();
            try
            {
                if (field.Length > 0)
                {
                    ret = field.IndexOf('.') > 0 ? Convert.ToInt32(Convert.ToDouble(field)) : Convert.ToInt32(field);
                }
            }
            catch (Exception e)
            {
                Logger.GetInstance().Exception("getInt: " + field + " ", e, EventLogPostfix);
            }

            return ret;
        }

        /// <summary>
        /// Gets a short from an input field with exception handling
        /// </summary>
        /// <param name="field"></param>
        /// <returns></returns>
        protected short getShort(String field)
        {
            short ret = 0;

            field = field.Trim();
            try
            {
                if (field.Length > 0)
                    ret = short.Parse(field);
            }
            catch (Exception e)
            {
                Logger.GetInstance().Exception("getShort: " + field + " ", e, EventLogPostfix);
            }

            return ret;
        }

        /// <summary>
        /// Gets a byte with range checking
        /// </summary>
        /// <param name="field"></param>
        /// <returns></returns>
        protected byte getByte(String field)
        {
            byte ret = 0;

            field = field.Trim();
            try
            {
                if (field.Length > 0)
                    ret = byte.Parse(field);
            }
            catch (Exception e)
            {
                Logger.GetInstance().Exception("getbyte: " + field + " ", e, EventLogPostfix);
            }

            return ret;
        }

        /// <summary>
        /// Gets a DateTime with Range checking
        /// </summary>
        /// <param name="field"></param>
        /// <returns></returns>
        protected DateTime getDateTime(String field)
        {
            CultureInfo MyCultureInfo = new CultureInfo("en-GB");
            DateTime ret = DateTime.Parse("1996-01-02 01:02:03", MyCultureInfo);

            field = field.Trim();
            try
            {
                if (field.Length > 0)
                    ret = DateTime.Parse(field, MyCultureInfo);
            }
            catch (Exception e)
            {
                Logger.GetInstance().Exception("getDateTime: " + field + " ", e, EventLogPostfix);
            }
            return ret;
        }

        /// <summary>
        /// Gets a hex byte with range checking, no need to add leading 0x
        /// </summary>
        /// <param name="field"></param>
        /// <returns></returns>
        protected byte getByteHex(String field)
        {
            byte ret = 0;

            field = field.Trim();
            try
            {
                if (field.Length > 2)
                    ret = byte.Parse(field.Substring(0, 2), NumberStyles.HexNumber);
                else if (field.Length > 0)
                    ret = byte.Parse(field, NumberStyles.HexNumber);
            }
            catch (Exception e)
            {
                Logger.GetInstance().Exception("getbytehex: " + field + " ", e, EventLogPostfix);
            }

            return ret;
        }

        /// <summary>
        /// Gets a u_16 from a ascii hex string with range checking, no need to add leading 0x
        /// </summary>
        /// <param name="field"></param>
        /// <returns></returns>
        protected ushort getShortHex(String field)
        {
            ushort ret = 0;

            field = field.Trim();
            try
            {
                if (field.Length > 4)
                    ret = ushort.Parse(field.Substring(0, 4), NumberStyles.HexNumber);
                else if (field.Length > 0)
                    ret = ushort.Parse(field, NumberStyles.HexNumber);
            }
            catch (Exception e)
            {
                Logger.GetInstance().Exception("getshorthex: " + field + " ", e, EventLogPostfix);
            }

            return ret;
        }

        /// <summary>
        /// Gets a u_32 from a ascii hex string with range checking, no need to add leading 0x
        /// </summary>
        /// <param name="field"></param>
        /// <returns></returns>
        protected uint getIntHex(String field)
        {
            uint ret = 0;

            field = field.Trim();
            try
            {
                if (field.Length > 8)
                    ret = uint.Parse(field.Substring(0, 8), NumberStyles.HexNumber);
                else if (field.Length > 0)
                    ret = uint.Parse(field, NumberStyles.HexNumber);
            }
            catch (Exception e)
            {
                Logger.GetInstance().Exception("getinthex: " + field + " ", e, EventLogPostfix);
            }

            return ret;
        }

        /// <summary>
        /// swap the byte order of a u_16
        /// </summary>
        /// <param name="i"></param>
        /// <returns></returns>
        protected ushort swapShort(ushort i)
        {
            i = (ushort)(((i & (ushort)0x00ff) << 8) | ((i & (ushort)0xff00) >> 8));
            return i;
        }

        /// <summary>
        /// swap the byte order of a u_32
        /// </summary>
        /// <param name="i"></param>
        /// <returns></returns>
        protected uint swapInt(uint i)
        {
            i = (uint)(((i & (uint)0x000000ff) << 24) | ((i & (uint)0x0000ff00) << 8) | ((i & (uint)0x00ff0000) >> 8) | ((i & (uint)0xff000000) >> 24));
            return i;
        }

        protected static ushort getUShort(byte byte0, byte byte1)
        {
            ushort ret = (ushort)(byte0 | (byte1 << 8));
            return ret;
        }

        protected static short getShort(byte byte0, byte byte1)
        {
            short ret = (short)(byte0 | (byte1 << 8));
            return ret;
        }

        protected static uint getUInt(byte byte0, byte byte1, byte byte2)
        {
            uint ret = (uint)(byte0 | (byte1 << 8) | (byte2 << 16));
            return ret;
        }

        protected static uint getUInt(byte byte0, byte byte1, byte byte2, byte byte3)
        {
            uint ret = (uint)(byte0 | (byte1 << 8) | (byte2 << 16) | (byte3 << 24));
            return ret;
        }

        protected static ulong getULong(byte byte0, byte byte1, byte byte2, byte byte3,
                                        byte byte4, byte byte5, byte byte6, byte byte7)
        {
            ulong ret = (ulong)(((ulong)byte0) | (((ulong)byte1) << 8) | (((ulong)byte2) << 16) | (((ulong)byte3) << 24) | (((ulong)byte4) << 32) | (((ulong)byte5) << 40) | (((ulong)byte6) << 48) | (((ulong)byte7) << 56));
            return ret;
        }

        protected static int getInt(byte byte0, byte byte1, byte byte2, byte byte3)
        {
            int ret = (int)(byte0 | (byte1 << 8) | (byte2 << 16) | (byte3 << 24));
            return ret;
        }

        // convert a chunk of raw byte data to a string
        // packetData - byte array containing data
        // offset - start index in packetData for string conversion
        // additionalDataLength - how much data is left in packetData
        // stop when we get to a null
        // return the string and update the offset value
        protected static string getString(byte[] packetData, ref int startOffset, int endOffset)
        {
            if (packetData.Length < startOffset)
            {
                Logger.GetInstance().Warning(string.Format("getString request startOffset is outside the bounds of packet array. Suspected incorrect additional data length.\r\n\tPacket length: {0}\r\n\tStart offset: {1}\r\n\tEnd offset: {2}", packetData.Length, startOffset, endOffset), EventLogPostfix);
                startOffset = endOffset;
                return string.Empty;
            }
            if (packetData.Length < endOffset)
            {
                Logger.GetInstance().Warning(string.Format("getString request endOffset is outside the bounds of packet array. Suspected incorrect additional data length.\r\n\tPacket length: {0}\r\n\tStart offset: {1}\r\n\tEnd offset: {2}", packetData.Length, startOffset, endOffset), EventLogPostfix);
                startOffset = endOffset;
                return string.Empty;
            }

            // Convert additional data to a string
            String packetString = null;
            int additionalDataLength = endOffset - startOffset;

            try
            {
                // Convert byte array to a string
                // It's a crap way to do it but pointers require an 'unsafe' section
                char[] cData = new char[additionalDataLength + 1];
                int i = 0;

                while ((i < additionalDataLength) && (packetData[startOffset] != 0))
                {
                    cData[i] = (char)packetData[startOffset];
                    i++;
                    startOffset++;
                }
                startOffset++; // step the null
                cData[i] = '\x00'; // null terminate
                packetString = new String(cData);

                // Temp debug, dump the packet data to the event log
                //Logger.GetInstance().Message(packetString);
            }
            catch (Exception e)
            {
                Logger.GetInstance().Exception("getString ", e, EventLogPostfix);
            }

            return packetString;
        }
        #endregion
    }
}

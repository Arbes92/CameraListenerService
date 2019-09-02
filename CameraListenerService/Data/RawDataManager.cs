using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Runtime.Serialization;
using System.Runtime.Serialization.Formatters.Binary;
using System.Text;
using System.Threading.Tasks;
using CameraListenerService.Configuration;
using CameraListenerService.PacketParsers;
using CameraListenerService.Utils;

namespace CameraListenerService.Data
{
    /// <summary>
	/// This object is a singleton and manages a collection of raw data objects
	/// </summary>
	public class RawDataManager
    {
        private const string EventLogPostfix = ".RawDataManager";
        private static RawDataManager self = null;

        private List<RawData> rawData = null;
        private List<RawData> currentlyParsing = null;
        private List<RawData> failedParsing = null;

        private readonly object rawDataLock = null;
        private readonly object currentlyParsingLock = null;

        private bool weAreParsing = false;

        public int currentlyParsingIndex = 0;
        public TimeSpan previousParseDuration = new TimeSpan(0);
        public double previousParseDurationAverage = 0.0;

        private volatile bool closing = false;

        private ServiceConfigurationSection configuration;

        // construct
        public RawDataManager(ServiceConfigurationSection config)
        {
            rawData = new List<RawData>();
            currentlyParsing = new List<RawData>();
            failedParsing = new List<RawData>();

            rawDataLock = new object();
            currentlyParsingLock = new object();

            weAreParsing = false;

            configuration = config;
        }

        // get ref to this object
        public static RawDataManager GetInstance(ServiceConfigurationSection config)
        {
            if (self == null)
            {
                self = new RawDataManager(config);
            }
            return self;
        }

        /// <summary>
        /// Write un parsed data to disc
        /// </summary>
        public void Close()
        {
            //sets class level flag used to break out of any currently executing loops
            closing = true;
        }

        // add a new raw data object
        public void addNewRawData(byte[] data, int length)
        {
            addNewRawData(data, length, null);
        }

        // add a new raw data object
        public void addNewRawData(byte[] data, int length, String trackerID)
        {
            if (!closing)
            {
                RawData ret = new RawData(data, length, trackerID);
                // check sync lock on list and add object
                lock (rawDataLock)
                {
                    rawData.Add(ret);
                }
            }
        }

        // lock the collection and move all ready to be parsed data to a seperate collection
        public void moveRawData()
        {
            if (!closing)
            {
                lock (rawDataLock)
                {
                    lock (currentlyParsingLock)
                    {
                        //rawData.ForEach(delegate(RawData obj) { currentlyParsing.Add(obj); });
                        currentlyParsing.AddRange(rawData);
                        rawData.Clear();
                    }
                }
            }
        }

        // step ready to be parsed collection
        public void parseAllReadyData(DBWriter myDBWriter)
        {
            if (!closing)
            {
                DateTime start = DateTime.Now;
                var parseDurations = new List<double>();
                lock (currentlyParsingLock)
                {
                    lock (failedParsing)
                    {
                        currentlyParsingIndex = 0;

                        // Parse all data
                        currentlyParsing.ForEach(delegate (RawData obj)
                        {

                            try
                            {
                                Logger.GetInstance().WritePayloadToLogFile(obj.rawData, obj.rawDataLength, LogType.RawData);
                                Logger.GetInstance().WritePayloadBinaryToLogFile(obj.rawData, obj.rawDataLength, LogType.RawBinary);
                            }
                            catch (Exception ex)
                            {
                                Logger.GetInstance().Exception("Failed to write the log file", ex, EventLogPostfix);
                            }

                            bool moreDataExpected = false; // currently ignored
                            bool packetParsedOK = false;
                            IPacketParser packetParser = PacketParserCommon.selectPacketParser(obj.rawData, obj.rawDataLength);

                            currentlyParsingIndex++;

                            var parseStart = DateTime.Now;

                            if (packetParser != null)
                            {
                                packetParser.TrackerID = obj.trackerID;
                                packetParsedOK = packetParser.Parse(myDBWriter, obj.rawData, obj.rawDataLength, ref moreDataExpected);
                                obj.parsedOK = packetParsedOK;
                                if (!packetParsedOK)
                                    obj.parseRetryCount++;
                            }

                            parseDurations.Add((DateTime.Now - parseStart).TotalMilliseconds);
                        });

                        currentlyParsing.Clear();
                    }
                }
                previousParseDurationAverage = Average(parseDurations);
                previousParseDuration = DateTime.Now - start;
            }
        }
        private void ParseData(DBWriter myDBWriter, RawData obj, List<double> parseDurations)
        {
            bool moreDataExpected = false; // currently ignored
            bool packetParsedOK = false;
            IPacketParser packetParser = PacketParserCommon.selectPacketParser(obj.rawData, obj.rawDataLength);

            currentlyParsingIndex++;

            var parseStart = DateTime.Now;

            if (packetParser != null)
            {
                packetParser.TrackerID = obj.trackerID;
                packetParsedOK = packetParser.Parse(myDBWriter, obj.rawData, obj.rawDataLength, ref moreDataExpected);
                obj.parsedOK = packetParsedOK;
                if (!packetParsedOK)
                    obj.parseRetryCount++;
            }

            parseDurations.Add((DateTime.Now - parseStart).TotalMilliseconds);
        }

        public double Average(List<double> list)
        {
            if (list == null || list.Count == 0)
                return 0;

            var sum = 0.0;
            foreach (var l in list)
                sum += l;

            return Convert.ToInt32(sum / list.Count);
        }

        public void setWeAreParsing()
        {
            weAreParsing = true;
        }

        public void resetWeAreParsing()
        {
            weAreParsing = false;
        }

        /// <summary>
        /// Used by the main timer to check if we're still parsing the data from the last time that the timer fired
        /// </summary>
        /// <returns></returns>
        public bool areWeParsing()
        {
            return weAreParsing;
        }

        public void saveRawList(Stream myStream)
        {
            try
            {
                lock (rawDataLock)
                {
                    IFormatter formatter = new BinaryFormatter();
                    formatter.Serialize(myStream, rawData);
                }
            }
            catch (Exception e)
            {
                Logger.GetInstance().Exception("Save rawData: ", e, EventLogPostfix);
            }
        }

        // will overwite existing list rather than adding to it
        public void loadRawList(Stream myStream)
        {
            try
            {
                lock (rawDataLock)
                {
                    IFormatter formatter = new BinaryFormatter();
                    rawData = (List<RawData>)formatter.Deserialize(myStream);
                }
            }
            catch (Exception e)
            {
                Logger.GetInstance().Exception("Load rawData: ", e, EventLogPostfix);
            }
        }

        public void saveParsingList(Stream myStream)
        {
            try
            {
                // will never be able to get this lock as we have the list permenantly locked while we parse it
                //lock (currentlyParsingLock) 
                {
                    IFormatter formatter = new BinaryFormatter();
                    formatter.Serialize(myStream, rawData);
                }
            }
            catch (Exception e)
            {
                Logger.GetInstance().Exception("Save parsingData: ", e, EventLogPostfix);
            }
        }

        // will overwite existing list rather than adding to it
        public void loadParsingList(Stream myStream)
        {
            try
            {
                IFormatter formatter = new BinaryFormatter();
                currentlyParsing = (List<RawData>)formatter.Deserialize(myStream);
            }
            catch (Exception e)
            {
                Logger.GetInstance().Exception("Load currentlyParsing: ", e, EventLogPostfix);
            }
        }

        public void LogStatsToEventLog()
        { 
            // can't lock as we block the socket close/tidy process
            //lock (rawDataLock)
            {
                //lock (currentlyParsingLock)
                {
                    //lock (failedParsing)
                    {
                        /*RawDataManager (2.0.0) 
                         *  raw: 31000 
                         *  parsing: 34000 
                         *  parsingIndex: 15000 
                         *  failed: 0 
                         *  prevParseDur: 00:15:24
                         */
                        Logger.GetInstance().Message(
                            "RawDataManager (" +
                            ")\r\nRaw: " + rawData.Count.ToString() +
                            "\r\nParsing: " + currentlyParsing.Count.ToString() +
                            "\r\nParsing Index: " + currentlyParsingIndex.ToString() +
                            "\r\nFailed: " + failedParsing.Count.ToString() +
                            "\r\nPrevious Parse Duration: " + previousParseDuration.ToString() +
                            "\r\nPrevious Average Parse Duration: " + previousParseDurationAverage
                            , EventLogPostfix);
                    }
                }
            }
        }

        internal void LogStatsDataBase(int port, DBWriter myDBWriter)
        {

            try
            {
                myDBWriter.WriteParseLog(configuration, port,
                                         "ListenerService" + port,
                                         DateTime.Now,
                                         rawData.Count,
                                         currentlyParsing.Count,
                                         currentlyParsingIndex,
                                         Convert.ToInt32(previousParseDuration.TotalSeconds),
                                         previousParseDurationAverage);
            }
            catch (Exception ex)
            {
                Logger.GetInstance().Exception("Failed to write parse log to DB: ", ex, EventLogPostfix);
            }

        }
    }
}

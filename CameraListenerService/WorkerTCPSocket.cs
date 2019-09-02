using System;
using System.Collections.Generic;
using System.Linq;
using System.Net.Sockets;
using System.Text;
using System.Threading.Tasks;
using CameraListenerService.Configuration;
using CameraListenerService.Data;
using CameraListenerService.PacketParsers;
using CameraListenerService.Utils;

namespace CameraListenerService
{
    public class WorkerTCPSocket
    {
        private const string EventLogPostfix = ".WorkerTCPSocket";
        /// <summary>
        /// Link to the parser that has been selected for this connection
        /// </summary>
        private IPacketParser packetParser;
        private object parserlock = new object();

        private bool firstChunk;
        private bool moreChunksExpected;
        private byte[] dataBufferAllChunks;
        private int dataBufferIndex;
        private int emptyRxCount;
        private bool allChunksReceived;
        private int m_WorkerTCPSocketID;
        private ServiceConfigurationSection myConfig;

        public WorkerTCPSocket(ServiceConfigurationSection config, Socket tcpSocket, DBWriter dbWriter, RawDataManager rawDataManager)
        {
            m_WorkerTCPSocketID = new Random().Next();
            myDBWriter = dbWriter;
            myTCPSocket = tcpSocket;
            myRawDataManager = rawDataManager;
            myConfig = config;

            firstChunk = true;
            moreChunksExpected = false;
            dataBufferIndex = 0;
            emptyRxCount = 0;
            allChunksReceived = false;

            // Mark the age of this worker object
            connectionTime = DateTime.Now;

            packetParser = null;
        }

        /// <summary>
        /// Handle to TCP socket for this connection
        /// </summary>
        private Socket myTCPSocket;
        /// <summary>
        /// Handle to SQL DB connection object
        /// </summary>
        public DBWriter myDBWriter;
        public RawDataManager myRawDataManager;

        /// <summary>
        /// Sets the worker object waiting for data
        /// </summary>
        public void WaitForData()
        {
            try
            {
                if (pfnWorkerCallback == null)
                {
                    pfnWorkerCallback = new AsyncCallback(OnDataReceived);
                }

                dataBuffer = new byte[myConfig.TCPBufferSize]; //Needs to be bigger for F blobs
                myTCPSocket.BeginReceive(dataBuffer, 0, dataBuffer.Length, SocketFlags.None, pfnWorkerCallback, this);
            }
            catch (SocketException)
            {
                // ignore as likley to be forced closure by remote end.
                // We could destroy this object here but the List that manages the objects is not thread safe. Maybe later.
                // still we close socket
                myTCPSocket.Close(30); // wait 30 sec to send any unsent data
            }
            catch (Exception e)
            {
                Logger.GetInstance().Exception("Worker wait for data", e, EventLogPostfix);
                // We could destroy this object here but the List that manages the objects is not thread safe. Maybe later.
            }
        }

        /// <summary>
        /// Callback for rcv'd data
        /// </summary>
        private AsyncCallback pfnWorkerCallback = null;
        /// <summary>
        /// Received data buffer.  Max incoming packet size should be less than 600 bytes.
        /// </summary>
        private byte[] dataBuffer = null; // need new buffer for each rx new byte[1500];
                                          /// <summary>
                                          /// Time at which the connection created this worker object
                                          /// </summary>
        private DateTime connectionTime;

        private List<byte> moreDataBuffer = new List<byte>();

        /// <summary>
        /// Call back for received data.
        /// </summary>
        public void OnDataReceived(IAsyncResult asyn)
        {
            bool packetParsedOK = false;
            //Logger.GetInstance().Message(string.Format("ID: {0};", m_WorkerTCPSocketID));

            lock (parserlock)
            {
                bool moreDataExpected = false;

                // Mark the age of this worker object
                connectionTime = DateTime.Now;

                try
                {
                    // finish the rx process and get the count of bytes in buffer
                    int iRx = myTCPSocket.EndReceive(asyn);

                    try
                    {
                        Logger.GetInstance().WritePayloadToLogFile(dataBuffer, iRx, LogType.RawData);
                    }
                    catch (Exception ex)
                    {
                        Logger.GetInstance().Exception("Failed to write the log file", ex, EventLogPostfix);
                    }


                    if ((packetParser == null))
                    {
                        packetParser = PacketParserCommon.selectPacketParser(dataBuffer, iRx);
                    }

                    if (packetParser != null)
                    {
                        if (packetParser.IsContinuous)
                        {
                            if (packetParser.Validate(dataBuffer, iRx, ref moreDataExpected))
                                myRawDataManager.addNewRawData(dataBuffer, iRx, packetParser.TrackerID);
                            sendACK_NACK(packetParser.getPacketACK_NACK());
                            moreDataExpected = true;
                        }
                        else
                        {
                            if (firstChunk && packetParser.IsMultiChunk) // this should be a method of the selected packet parser that tells us if we need more data
                            {
                                moreChunksExpected = true;
                                // for now the only end condition for this is to allow the socket to timeout and save the data on socket close
                                // JL also doesn't have an ACK scheme here!
                            }

                            if (!moreChunksExpected)
                            {
                                // original behaviour before freeze frame data
                                packetParsedOK = packetParser.Validate(dataBuffer, iRx, ref moreDataExpected);
                                if (packetParsedOK)
                                {
                                    myRawDataManager.addNewRawData(dataBuffer, iRx);
                                }
                                sendACK_NACK(packetParser.getPacketACK_NACK());
                            }
                            else
                            {
                                //Logger.GetInstance().Warning(string.Format("ID: {0}; TCP, More Chunks; {1}", m_WorkerTCPSocketID, dataBufferIndex));
                                // more chunks expected
                                if (firstChunk)
                                {
                                    dataBufferAllChunks = new byte[32 * 1024]; //Needs to be bigger for F blobs
                                }
                                Array.Copy(dataBuffer, 0, dataBufferAllChunks, dataBufferIndex, iRx);
                                dataBufferIndex += iRx;

                                if (iRx == 0)
                                {
                                    emptyRxCount++;
                                    if (emptyRxCount > 10)
                                    {
                                        allChunksReceived = true;
                                    }
                                }
                            }
                        }
                    }
                    else
                    {
                        Logger.GetInstance().Message("OnReceiveData, no packet parser selected.", EventLogPostfix);
                    }
                }
                catch (ObjectDisposedException)
                {
                    // Socket was closed unexpectadly just ignore?
                    // Should we remove / dispose of worker object?
                }
                catch (SocketException se)
                {
                    if (se.ErrorCode == 10054) // error code for connection reset by peer
                    {
                        // no error just remove worker object?
                    }
                    else
                    {
                        Logger.GetInstance().Exception("OnReceiveData, socket error", se, EventLogPostfix);
                        // Should we remove / dispose of worker object?
                    }
                }
                catch (Exception e)
                {
                    Logger.GetInstance().Exception("OnDataReceive, general", e, EventLogPostfix);
                    // Should we remove / dispose of worker object?
                    // timer task ill tidy us up
                }
                finally
                {
                    firstChunk = false; // no longer first Chunk

                    if (moreChunksExpected)
                    {
                        // New method for large single packets from (freeze frame data)
                        if (allChunksReceived)
                        {
                            // all done
                            packetParsedOK = packetParser.Validate(dataBufferAllChunks, dataBufferIndex, ref moreDataExpected);
                            if (packetParsedOK)
                            {
                                myRawDataManager.addNewRawData(dataBufferAllChunks, dataBufferIndex);
                            }
                            sendACK_NACK(packetParser.getPacketACK_NACK());
                            // don't bother with command sending on FF data blobs
                            packetParser = null;
                            CloseAndDestroy();
                        }
                        else
                        {
                            WaitForData();
                        }
                    }
                    else if (moreDataExpected)
                    {
                        // old method for multi line packets from C-Series
                        WaitForData();
                    }
                    else
                    {
                        packetParser = null;
                        CloseAndDestroy();
                    }
                }
            }
        }

        /// <summary>
        /// send an ACK/NACK packet to the remote device
        /// </summary>
        public void sendACK_NACK(byte[] response)
        {
            try
            {
                // Send ACK byte
                if (response != null)
                    myTCPSocket.Send(response);
            }
            catch (Exception e)
            {
                Logger.GetInstance().Exception("Send ACK/NACK", e, EventLogPostfix);
            }
        }


        /// <summary>
        /// Close the socket and dispose of the worker object
        /// </summary>
        public void CloseAndDestroy()
        {
            if (myTCPSocket != null)
            {
                try
                {
                    myTCPSocket.Close();
                }
                catch (Exception)
                {
                    // socket may already have been closed
                }
                myTCPSocket = null;
            }

            packetParser = null;

            // request removal from the worker object array list so that we get gc'd
            CameraListenerService.RemoveWorkerTCPSocket(this);
        }

        /// <summary>
        /// Returns the age of this worker socket
        /// </summary>
        public TimeSpan GetAge()
        {
            TimeSpan diff = DateTime.Now - connectionTime;
            
            return diff;
        }

        public Boolean IsOverAge
        {
            get { return packetParser != null && GetAge() > TimeSpan.FromSeconds(myConfig.SocketInactivityTimeout); }
        }

        public void EventLogDumpPacketRaw(byte[] packetData, int dataLength)
        {
            // Translate data bytes to a ASCII string.
            String data = " ";
            int j;

            for (j = 0; j < dataLength; j++)
            {
                data += packetData[j].ToString("X2");
                data += " ";
            }
            Logger.GetInstance().Message(data, EventLogPostfix);
            //Console.WriteLine(String.Format("Received: {0}", data));
        }
    }
}

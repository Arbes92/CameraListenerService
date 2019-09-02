using System;
using System.Collections;
using System.Collections.Generic;
using System.ComponentModel;
using System.Configuration;
using System.Data;
using System.Diagnostics;
using System.Linq;
using System.Net;
using System.Net.Sockets;
using System.ServiceProcess;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using CameraListenerService.Configuration;
using CameraListenerService.Data;
using CameraListenerService.Utils;

namespace CameraListenerService
{
    public partial class CameraListenerService : ServiceBase
    {
        #region Variables

        internal ServiceConfigurationSection Configuration { get; set; }

        private Socket m_MainTCPSocket = null;
        private DBWriter m_DBWriter = null;
        private RawDataManager m_RawDataManager = null;
        private static ArrayList m_WorkerTCPSocketList = ArrayList.Synchronized(new ArrayList());

        private volatile bool closing = false;
        private volatile bool exitontcpconnect = false;
        private volatile bool waitingfortcp = true;

        #endregion

        #region c-tor, OnStart and OnStop

        public CameraListenerService()
        {
            InitializeComponent();
            Configuration =
                (ServiceConfigurationSection) ConfigurationManager.GetSection("CameraListenerService.Configs");
            Logger.GetInstance().SetSuffixForFlatFileLogging(Configuration.PortNumberData.ToString());
        }

        protected override void OnStart(string[] args)
        {
            try
            {
                Timer_Cleanup.Interval = Configuration.CleanupTimerInterval;
                Timer_Cleanup.Enabled = true;


                Timer_Parse.Interval = Configuration.ParseTimerInterval;
                Timer_Parse.Enabled = true;

                m_DBWriter = new DBWriter();
                m_RawDataManager = RawDataManager.GetInstance(Configuration);

                var ipLocalTCP = new IPEndPoint(IPAddress.Any, Configuration.PortNumberData);
                m_MainTCPSocket = new Socket(AddressFamily.InterNetwork, SocketType.Stream, ProtocolType.Tcp);

                m_MainTCPSocket.Bind(ipLocalTCP);
                m_MainTCPSocket.Listen(Configuration
                    .PendingConnectionQueue); // param is backlog of connections to allow
                m_MainTCPSocket.BeginAccept(new AsyncCallback(OnTCPConnect), null);
            }
            catch (Exception e)
            {
                Logger.GetInstance().Exception("Failed to create TCP listener Socket", e, string.Empty);
            }
        }

        protected override void OnStop()
        {
            //set class level closing flag used to refuse any new UDP/TCP connections
            closing = true;

            //wait for timers to run and to allow the workertcpsockets to process any outstanding requests
            var shorttimeout = 5000;
            Thread.Sleep(shorttimeout);

            //disable the timers
            Timer_Cleanup.Enabled = false;
            Timer_Parse.Enabled = false;
            //this.timer1.Enabled = false;
            //this.timer2.Enabled = false;
            //this.timer3.Enabled = false;
            //this.timer4.Enabled = false;

            //set worker tcp socket max age to 30 seconds
            TimeSpan maxAge = new TimeSpan(0, 0, 30); // 30 seconds

            long waiting = Environment.TickCount;
            try
            {
                //while loop to break when socket count reaches 0 or 30s timeout expires
                while (m_WorkerTCPSocketList.Count > 0 && Environment.TickCount - waiting <= 30000)
                {
                    WorkerTCPSocket workerObj = null;
                    foreach (WorkerTCPSocket worker in m_WorkerTCPSocketList)
                    {
                        if (worker.GetAge() > maxAge)
                        {
                            workerObj = worker;
                            break;
                        }
                    }

                    //check to see if socket is too old
                    if (workerObj != null)
                    {
                        try
                        {
                            workerObj.CloseAndDestroy();
                        }
                        catch (Exception excep)
                        {
                            Logger.GetInstance().Exception("Exception on TCP Worker disposal: ", excep, string.Empty);
                        }

                        Thread.Sleep(100);
                    }
                    else
                    {
                        Thread.Sleep(1000);
                    }
                }
            }
            catch (Exception excep)
            {
                Logger.GetInstance().Exception("Exception on age check foreach: ", excep, string.Empty);
            }

            //close main Socket Handler
            if (m_MainTCPSocket != null)
            {
                //if waiting for tcp socket to finish or pending connection
                if (!exitontcpconnect && !waitingfortcp)
                {
                    Thread.Sleep(shorttimeout);
                }

                m_MainTCPSocket.Close();
            }

            //purge and close the raw data manager
            if (m_RawDataManager != null)
            {
                //force all pending parses to be parsed
                m_RawDataManager.moveRawData();
                m_RawDataManager.parseAllReadyData(m_DBWriter);
                m_RawDataManager.Close();
            }

            //close the db writer
            m_DBWriter?.Close();
        }

        #endregion

        #region TCP

        public void OnTCPConnect(IAsyncResult asyn)
        {
            if (closing)
            {
                exitontcpconnect = true;
                return;
            }

            Socket newConnection = null;

            // Accept incoming connection, create a worker object and add to arraylist of workers.
            try
            {
                waitingfortcp = false;
                newConnection = m_MainTCPSocket.EndAccept(asyn);
                newConnection.NoDelay = true;
                var newWorker = new WorkerTCPSocket(Configuration, newConnection, m_DBWriter, m_RawDataManager);
                m_WorkerTCPSocketList.Add(newWorker);
                newWorker.WaitForData();
            }
            catch (ObjectDisposedException)
            {
                // Socket was unexpectedly closed, so just ignore
            }
            catch (Exception e)
            {
                try
                {
                    Logger.GetInstance().Exception("Incoming TCP Connection error", e, string.Empty);
                    if (newConnection != null)
                        newConnection.Close();
                }
                catch (Exception e2)
                {
                    Logger.GetInstance().Exception("Incoming TCP Connection error, failed to close", e2, string.Empty);
                }
            }

            waitingfortcp = true;

            // Start listening for the next connection
            try
            {
                m_MainTCPSocket.BeginAccept(new AsyncCallback(OnTCPConnect), null);
            }
            catch (Exception e3)
            {
                Logger.GetInstance().Exception("Listen for new TCP connection error", e3, string.Empty);
                // this would be fatal as it would totally block the socket.
                // should we just force an exit here and let windows restart the service?
                // or should we destroy the mainSocket and start again?
            }
        }

        /// <summary>
        /// Removes a worker object from the array list
        /// </summary>
        public static void RemoveWorkerTCPSocket(WorkerTCPSocket worker)
        {
            m_WorkerTCPSocketList.Remove(worker);
        }

        #endregion

        #region Timers

        private void Timer_Cleanup_Tick(object sender, EventArgs e)

        {
            if (closing)
            {
                Timer_Cleanup.Enabled = false;
                return;
            }

            Logger.GetInstance().Message("Performing cleanup. TCP Socket Count: " + m_WorkerTCPSocketList.Count, string.Empty);

            m_RawDataManager.LogStatsToEventLog();
            m_RawDataManager.LogStatsDataBase(Configuration.PortNumberData, m_DBWriter);

            // Process workerTCPSocketList and check age of all sockets, any more than 2 min old get destroyed.
            TimeSpan maxAge = new TimeSpan(0, 2, 0); // 2 minutes
            int count = 0;
            try
            {
                int listSize = m_WorkerTCPSocketList.Count;
                while (count < listSize)
                {
                    var workerObj = (WorkerTCPSocket)m_WorkerTCPSocketList[count];

                    if (workerObj.IsOverAge)
                    {
                        try
                        {
                            // This modifies the collection and screws the enumerator
                            workerObj.CloseAndDestroy(); // will remove obj form list
                            // don't inc count as array entries will have shifted
                            // however dec list size as we've just deleted an object
                            listSize--;
                        }
                        catch (Exception excep)
                        {
                            Logger.GetInstance().Exception("Exception on TCP Worker disposal: ", excep, string.Empty);
                            count++; // fail so skip this object
                        }
                    }
                    else
                    {
                        // just step over this object
                        count++;
                    }
                }
            }
            catch (Exception excep)
            {
                Logger.GetInstance().Exception("Exception on age check foreach: ", excep, string.Empty);
            }

            Logger.GetInstance().Message("Cleanup performed. TCP Socket Count: " + m_WorkerTCPSocketList.Count, string.Empty);
        }


        private void Timer_Parse_Tick(object sender, EventArgs e)
        {
            if (closing)
            {
                Timer_Parse.Enabled = false;
                return;
            }
            if (!m_RawDataManager.areWeParsing())
            {
                m_RawDataManager.setWeAreParsing();
                m_RawDataManager.moveRawData();
                m_RawDataManager.parseAllReadyData(m_DBWriter);
                m_RawDataManager.resetWeAreParsing();
            }
        }

        #endregion
    }
}

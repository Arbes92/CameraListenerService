using System;
using System.Collections.Generic;
using System.Data;
using System.Data.SqlClient;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using CameraListenerService.Configuration;
using CameraListenerService.Utils;

namespace CameraListenerService.Data
{
    /// <summary>
    /// Write new data into the database
    /// </summary>
    public class DBWriter
    {
        private const string EventLogPostfix = ".DBWriter";

        public DBWriter()
        {
        }

        public void Close()
        {

        }

        public void WriteParseLog(ServiceConfigurationSection config, int port, string listenerName, DateTime logDateTime, int rawCount, int curParseSize,
                                  int curParseIndex, int prevParseDuration, double prevParseDurationAvg)
        {
            try
            {
                using (var mySqlConnection = new SqlConnection(config.DataConnectionString.ConnectionString))
                {
                    try
                    {
                        mySqlConnection.Open();
                    }
                    catch (Exception e)
                    {
                        Logger.GetInstance().Exception("Open Sql connection failed (WriteParseLog): ", e, EventLogPostfix);
                        return;
                    }

                    try
                    {
                        const string mySQLCommandString = "proc_WriteListenerParseLog";

                        var myCommand = new SqlCommand(mySQLCommandString, mySqlConnection)
                        {
                            CommandType = CommandType.StoredProcedure
                        };

                        try
                        {
                            SqlParameter paramPort = myCommand.Parameters.Add("@port", SqlDbType.Int);
                            paramPort.Value = port;

                            SqlParameter paramName = myCommand.Parameters.Add("@lname", SqlDbType.VarChar, 32);
                            paramName.Value = listenerName;

                            SqlParameter paramTime = myCommand.Parameters.Add("@datetime", SqlDbType.DateTime);
                            paramTime.Value = logDateTime;

                            SqlParameter paramRawCount = myCommand.Parameters.Add("@raw", SqlDbType.Int);
                            paramRawCount.Value = rawCount;

                            SqlParameter paramCurParseSize = myCommand.Parameters.Add("@parsing", SqlDbType.Int);
                            paramCurParseSize.Value = curParseSize;

                            SqlParameter paramCurParseIndex = myCommand.Parameters.Add("@parseindex", SqlDbType.Int);
                            paramCurParseIndex.Value = curParseIndex;

                            SqlParameter paramPrevParseDuration = myCommand.Parameters.Add("@prevduration", SqlDbType.Int);
                            paramPrevParseDuration.Value = prevParseDuration;

                            SqlParameter paramPrevParseDurationAvg = myCommand.Parameters.Add("@prevdurationAvg", SqlDbType.Float);
                            paramPrevParseDurationAvg.Value = prevParseDurationAvg;


                            SqlParameter paramPreviousParseDurationAverageEvents = myCommand.Parameters.Add("@prevdurationAvgEvents", SqlDbType.Float);
                            paramPreviousParseDurationAverageEvents.Value = 0;

                            SqlParameter paramPreviousParseDurationAverageAccums = myCommand.Parameters.Add("@prevdurationAvgAccums", SqlDbType.Float);
                            paramPreviousParseDurationAverageAccums.Value = 0;

                            SqlParameter paramPreviousParseDurationAverageOther = myCommand.Parameters.Add("@prevdurationAvgOther", SqlDbType.Float);
                            paramPreviousParseDurationAverageOther.Value = 0;
                        }
                        catch (Exception e)
                        {
                            Logger.GetInstance().Exception("DB write failed (WriteParseLog)(): ", e, EventLogPostfix);
                            return;
                        }

                        try
                        {
                            myCommand.ExecuteNonQuery();
                        }
                        catch (Exception e)
                        {
                            Logger.GetInstance().Exception("DB write failed (" + mySQLCommandString + ")(execute)(" + port + "): ", e, EventLogPostfix);
                        }
                    }
                    catch (Exception e)
                    {
                        Logger.GetInstance().Exception("DB write failed (WriteParseLog): ", e, EventLogPostfix);
                    }
                }
            }
            catch (Exception e)
            {
                Logger.GetInstance().Exception("SQL Connection creation failed (WriteParseLog): ", e, EventLogPostfix);
            }
        }

        public Int64 WriteEventNew(ServiceConfigurationSection config, string trackerID, string driverID, int creationCode,
                                double latFloat, double lonFloat, short headingShort, short speedByte,
                                int odoGPS, int odoRoadspeed, int odoDash,
                                DateTime dateTime, byte digitalsByte,
                                Int16 analog0, Int16 analog1, Int16 analog2,
                                Int16 analog3, Int16 analog4, Int16 analog5,
                                UInt32 sequenceNumber,
                                string additionalDataString, string additionalDataName,
                                short altitude, byte gpssatellitecount, byte gprssignalstrength,
                                byte systemstatus, byte batterychargelevel, byte externalinputvoltage,
                                short maxspeed,
                                int tripDistance, byte tachoStatus, byte canStatus, byte fuelLevel, byte hardwareStatus,
                                ref int customerId, ref Guid vehicleId)
        {
            // DJ - attempt to fix future or long past dates, commented out not to hid the development cockups
            //if (dateTime > DateTime.UtcNow.AddDays(1) || dateTime < DateTime.UtcNow.AddDays(-30))
            //    dateTime = DateTime.UtcNow;

            // initialise return values to defaults
            customerId = 0;
            vehicleId = Guid.Empty;

            try
            {
                using (SqlConnection mySqlConnection = new SqlConnection(config.DataConnectionString.ConnectionString))
                {
                    try
                    {
                        mySqlConnection.Open();
                    }
                    catch (Exception e)
                    {
                        Logger.GetInstance().Exception("Open Sql connection failed (eventnew): ", e, EventLogPostfix);
                        return 0;
                    }

                    try
                    {
                        string mySQLCommandString = "proc_WriteEventNewNonIdTemp";

                        SqlCommand myCommand = new SqlCommand(mySQLCommandString, mySqlConnection);
                        myCommand.CommandType = CommandType.StoredProcedure;
                        //myCommand.CommandTimeout = 120;

                        try
                        {
                            SqlParameter trackerid = myCommand.Parameters.Add("@trackerid", SqlDbType.VarChar, 50);
                            trackerid.Value = trackerID;
                        }
                        catch (Exception e)
                        {
                            Logger.GetInstance().Exception("DB write failed (eventnew)(trackerid): ", e, EventLogPostfix);
                            return 0;
                        }

                        try
                        {
                            SqlParameter driverid = myCommand.Parameters.Add("@driverid", SqlDbType.VarChar, 32);
                            driverid.Value = driverID;
                        }
                        catch (Exception e)
                        {
                            Logger.GetInstance().Exception("DB write failed (eventnew)(driverid): ", e, EventLogPostfix);
                            return 0;
                        }

                        try
                        {
                            SqlParameter ccid = myCommand.Parameters.Add("@ccid", SqlDbType.SmallInt);
                            ccid.Value = RCSmallInt(creationCode);
                        }
                        catch (Exception e)
                        {
                            Logger.GetInstance().Exception("DB write failed (eventnew)(ccid): ", e, EventLogPostfix);
                            return 0;
                        }

                        try
                        {
                            // SQL float types are actually doubles
                            SqlParameter longitude = myCommand.Parameters.Add("@long", SqlDbType.Float);
                            longitude.Value = lonFloat;
                            SqlParameter latitude = myCommand.Parameters.Add("@lat", SqlDbType.Float);
                            latitude.Value = latFloat;
                        }
                        catch (Exception e)
                        {
                            Logger.GetInstance().Exception("DB write failed (eventnew)(lat/lon): ", e, EventLogPostfix);
                            return 0;
                        }

                        try
                        {
                            SqlParameter heading = myCommand.Parameters.Add("@heading", SqlDbType.SmallInt);
                            heading.Value = RCSmallInt(headingShort);
                        }
                        catch (Exception e)
                        {
                            Logger.GetInstance().Exception("DB write failed (eventnew)(heading): ", e, EventLogPostfix);
                            return 0;
                        }

                        try
                        {
                            SqlParameter speed = myCommand.Parameters.Add("@speed", SqlDbType.SmallInt);
                            speed.Value = RCSmallInt((short)speedByte);
                        }
                        catch (Exception e)
                        {
                            Logger.GetInstance().Exception("DB write failed (eventnew)(speed): ", e, EventLogPostfix);
                            return 0;
                        }

                        try
                        {
                            SqlParameter odogps = myCommand.Parameters.Add("@odogps", SqlDbType.Int);
                            odogps.Value = odoGPS;
                            SqlParameter odoroadspeed = myCommand.Parameters.Add("@odoroadspeed", SqlDbType.Int);
                            odoroadspeed.Value = odoRoadspeed;
                            SqlParameter ododash = myCommand.Parameters.Add("@ododash", SqlDbType.Int);
                            ododash.Value = odoDash;
                        }
                        catch (Exception e)
                        {
                            Logger.GetInstance().Exception("DB write failed (eventnew)(odos): ", e, EventLogPostfix);
                            return 0;
                        }

                        try
                        {
                            SqlParameter eventdt = myCommand.Parameters.Add("@eventdt", SqlDbType.DateTime);
                            eventdt.Value = dateTime;
                        }
                        catch (Exception e)
                        {
                            Logger.GetInstance().Exception("DB write failed (eventnew)(datetime): ", e, EventLogPostfix);
                            return 0;
                        }

                        try
                        {
                            // TinyInt is a 8bit unsigned value
                            SqlParameter dio = myCommand.Parameters.Add("@dio", SqlDbType.TinyInt);
                            dio.Value = RCTinyInt(digitalsByte);
                        }
                        catch (Exception e)
                        {
                            Logger.GetInstance().Exception("DB write failed (eventnew)(dio): ", e, EventLogPostfix);
                            return 0;
                        }

                        try
                        {
                            SqlParameter an0 = myCommand.Parameters.Add("@analog0", SqlDbType.SmallInt);
                            an0.Value = analog0;
                            SqlParameter an1 = myCommand.Parameters.Add("@analog1", SqlDbType.SmallInt);
                            an1.Value = analog1;
                            SqlParameter an2 = myCommand.Parameters.Add("@analog2", SqlDbType.SmallInt);
                            an2.Value = analog2;
                            SqlParameter an3 = myCommand.Parameters.Add("@analog3", SqlDbType.SmallInt);
                            an3.Value = analog3;
                            SqlParameter an4 = myCommand.Parameters.Add("@analog4", SqlDbType.SmallInt);
                            an4.Value = analog4;
                            SqlParameter an5 = myCommand.Parameters.Add("@analog5", SqlDbType.SmallInt);
                            an5.Value = analog5;
                        }
                        catch (Exception e)
                        {
                            Logger.GetInstance().Exception("DB write failed (eventnew)(analog): ", e, EventLogPostfix);
                            return 0;
                        }


                        try
                        {
                            SqlParameter sequencenumber = myCommand.Parameters.Add("@sequencenumber", SqlDbType.Int);
                            sequencenumber.Value = sequenceNumber > Int32.MaxValue
                                                       ? Int32.MaxValue
                                                       : Convert.ToInt32(sequenceNumber);
                        }
                        catch (Exception e)
                        {
                            Logger.GetInstance().Exception("DB write failed (eventnew)(sequencenumber): ", e, EventLogPostfix);
                            return 0;
                        }

                        if (additionalDataString != null)
                        {
                            try
                            {
                                SqlParameter evtstring = myCommand.Parameters.Add("@evtstring", SqlDbType.VarChar);
                                evtstring.Value = additionalDataString;
                            }
                            catch (Exception e)
                            {
                                Logger.GetInstance().Exception("DB write failed (eventnew)(evtstring): ", e, EventLogPostfix);
                                return 0;
                            }
                        }

                        if (additionalDataName != null)
                        {
                            try
                            {
                                SqlParameter evtdname = myCommand.Parameters.Add("@evtdname", SqlDbType.VarChar);
                                evtdname.Value = additionalDataName;
                            }
                            catch (Exception e)
                            {
                                Logger.GetInstance().Exception("DB write failed (eventnew)(evtdname): ", e, EventLogPostfix);
                                return 0;
                            }
                        }

                        try
                        {
                            var p = myCommand.Parameters.Add("@altitude", SqlDbType.VarChar);
                            p.Value = altitude;
                        }
                        catch (Exception e)
                        {
                            Logger.GetInstance().Exception("DB write failed (eventnew)(altitude): ", e, EventLogPostfix);
                            return 0;
                        }

                        try
                        {
                            var p = myCommand.Parameters.Add("@gpssatellitecount", SqlDbType.VarChar);
                            p.Value = gpssatellitecount;
                        }
                        catch (Exception e)
                        {
                            Logger.GetInstance().Exception("DB write failed (eventnew)(gpssatellitecount): ", e, EventLogPostfix);
                            return 0;
                        }

                        try
                        {
                            var p = myCommand.Parameters.Add("@gprssignalstrength", SqlDbType.VarChar);
                            p.Value = gprssignalstrength;
                        }
                        catch (Exception e)
                        {
                            Logger.GetInstance().Exception("DB write failed (eventnew)(gprssignalstrength): ", e, EventLogPostfix);
                            return 0;
                        }

                        try
                        {
                            var p = myCommand.Parameters.Add("@systemstatus", SqlDbType.VarChar);
                            p.Value = systemstatus;
                        }
                        catch (Exception e)
                        {
                            Logger.GetInstance().Exception("DB write failed (eventnew)(systemstatus): ", e, EventLogPostfix);
                            return 0;
                        }

                        try
                        {
                            var p = myCommand.Parameters.Add("@batterychargelevel", SqlDbType.VarChar);
                            p.Value = batterychargelevel;
                        }
                        catch (Exception e)
                        {
                            Logger.GetInstance().Exception("DB write failed (eventnew)(batterychargelevel): ", e, EventLogPostfix);
                            return 0;
                        }

                        try
                        {
                            var p = myCommand.Parameters.Add("@externalinputvoltage", SqlDbType.VarChar);
                            p.Value = externalinputvoltage;
                        }
                        catch (Exception e)
                        {
                            Logger.GetInstance().Exception("DB write failed (eventnew)(externalinputvoltage): ", e, EventLogPostfix);
                            return 0;
                        }

                        try
                        {
                            var p = myCommand.Parameters.Add("@maxspeed", SqlDbType.VarChar);
                            p.Value = maxspeed;
                        }
                        catch (Exception e)
                        {
                            Logger.GetInstance().Exception("DB write failed (eventnew)(maxspeed): ", e, EventLogPostfix);
                            return 0;
                        }


                        try
                        {
                            var p = myCommand.Parameters.Add("@tripDistance", SqlDbType.Int);
                            p.Value = tripDistance;
                        }
                        catch (Exception e)
                        {
                            Logger.GetInstance().Exception("DB write failed (eventnew)(tripDistance): ", e, EventLogPostfix);
                            return 0;
                        }
                        try
                        {
                            var p = myCommand.Parameters.Add("@tachoStatus", SqlDbType.TinyInt);
                            p.Value = tachoStatus;
                        }
                        catch (Exception e)
                        {
                            Logger.GetInstance().Exception("DB write failed (eventnew)(tachoStatus): ", e, EventLogPostfix);
                            return 0;
                        }
                        try
                        {
                            var p = myCommand.Parameters.Add("@canStatus", SqlDbType.TinyInt);
                            p.Value = canStatus;
                        }
                        catch (Exception e)
                        {
                            Logger.GetInstance().Exception("DB write failed (eventnew)(canStatus): ", e, EventLogPostfix);
                            return 0;
                        }
                        try
                        {
                            var p = myCommand.Parameters.Add("@fuelLevel", SqlDbType.TinyInt);
                            p.Value = fuelLevel;
                        }
                        catch (Exception e)
                        {
                            Logger.GetInstance().Exception("DB write failed (eventnew)(fuelLevel): ", e, EventLogPostfix);
                            return 0;
                        }
                        try
                        {
                            var p = myCommand.Parameters.Add("@hardwareStatus", SqlDbType.TinyInt);
                            p.Value = hardwareStatus;
                        }
                        catch (Exception e)
                        {
                            Logger.GetInstance().Exception("DB write failed (eventnew)(hardwareStatus): ", e, EventLogPostfix);
                            return 0;
                        }

                        try
                        {
                            SqlParameter custid = myCommand.Parameters.Add("@customerintid", SqlDbType.Int);
                            custid.Direction = ParameterDirection.Output;
                        }
                        catch (Exception e)
                        {
                            Logger.GetInstance().Exception("DB write failed (eventnew)(custid-out): ", e, EventLogPostfix);
                        }

                        try
                        {
                            SqlParameter vid = myCommand.Parameters.Add("@vid", SqlDbType.UniqueIdentifier);
                            vid.Direction = ParameterDirection.Output;
                        }
                        catch (Exception e)
                        {
                            Logger.GetInstance().Exception("DB write failed (eventnew)(vid-out): ", e, EventLogPostfix);
                        }

                        try
                        {
                            SqlParameter eid = myCommand.Parameters.Add("@eid", SqlDbType.BigInt);
                            eid.Direction = ParameterDirection.Output;
                        }
                        catch (Exception e)
                        {
                            Logger.GetInstance().Exception("DB write failed (eventnew)(eid-out): ", e, EventLogPostfix);
                            return 0;
                        }

                        try
                        {
                            ExecuteCommandNonQuery(trackerID, myCommand, 3);
                        }
                        catch (Exception e)
                        {
                            Logger.GetInstance().Exception("DB write failed (" + mySQLCommandString + ")(execute)(" + trackerID + "): ", e, EventLogPostfix);
                            return 0;
                        }

                        try
                        {
                            if (myCommand.Parameters["@customerintid"].Value != null)
                                customerId = (int)myCommand.Parameters["@customerintid"].Value;
                            if (myCommand.Parameters["@vid"].Value != null)
                                vehicleId = (Guid)myCommand.Parameters["@vid"].Value;
                            if (myCommand.Parameters["@eid"].Value != null)
                                return (Int64)myCommand.Parameters["@eid"].Value;
                            else
                            {
                                Logger.GetInstance().Message("DB write failed (eventnew)(null return)", EventLogPostfix);
                                return 0;
                            }
                        }
                        catch (Exception e)
                        {
                            Logger.GetInstance().Exception("DB write failed (eventnew)(return): ", e, EventLogPostfix);
                            return 0;
                        }
                    }
                    catch (Exception e)
                    {
                        Logger.GetInstance().Exception("DB write failed (eventnew): ", e, EventLogPostfix);
                        return 0;
                    }
                }
            }
            catch (Exception e)
            {
                Logger.GetInstance().Exception("SQL Connection creation failed (eventnew): ", e, EventLogPostfix);
                return 0;
            }
        }

        public void WriteEventData(ServiceConfigurationSection config, string trackerID, string driverID, Int64 eventID, DateTime eventDateTime, string additionalDataName, string additionalDataString,
            int additionalDataInt, double additionalDataDouble, bool additionalDataBit, int creationCode, int depotID)
        {
            try
            {
                using (SqlConnection mySqlConnection = new SqlConnection(config.DataConnectionString.ConnectionString))
                {
                    try
                    {
                        mySqlConnection.Open();
                    }
                    catch (Exception e)
                    {
                        Logger.GetInstance().Exception("Open Sql connection failed (eventsdata): ", e, EventLogPostfix);
                    }

                    try
                    {
                        string mySQLCommandString = "proc_WriteEventDataTemp";

                        SqlCommand myCommand = new SqlCommand(mySQLCommandString, mySqlConnection);
                        myCommand.CommandType = CommandType.StoredProcedure;

                        try
                        {
                            SqlParameter eid = myCommand.Parameters.Add("@eid", SqlDbType.BigInt);
                            eid.Value = eventID;
                        }
                        catch (Exception e)
                        {
                            Logger.GetInstance().Exception("DB write failed (eventsdata)(eid): ", e, EventLogPostfix);
                            return;
                        }

                        try
                        {
                            SqlParameter evtdname = myCommand.Parameters.Add("@evtdname", SqlDbType.VarChar, 30);
                            evtdname.Value = additionalDataName;
                        }
                        catch (Exception e)
                        {
                            Logger.GetInstance().Exception("DB write failed (eventsdata)(evtdname): ", e, EventLogPostfix);
                            return;
                        }

                        try
                        {
                            SqlParameter evtstring = myCommand.Parameters.Add("@evtstring", SqlDbType.VarChar, 1024);
                            evtstring.Value = additionalDataString;
                        }
                        catch (Exception e)
                        {
                            Logger.GetInstance().Exception("DB write failed (eventsdata)(evtstring): ", e, EventLogPostfix);
                            return;
                        }

                        // Need a method to distinguish nulls for these params
                        try
                        {
                            SqlParameter evtint = myCommand.Parameters.Add("@evtint", SqlDbType.Int);
                            evtint.Value = additionalDataInt;
                        }
                        catch (Exception e)
                        {
                            Logger.GetInstance().Exception("DB write failed (eventsdata)(evtint): ", e, EventLogPostfix);
                            return;
                        }

                        try
                        {
                            // SQL float types are actually doubles
                            SqlParameter evtfloat = myCommand.Parameters.Add("@evtfloat", SqlDbType.Float);
                            evtfloat.Value = additionalDataDouble;
                        }
                        catch (Exception e)
                        {
                            Logger.GetInstance().Exception("DB write failed (eventsdata)(evtfloat): ", e, EventLogPostfix);
                            return;
                        }

                        try
                        {
                            SqlParameter evtbit = myCommand.Parameters.Add("@evtbit", SqlDbType.Bit);
                            evtbit.Value = additionalDataBit;
                        }
                        catch (Exception e)
                        {
                            Logger.GetInstance().Exception("DB write failed (eventsdata)(evtbit): ", e, EventLogPostfix);
                            return;
                        }

                        try
                        {
                            SqlParameter ccid = myCommand.Parameters.Add("@ccid", SqlDbType.SmallInt);
                            ccid.Value = RCSmallInt(creationCode);
                        }
                        catch (Exception e)
                        {
                            Logger.GetInstance().Exception("DB write failed (eventdata)(ccid): ", e, EventLogPostfix);
                            return;
                        }

                        try
                        {
                            SqlParameter customerintid = myCommand.Parameters.Add("@customerintid", SqlDbType.Int);
                            customerintid.Value = depotID;
                        }
                        catch (Exception e)
                        {
                            Logger.GetInstance().Exception("DB write failed (event)(customerintid): ", e, EventLogPostfix);
                            return;
                        }

                        try
                        {
                            // SQL float types are actually doubles
                            SqlParameter trackerid = myCommand.Parameters.Add("@trackerid", SqlDbType.VarChar, 50);
                            trackerid.Value = trackerID;
                        }
                        catch (Exception e)
                        {
                            Logger.GetInstance().Exception("DB write failed (eventsdata)(trackerid): ", e, EventLogPostfix);
                            return;
                        }

                        try
                        {
                            SqlParameter driverid = myCommand.Parameters.Add("@driverid", SqlDbType.VarChar, 32);
                            driverid.Value = driverID;
                        }
                        catch (Exception e)
                        {
                            Logger.GetInstance().Exception("DB write failed (eventsdata)(driverid): ", e, EventLogPostfix);
                            return;
                        }

                        try
                        {
                            SqlParameter eventdt = myCommand.Parameters.Add("@eventdt", SqlDbType.DateTime);
                            eventdt.Value = eventDateTime;
                        }
                        catch (Exception e)
                        {
                            Logger.GetInstance().Exception("DB write failed (eventsdata)(eventdt): ", e, EventLogPostfix);
                            return;
                        }

                        try
                        {
                            ExecuteCommandNonQuery(trackerID, myCommand, 3);
                        }
                        catch (Exception e)
                        {
                            Logger.GetInstance().Exception("DB write failed (eventsdata)(execute): ", e, EventLogPostfix);
                            return;
                        }
                    }
                    catch (Exception e)
                    {
                        Logger.GetInstance().Exception("DB write failed (eventdata): ", e, EventLogPostfix);
                        return;
                    }
                }
            }
            catch (Exception e)
            {
                Logger.GetInstance().Exception("SQL Connection creation failed (eventsdata): ", e, EventLogPostfix);
            }
        }

        #region Helpers
        private int ExecuteCommandNonQuery(string trackerID, SqlCommand myCommand, int retries)
        {
            try
            {
                var success = false;

                for (var i = 0; i < retries; i++)
                {
                    var res = ExecuteCommandNonQuery(myCommand);

                    if (res == null)
                    {
                        success = true;
                        break;
                    }
                    RecordWriteException(trackerID, myCommand, res);
                }

                return success ? 1 : 0;
            }
            catch (Exception e)
            {
                Logger.GetInstance().Exception("Failed to execute (" + myCommand.CommandText + ")(execute)(" + trackerID + "): Retries: " + retries, e, EventLogPostfix);
                return 0;
            }
        }
        private Exception ExecuteCommandNonQuery(SqlCommand myCommand)
        {
            try
            {
                myCommand.ExecuteNonQuery();
            }
            catch (Exception ex)
            {
                return ex;
            }
            return null;
        }
        private void RecordWriteException(string trackerID, SqlCommand myCommand, Exception e)
        {
            Logger.GetInstance().Exception("DB write failed (" + myCommand.CommandText + ")(execute)(" + trackerID + "): ", e, EventLogPostfix);
        }
        /// <summary>
        /// Range check a small int
        /// </summary>
        /// <param name="i"></param>
        /// <returns></returns>
        private Object RCSmallInt(int i)
        {
            if ((i > 32767) || (i < -32768))
                return DBNull.Value;
            else
                return i;
        }

        /// <summary>
        /// Range chack a tiny int
        /// </summary>
        /// <param name="i"></param>
        /// <returns></returns>
        private Object RCTinyInt(int i)
        {
            if ((i > 255) || (i < 0))
                return DBNull.Value;
            else
                return i;
        }
        #endregion
    }
}

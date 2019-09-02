using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace CameraListenerService.Data
{
    /// <summary>
    /// This object encapsulates each incoming piece of raw data whilst it waits to be parsed
    /// </summary>
    [Serializable]
    public class RawData
    {
        // Array of bytes containing the incoming data packet
        public byte[] rawData;
        public int rawDataLength;

        // Data arrival time
        //public DateTime arrivalTime;

        // Is the data complete and ready to be parsed
        // use method to set this flag to true when rest of struct has been filled out successfully
        // method checks for list being locked by parse op before setting flag
        // 
        // now just pass in data when we create object and set ready to parse flag to true imeddiatly as object is ready as soon as it is added to the list
        //private bool readyToParse;

        // Has the data been parsed OK, can we Delete it
        public bool parsedOK;

        // Parse retry count.
        public int parseRetryCount;

        public String trackerID;

        public RawData(byte[] data, int length, String trackerID)
        {
            rawData = data;
            rawDataLength = length;
            this.trackerID = trackerID;
            //arrivalTime = DateTime.Now; //new System.DateTime();
            //readyToParse = true;
            parsedOK = false;
            parseRetryCount = 0;
        }
    }
}

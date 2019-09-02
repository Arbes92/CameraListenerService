using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace CameraListenerService.Data.JSONobjects
{
    public class CTRestRequest
    {
        public string text { get; set; }
        public string[] to { get; set; }
        public string from { get; set; }
        public string mo { get; set; }

        public string maxMessageParts { get; set; }
    }
    public class CTMessage
    {
        public bool accepted { get; set; }
        public string to { get; set; }
        public string apiMessageId { get; set; }

        public CTDataError error { get; set; }
    }

    public class CTDataError
    {
        public string code { get; set; }
        public string description { get; set; }
    }

    public class CTData
    {
        public List<CTMessage> message { get; set; }
    }

    public class CTRestResponse
    {
        public CTData data { get; set; }
    }
}

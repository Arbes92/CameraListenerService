using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace CameraListenerService.Data.JSONobjects
{
    public class TelegramSendMessage
    {
        public string chat_id { get; set; }
        public string text { get; set; }
    }
    public class Chat
    {
        public string id { get; set; }
        public string title { get; set; }
        public string type { get; set; }
    }

    public class Result
    {
        public int message_id { get; set; }
        public string author_signature { get; set; }
        public Chat chat { get; set; }
        public int date { get; set; }
        public string text { get; set; }
    }

    public class TelegramResponse
    {
        public bool ok { get; set; }
        public Result result { get; set; }
    }
}

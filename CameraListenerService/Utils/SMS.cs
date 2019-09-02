using System;
using System.Configuration;
using System.Linq;
using System.Text;
using Newtonsoft.Json;
using CameraListenerService.Configuration;
using CameraListenerService.Data.JSONobjects;
using RestSharp;
using RestSharp.Serialization.Json;

namespace CameraListenerService.Utils
{
    public static class SMS
    {
        public static void Send(string aRecipient, string aSubject, string aBodyText)
        {
            try
            {
                //Add datetime
                aBodyText = DateTime.Now.ToString("dd-MM-yyyy HH:mm") + Environment.NewLine + aBodyText;
                SendThroughAPI(aBodyText, aRecipient);
            }
            catch (Exception ex)
            {
                Logger.GetInstance().Exception("Failed send SMS.", ex, string.Empty);
            }
        }

        private static void SendThroughAPI(string messageText, string recipient)
        {
            try
            {
                var elem = GetSmsSender();

                var svc = new RestClient(elem.url);   //elem.APIURL
                var request = new RestRequest("/message", Method.POST)
                {
                    RequestFormat = DataFormat.Json, JsonSerializer = new RestSharp.Serialization.Json.JsonSerializer()
                };
                request.AddHeader("Authorization", string.Format("Bearer " + elem.AuthToken));
                request.AddHeader("X-Version", "1");
                request.AddHeader("Accept", "application/json");

                var content = new CTRestRequest
                {
                    text = messageText,
                    to = new String[] { recipient },
                    from = elem.SenderId,
                    mo = "1",
                    maxMessageParts = "3"
                };

                var json = request.JsonSerializer.Serialize(content);
                request.AddParameter("application/json; charset=utf-8", json, ParameterType.RequestBody);
                IRestResponse response = svc.Execute(request);
                var data = JsonConvert.DeserializeObject<CTRestResponse>(response.Content);
                if (!data.data.message[0].accepted)
                {
                    Logger.GetInstance().Exception($"Failed to send SMS: {data.data.message[0].error.description}", new Exception(data.data.message[0].error.code), string.Empty);
                }
                else
                {
                    //Message sent
                }
            }
            catch (Exception ex)
            {
                Logger.GetInstance().Exception($"Failed to send SMS!", ex, string.Empty);
            }
        }
        private static SmsSenderElement GetSmsSender()
        {
            var config = (ServiceConfigurationSection)ConfigurationManager.GetSection("CameraListenerService.Configs");
            SmsSenderElement sender = null;
            if (config.SmsSenders != null && config.SmsSenders.Count != 0)
            {
                var senders = config.SmsSenders.Cast<SmsSenderElement>().Where(s => s.Enabled).ToList();
                sender = senders.Count > 0 ? senders[0] : null;
            }
            return sender;
        }


        /// <summary>
        /// Formats the SMS text. Appends new lines to clickatel format.
        /// </summary>
        /// <param name="origString">The original string.</param>
        /// <returns>Clickatel-formated text:{0} string</returns>
        private static string FormatSmsText(string origString)
        {
            var startIndex = origString.IndexOf(Environment.NewLine);
            if (startIndex < 0)
                return string.Format("text:{0}", origString);

            var lines = origString.Split(new[] { Environment.NewLine }, StringSplitOptions.RemoveEmptyEntries);
            var result = new StringBuilder();
            foreach (var line in lines.Where(line => line.Length >= 1))
            {
                result.AppendLine(string.Format("text:{0}", line));
            }
            return result.ToString();
        }
        /// <summary>
        /// Trims the text to 140 characters.
        /// </summary>
        /// <param name="origString">The original string.</param>
        /// <returns>Original string OR 140 characters of original string</returns>
        private static string FormatSmsLength(string origString)
        {
            return origString.Length > 140 ? origString.Substring(0, 139) : origString;
        }
    }
}

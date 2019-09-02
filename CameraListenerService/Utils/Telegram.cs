using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using CameraListenerService.Data.JSONobjects;
using Newtonsoft.Json;
using RestSharp;

namespace CameraListenerService.Utils
{
    public static class Telegram
    {
        public static void Send(string chatId, string botId, string aSubject, string aBodyText)
        {
            try
            {
                var messageObj = new TelegramSendMessage
                {
                    chat_id = chatId,
                    text = $"*{aSubject}*\r\n{aBodyText}"
                };
                var message = JsonConvert.SerializeObject(messageObj, Formatting.None,
                    new PrimitiveToStringConverter());

                var svc = new RestClient("https://api.telegram.org/bot" + botId);

                var request = new RestRequest("sendMessage", Method.POST);
                //request.AddHeader("Content-Type", "application/json");
                request.AddParameter("application/json", message, ParameterType.RequestBody);

                var response = svc.Execute(request);
                var responseObject = JsonConvert.DeserializeObject<TelegramResponse>(response.Content);
            }
            catch (Exception ex)
            {
                Logger.GetInstance().Exception("Failed send Telegram message.", ex, string.Empty);
            }
        }
    }
}

using System;
using System.Collections.Generic;
using System.Configuration;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using CameraListenerService.Utils;

namespace CameraListenerService.Configuration
{
    #region Base
    public class NotificationElement : ConfigurationElement
    {
        [ConfigurationProperty("name", IsKey = true, IsRequired = true)]
        public string Name
        {
            get { return (string)this["name"]; }
        }

        [ConfigurationProperty("value", IsKey = false, IsRequired = true)]
        public string Value
        {
            get { return (string)this["value"]; }
        }

        [ConfigurationProperty("enabled", IsKey = false, IsRequired = true)]
        public bool Enabled
        {
            get { return (bool)this["enabled"]; }
        }
    }
    #endregion

    #region Elements
    public class EmailSenderElement : NotificationElement
    {
        [ConfigurationProperty("Address", IsKey = false, IsRequired = true)]
        public string Address
        {
            get { return (string)this["Address"]; }
        }

        [ConfigurationProperty("Port", IsKey = false, IsRequired = true)]
        public int Port
        {
            get { return (int)this["Port"]; }
        }

        [ConfigurationProperty("Account", IsKey = false, IsRequired = true)]
        public string Account
        {
            get { return (string)this["Account"]; }
        }

        [ConfigurationProperty("Password", IsKey = false, IsRequired = true)]
        public string Password
        {
            get { return (string)this["Password"]; }
        }

        [ConfigurationProperty("Sender", IsKey = false, IsRequired = true)]
        public string Sender
        {
            get { return (string)this["Sender"]; }
        }
    }
    public class SmsSenderElement : NotificationElement
    {

        [ConfigurationProperty("url", IsKey = false, IsRequired = true)]
        public string url
        {
            get { return (string)this["url"]; }
        }

        [ConfigurationProperty("APIId", IsKey = false, IsRequired = true)]
        public string APIId
        {
            get { return (string)this["APIId"]; }
        }


        [ConfigurationProperty("SenderId", IsKey = false, IsRequired = false)]
        public string SenderId
        {
            get { return (string)this["SenderId"]; }
        }

        [ConfigurationProperty("AuthToken", IsKey = false, IsRequired = true)]
        public string AuthToken
        {
            get { return (string)this["AuthToken"]; }
        }
    }
    public class RecipientElement : NotificationElement
    {
        [ConfigurationProperty("telegramChatId", IsKey = false, IsRequired = false)]
        public string TelegramChatId
        {
            get { return (string)this["telegramChatId"]; }
        }

        [ConfigurationProperty("telegramBotId", IsKey = false, IsRequired = false)]
        public string TelegramBotId
        {
            get { return (string)this["telegramBotId"]; }
        }
    }
    #endregion

    #region Collections

    public class EmailSenderElementCollection : ConfigurationElementCollection
    {
        protected override ConfigurationElement CreateNewElement()
        {
            return new EmailSenderElement();
        }

        protected override object GetElementKey(ConfigurationElement element)
        {
            return ((EmailSenderElement)element).Name;
        }
    }
    public class SmsSenderElementCollection : ConfigurationElementCollection
    {
        protected override ConfigurationElement CreateNewElement()
        {
            return new SmsSenderElement();
        }

        protected override object GetElementKey(ConfigurationElement element)
        {
            return ((SmsSenderElement)element).Name;
        }
    }
    public class RecipientElementCollection : ConfigurationElementCollection
    {
        protected override ConfigurationElement CreateNewElement()
        {
            return new RecipientElement();
        }
        protected override object GetElementKey(ConfigurationElement element)
        {
            return ((RecipientElement)element).Name;
        }
    }
    #endregion

    #region Configuration reader
    public class ServiceConfigurationSection : ConfigurationSection
    {
        #region Constants

        private const string PortNumberDataKey = "PortNumber_Data";
        private const string PortNumberVideoKey = "PortNumber_Video";
        private const string PendingConnectionQueueKey = "PendingConnectionQueueSize";
        private const string DataConnectionStringKey = "DataConnectionString";
        private const string TCPBufferSizeKey = "TCPBufferSize";
        private const string TimerCleanupIntervalKey = "CleanupTimerIntervalMilliseconds";
        private const string TimerParseIntervalKey = "ParseTimerIntervalMilliseconds";
        private const string SocketInactivityTimeoutKey = "SocketInactivityTimeoutSeconds";

        #endregion

        public int SocketInactivityTimeout
        {
            get
            {
                try
                {
                    if (ConfigurationManager.AppSettings.AllKeys.Contains(SocketInactivityTimeoutKey))
                    {
                        var parseResult = int.TryParse(ConfigurationManager.AppSettings[SocketInactivityTimeoutKey], out var result);

                        return parseResult
                            ? result
                            : throw new FormatException($"{SocketInactivityTimeoutKey} is not Int32. Please check the record appSettings in App.config");
                    }
                    else
                    {
                        throw new ArgumentNullException($"{SocketInactivityTimeoutKey} is not configured. Please add the necessary kv pair to the appSettings in App.config");
                    }
                }
                catch (Exception e)
                {
                    Logger.GetInstance().Exception($"Failed to read {PortNumberDataKey}", e, string.Empty, new List<NotificationChannel> { NotificationChannel.Email, NotificationChannel.SMS, NotificationChannel.Telegram });
                    return 1500;
                }
            }
        }
        public int ParseTimerInterval
        {
            get
            {
                try
                {
                    if (ConfigurationManager.AppSettings.AllKeys.Contains(TimerParseIntervalKey))
                    {
                        var parseResult = int.TryParse(ConfigurationManager.AppSettings[TimerParseIntervalKey], out var result);

                        return parseResult
                            ? result
                            : throw new FormatException($"{TimerParseIntervalKey} is not Int32. Please check the record appSettings in App.config");
                    }
                    else
                    {
                        throw new ArgumentNullException($"{TimerParseIntervalKey} is not configured. Please add the necessary kv pair to the appSettings in App.config");
                    }
                }
                catch (Exception e)
                {
                    Logger.GetInstance().Exception($"Failed to read {TimerParseIntervalKey}", e, string.Empty, new List<NotificationChannel> { NotificationChannel.Email, NotificationChannel.SMS, NotificationChannel.Telegram });
                    return 1500;
                }
            }
        }
        public int CleanupTimerInterval
        {
            get
            {
                try
                {
                    if (ConfigurationManager.AppSettings.AllKeys.Contains(TimerCleanupIntervalKey))
                    {
                        var parseResult = int.TryParse(ConfigurationManager.AppSettings[TimerCleanupIntervalKey], out var result);

                        return parseResult
                            ? result
                            : throw new FormatException($"{TimerCleanupIntervalKey} is not Int32. Please check the record appSettings in App.config");
                    }
                    else
                    {
                        throw new ArgumentNullException($"{TimerCleanupIntervalKey} is not configured. Please add the necessary kv pair to the appSettings in App.config");
                    }
                }
                catch (Exception e)
                {
                    Logger.GetInstance().Exception($"Failed to read {TimerCleanupIntervalKey}", e, string.Empty, new List<NotificationChannel> { NotificationChannel.Email, NotificationChannel.SMS, NotificationChannel.Telegram });
                    return 1500;
                }
            }
        }
        public int TCPBufferSize
        {
            get
            {
                try
                {
                    if (ConfigurationManager.AppSettings.AllKeys.Contains(TCPBufferSizeKey))
                    {
                        var parseResult = int.TryParse(ConfigurationManager.AppSettings[TCPBufferSizeKey], out var result);

                        return parseResult
                            ? result
                            : throw new FormatException($"{TCPBufferSizeKey} is not Int32. Please check the record appSettings in App.config");
                    }
                    else
                    {
                        throw new ArgumentNullException($"{TCPBufferSizeKey} is not configured. Please add the necessary kv pair to the appSettings in App.config");
                    }
                }
                catch (Exception e)
                {
                    Logger.GetInstance().Exception($"Failed to read {PortNumberDataKey}", e, string.Empty, new List<NotificationChannel> { NotificationChannel.Email, NotificationChannel.SMS, NotificationChannel.Telegram });
                    return 1500;
                }
            }
        }
        public int PortNumberData
        {
            get
            {
                try
                {
                    if (ConfigurationManager.AppSettings.AllKeys.Contains(PortNumberDataKey))
                    {
                        var parseResult = int.TryParse(ConfigurationManager.AppSettings[PortNumberDataKey], out var result);

                        return parseResult
                            ? result
                            : throw new FormatException($"{PortNumberDataKey} is not Int32. Please check the record appSettings in App.config");
                    }
                    else
                    {
                        throw new ArgumentNullException($"{PortNumberDataKey} is not configured. Please add the necessary kv pair to the appSettings in App.config");
                    }
                }
                catch (Exception e)
                {
                    Logger.GetInstance().Exception($"Failed to read {PortNumberDataKey}", e, string.Empty, new List<NotificationChannel>{NotificationChannel.Email, NotificationChannel.SMS, NotificationChannel.Telegram});
                    return 0;
                }
            }
        }
        public int PortNumberVideo
        {
            get
            {
                try
                {
                    if (ConfigurationManager.AppSettings.AllKeys.Contains(PortNumberVideoKey))
                    {
                        var parseResult = int.TryParse(ConfigurationManager.AppSettings[PortNumberVideoKey], out var result);

                        return parseResult
                            ? result
                            : throw new FormatException($"{PortNumberVideoKey} is not Int32. Please check the record appSettings in App.config");
                    }
                    else
                    {
                        throw new ArgumentNullException($"{PortNumberVideoKey} is not configured. Please add the necessary kv pair to the appSettings in App.config");
                    }
                }
                catch (Exception e)
                {
                    Logger.GetInstance().Exception($"Failed to read {PortNumberVideoKey}", e, string.Empty, new List<NotificationChannel> { NotificationChannel.Email, NotificationChannel.SMS, NotificationChannel.Telegram });
                    return 0;
                }
            }
        }
        public int PendingConnectionQueue
        {
            get
            {
                try
                {
                    if (ConfigurationManager.AppSettings.AllKeys.Contains(PendingConnectionQueueKey))
                    {
                        var parseResult = int.TryParse(ConfigurationManager.AppSettings[PendingConnectionQueueKey], out var result);

                        return parseResult
                            ? result
                            : throw new FormatException($"{PendingConnectionQueueKey} is not Int32. Please check the record appSettings in App.config");
                    }
                    else
                    {
                        throw new ArgumentNullException($"{PendingConnectionQueueKey} is not configured. Please add the necessary kv pair to the appSettings in App.config");
                    }
                }
                catch (Exception e)
                {
                    Logger.GetInstance().Exception($"Failed to read {PendingConnectionQueueKey}", e, string.Empty, new List<NotificationChannel> { NotificationChannel.Email, NotificationChannel.SMS, NotificationChannel.Telegram });
                    return 0;
                }
            }
        }
        public ConnectionStringSettings DataConnectionString
        {
            get
            {
                try
                {
                    var parseResult = ConfigurationManager.ConnectionStrings[DataConnectionStringKey];

                    return parseResult != null
                        ? parseResult
                        : throw new FormatException($"{DataConnectionStringKey} is missing. Please check the record connectionStrings in App.config");
                }
                catch (Exception e)
                {
                    Logger.GetInstance().Exception($"Failed to read {DataConnectionStringKey}", e, string.Empty, new List<NotificationChannel> { NotificationChannel.Email, NotificationChannel.SMS, NotificationChannel.Telegram });
                    return null;
                }
            }
        }


        [ConfigurationProperty("EmailSenders")]
        public EmailSenderElementCollection EmailSenders
        {
            get { return (EmailSenderElementCollection)this["EmailSenders"]; }
        }

        [ConfigurationProperty("SmsSenders")]
        public SmsSenderElementCollection SmsSenders
        {
            get { return (SmsSenderElementCollection)this["SmsSenders"]; }
        }

        [ConfigurationProperty("Recipients")]
        public RecipientElementCollection Recipients
        {
            get { return (RecipientElementCollection)this["Recipients"]; }
        }

    }
    #endregion
}

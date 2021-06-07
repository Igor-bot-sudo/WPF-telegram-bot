using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.IO;
using System.Text;

namespace WpfTelegramBot
{
    class JsonOps
    {

        public static void JsonSerializeMessageLog(ObservableCollection<MessageLog> messageList)
        {
            string json = JsonConvert.SerializeObject(messageList);
            File.WriteAllText("MessageLog.json", json);
        }


        public static void JsonDeSerializeMessageLog(ref ObservableCollection<MessageLog> messageList)
        {
            if (File.Exists("MessageLog.json"))
            {
                string json = File.ReadAllText("MessageLog.json");
                messageList = JsonConvert.DeserializeObject<ObservableCollection<MessageLog>>(json);
            }
        }
    }
}

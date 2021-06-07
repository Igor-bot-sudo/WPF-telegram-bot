using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.IO;
using System.Linq;
using System.Net;
using System.Threading.Tasks;
using Telegram.Bot;
using Telegram.Bot.Types;
using Telegram.Bot.Types.Enums;
using Telegram.Bot.Types.InputFiles;

namespace WpfTelegramBot
{
    class TelegramMessageClient
    {
        private MainWindow w;
        private TelegramBotClient Bot;
        public ObservableCollection<MessageLog> BotMessageLog { get; set; }
        public ObservableCollection<string> filesList = new ObservableCollection<string>();

        private async void MessageListener(object sender, Telegram.Bot.Args.MessageEventArgs e)
        {
            //Debug.WriteLine("+++---");

            //string text = $"{DateTime.Now.ToLongTimeString()}: {e.Message.Chat.FirstName} {e.Message.Chat.Id} {e.Message.Text}";

            //Debug.WriteLine($"{text} TypeMessage: {e.Message.Type.ToString()}");

            if (e.Message.Text == null) return;

            var messageText = e.Message.Text;

            w.Dispatcher.Invoke(() =>
            {
                BotMessageLog.Add(
                new MessageLog(
                    DateTime.Now.ToLongTimeString(), messageText, e.Message.Chat.FirstName, e.Message.Chat.Id));
            });

            var message = e.Message;
            string Answer = "";

            switch (message.Text.Split(' ').First())
            {
                // Запуск бота
                case "/start":
                    Answer = "Привет! Я - тестовый безымянный скромный бот.\n" +
                         "Умею показывать картинки, выгружать музыку, цитировать некоторые гарики Игоря Губермана и сообщать сведения о погоде в одном из 200.000 городов Земли";
                    await Bot.SendTextMessageAsync(message.Chat.Id, Answer);
                    break;

                // Гарики
                case "/garik":
                    await SendGarik(message);
                    break;

                // Список загруженных файлов
                case "/list":
                    Answer = "";
                    foreach (var item in filesList)
                    {
                        Answer = Answer + item + "\n";
                    }
                    if (Answer == "")
                    {
                        break;
                    }
                    await Bot.SendTextMessageAsync(message.Chat.Id, Answer);
                    break;

                // Картинка
                case "/photo":
                    await SendPicture(message);
                    break;

                // Аудио
                case "/audio":
                    await SendAudio(message);
                    break;

                // Погода
                case "/wheather":
                    await SendWheather(message);
                    break;

                default:
                    await Usage(message);
                    break;
            }

            // Отправляем гарика
            async Task SendGarik(Message message)
            {
                string Answer = "";

                StreamReader streamReader = System.IO.File.OpenText("Files/Gariki.txt");
                string lines = streamReader.ReadToEnd();
                string[] splitLines = lines.Split('#');
                Random random = new Random();
                int fourLines = random.Next(splitLines.Length);
                string[] splitSplitLines = splitLines[fourLines].Split('\n');
                for (int i = 0; i < 4; i++)
                {
                    Answer = Answer + splitSplitLines[i] + "\n";
                }
                await Bot.SendTextMessageAsync(message.Chat.Id, Answer);
            }

            // Отправляем картинку
            async Task SendPicture(Message message)
            {
                await Bot.SendChatActionAsync(message.Chat.Id, ChatAction.UploadPhoto);

                DirectoryInfo directoryInfo = new DirectoryInfo(@"Files/Razumov");
                Random random = new Random();
                FileInfo[] fi = directoryInfo.GetFiles();
                int item = random.Next(fi.Length);
                var fileName = fi[item].Name;
                using var fileStream = fi[item].Open(FileMode.Open, FileAccess.Read, FileShare.Read);
                w.Dispatcher.Invoke(() => {filesList.Add(fileName);});

                await Bot.SendPhotoAsync(
                    chatId: message.Chat.Id,
                    photo: new InputOnlineFile(fileStream, fileName),
                    caption: "Nice Picture"
                );
            }

            // Отправляем аудиофайл
            async Task SendAudio(Message message)
            {
                await Bot.SendChatActionAsync(message.Chat.Id, ChatAction.UploadAudio);

                DirectoryInfo directoryInfo = new DirectoryInfo(@"Files/Satriani");
                Random random = new Random();
                FileInfo[] fi = directoryInfo.GetFiles();
                int item = random.Next(fi.Length);
                var fileName = fi[item].Name;
                using var fileStream = fi[item].Open(FileMode.Open, FileAccess.Read, FileShare.Read);
                w.Dispatcher.Invoke(() => { filesList.Add(fileName); });

                await Bot.SendAudioAsync(
                    chatId: message.Chat.Id,
                    audio: new InputOnlineFile(fileStream, fileName),
                    caption: "Nice Music"
                );
            }

            // Отправляем сведения о погоде
            async Task SendWheather(Message message)
            {
                // Берем из команды название города
                string[] cityNames = message.Text.Split(' ');
                string cityName = cityNames[1].ToUpper();
                string Id = "";

                // Список городов и их идентификаторов
                List<JsonCity> jsonCities = new List<JsonCity>();

                // Заполняем список из файла
                if (System.IO.File.Exists("city.list.json"))
                {
                    string json = System.IO.File.ReadAllText("city.list.json");
                    jsonCities = JsonConvert.DeserializeObject<List<JsonCity>>(json);
                }
                else return;

                // Определяем идентификатор по названию города
                foreach (var item in jsonCities)
                {
                    if (cityName == item.Name.ToUpper())
                    {
                        Id = item.Id.ToString();
                        break;
                    }
                }

                string Answer = "В городе " + cityName + ":\n\n";

                if (Id == "")
                {
                    Answer = "Данных о таком городе нет";
                    await Bot.SendTextMessageAsync(message.Chat.Id, Answer);
                    return;
                }

                string jsonString;
                // Строка запроса для сайта openweathermap.org
                string requestString = "http://api.openweathermap.org/data/2.5/weather?id=" + Id + "&appid=e3f423ccce791d572fb8eff2c30c9015&units=metric";
                WebRequest request = WebRequest.Create(requestString);
                request.Method = "GET";
                WebResponse response = await request.GetResponseAsync();

                using (Stream stream = response.GetResponseStream())
                {
                    using (StreamReader reader = new StreamReader(stream))
                    {
                        jsonString = reader.ReadToEnd();
                    }
                }

                JsonTextReader jsonReader = new JsonTextReader(new StringReader(jsonString));
                bool getValue = false;
                // Парсим нужные данные
                while (jsonReader.Read())
                {
                    if (jsonReader.Value != null)
                    {
                        if (getValue)
                        {
                            Answer = Answer + jsonReader.Value + "\n";
                            getValue = false;
                        }

                        if (jsonReader.Value.ToString() == "temp")
                        {
                            getValue = true;
                            Answer = Answer + "Температура: ";
                        }

                        if (jsonReader.Value.ToString() == "pressure")
                        {
                            getValue = true;
                            Answer = Answer + "Давление: ";
                        }

                        if (jsonReader.Value.ToString() == "humidity")
                        {
                            getValue = true;
                            Answer = Answer + "Влажность: ";
                        }
                    }
                }

                // Сообщаем данные элементу главного окна
                w.Dispatcher.Invoke(() => { w.Wheather.Text = Answer; });

                await Bot.SendTextMessageAsync(message.Chat.Id, Answer);
            }

            async Task Usage(Message message)
            {
                const string usage = "Usage:\n" +
                                        "/start - запуск бота\n" +
                                        "/garik - гарики Игоря Губермана\n" +
                                        "/audio - избранные композиции супергитариста Джо Сатриани\n" +
                                        "/photo - коллекция романтичных женских образов, созданных художником Константином Разумовым\n" +
                                        "/wheatherПРОБЕЛназвание_города(латиницей) - сведения о текущей погоде в городе\n" +
                                        "/list - список загруженных файлов";
                await Bot.SendTextMessageAsync(
                        chatId: message.Chat.Id,
                        text: usage);
            }
        }

        public TelegramMessageClient(MainWindow W)
        {
            this.BotMessageLog = new ObservableCollection<MessageLog>();
            this.w = W;

            _ = CreateBot();
        }

        async Task CreateBot()
        {
            // Создаем бота
            Bot = new TelegramBotClient("1766141466:AAFptnCParFr6GF8I02_fdp2WvjxELantsU");

            User me = await Bot.GetMeAsync();

            // Регистрируем обработчик событий от бота
            Bot.OnMessage += MessageListener;

            // Запускаем получение сообщений 
            Bot.StartReceiving(Array.Empty<UpdateType>());
            this.w.Title = "Listening for: " + me.Username;
        }

        public void SendMessage(string Text, string Id)
        {
            long id = Convert.ToInt64(Id);
            Bot.SendTextMessageAsync(id, Text);
        }
    }
}

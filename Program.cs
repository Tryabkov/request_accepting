using Telegram.Bot;
using Telegram.Bot.Exceptions;
using Telegram.Bot.Polling;
using Telegram.Bot.Requests;
using Telegram.Bot.Types;
using Telegram.Bot.Types.Enums;
using Telegram.Bot.Types.ReplyMarkups;
using System.Data.SQLite;

namespace Request_accepting
{
    internal class Program
    {
        #region Variables
        private const string Token = "7297071183:AAEpIv51byTBNWcGELQmu0e4ZrwBSFwbAYw";
        private const long ChatId = -1002008368194;
        private const string msg = "";
        private const string stepBack = "..//..//..//";
        private const string connectionString = $"Data Source={stepBack}Users_db.db";
        private readonly static List<long> adminId = new List<long>() { 948468834, 6805337669 };

        private static ITelegramBotClient _botClient;
        private static ReceiverOptions _receiverOptions;
        #endregion

        #region Keyboards
        public static ReplyKeyboardMarkup menu = new(new[]
        {
            new KeyboardButton[] { "Кол-во вступивших сегодня" },
            new KeyboardButton[] { "Кол-во вступивших вчера" },
            new KeyboardButton[] { "Кол-во пользователей" }
        })
        {
            ResizeKeyboard = true
        };
        #endregion

        static async Task Main()
        {
            System.IO.File.WriteAllText("Errors.txt", "");
            var connection = new SQLiteConnection(connectionString);
            connection.Open();

            var _httpClient = new HttpClient();
            _httpClient.Timeout = new TimeSpan(3, 0, 0);
            _botClient = new TelegramBotClient(Token, _httpClient);

            _receiverOptions = new ReceiverOptions
            {
                AllowedUpdates = new[]
                {
                        UpdateType.Message,
                        UpdateType.ChatJoinRequest
                    },

                ThrowPendingUpdates = true,
            };

            using var cts = new CancellationTokenSource();
            _botClient.StartReceiving(HandleUpdateAsync, HandlePollingErrorAsync, _receiverOptions, cts.Token);
            var me = await _botClient.GetMeAsync();
            Console.WriteLine($"{me.FirstName} is started!");
            await Task.Delay(-1);

            async Task HandleUpdateAsync(ITelegramBotClient botClient, Update update, CancellationToken cancellationToken)
            {

                var message = update.Message;
                var messageText = "";

                if (update.ChatJoinRequest != null)
                {
                    long userId = update.ChatJoinRequest.From.Id;
                    long ChatId = update.ChatJoinRequest.Chat.Id;

                    try
                    {
                        new SQLiteCommand($"INSERT INTO Users (Id, StartTime) VALUES ('{userId}', datetime('now'))", connection).ExecuteNonQuery();
                    }
                    catch (Exception e) { }

                    try
                    {
                        await botClient.ApproveChatJoinRequest(ChatId, userId, cancellationToken);
                        await botClient.SendTextMessageAsync(
                            chatId: userId,
                            text: msg,
                            cancellationToken: cancellationToken);
                    }
                    catch (Exception e) { }
                }
                if (message != null)
                {
                    messageText = update.Message.Text;
                    if (messageText != null)
                    {
                        #region commands
                        if (adminId.Contains(update.Message.From.Id))
                        {
                            if (messageText.Contains("/mailing"))
                            {
                                SQLiteCommand command = connection.CreateCommand();
                                command.CommandText = "SELECT Id FROM Users";
                                SQLiteDataReader reader = command.ExecuteReader();
                                List<string[]> data = new List<string[]>();
                                while (reader.Read())
                                {
                                    data.Add(new string[1]);
                                    data[data.Count - 1][0] = reader[0].ToString();
                                }

                                foreach (string[] s in data)
                                {
                                    try
                                    {
                                        await botClient.SendTextMessageAsync(
                                        chatId: s[0].ToString(),
                                        text: messageText.Substring(9),
                                        cancellationToken: cancellationToken);
                                    }
                                    catch (Exception e) { }

                                }
                            }

                            if (messageText.Contains("admin"))
                            {
                                await botClient.SendTextMessageAsync(
                                        chatId: update.Message.From.Id,
                                        text: "Вызов админ панели",
                                        replyMarkup: menu,
                                        cancellationToken: cancellationToken);
                            }

                            if (messageText.Contains("Кол-во вступивших сегодня"))
                            {
                                var newUsersReader = new SQLiteCommand($"SELECT COUNT(*) FROM Users WHERE StartTime >= datetime('now', '-1 day')", connection: connection).ExecuteReader();
                                newUsersReader.Read();
                                int newUsers = newUsersReader.GetInt32(0);

                                await botClient.SendTextMessageAsync(
                                    chatId: update.Message.From.Id,
                                    text: $"Кол во вступивших сегодня - {newUsers}",
                                    replyMarkup: menu,
                                    cancellationToken: cancellationToken);
                            }

                            if (messageText.Contains("Кол-во пользователей"))
                            {
                                var UsersReader = new SQLiteCommand($"SELECT COUNT(*) FROM Users", connection: connection).ExecuteReader();
                                UsersReader.Read();
                                int Users = UsersReader.GetInt32(0);

                                await botClient.SendTextMessageAsync(
                                    chatId: update.Message.From.Id,
                                    text: $"Кол во пользователей - {Users}",
                                    replyMarkup: menu,
                                    cancellationToken: cancellationToken);
                            }

                            if (messageText.Contains("Кол-во вступивших вчера"))
                            {
                                var OldUsersReader = new SQLiteCommand($"SELECT COUNT(*) FROM Users WHERE StartTime <= datetime('now', '-1 day') and StartTime >= datetime('now', '-2 day')", connection: connection).ExecuteReader();
                                OldUsersReader.Read();
                                int OldUsers = OldUsersReader.GetInt32(0);

                                await botClient.SendTextMessageAsync(
                                    chatId: update.Message.From.Id,
                                    text: $"Кол во вступивших вчера - {OldUsers}",
                                    replyMarkup: menu,
                                    cancellationToken: cancellationToken);
                            }
                        }
                        #endregion
                    }
                }
            }

            Task HandlePollingErrorAsync(ITelegramBotClient botClient, Exception exception, CancellationToken cancellationToken)
            {
                var ErrorMessage = exception switch
                {
                    ApiRequestException apiRequestException
                        => $"Telegram API Error:\n[{apiRequestException.ErrorCode}]\n{apiRequestException.Message}",
                    _ => exception.ToString()
                };

                System.IO.File.AppendAllText("Errors.txt", ErrorMessage);
                return Task.CompletedTask;
            }
        }

    }
}

using System;
using System.IO;
using System.Net;
using System.Net.Sockets;
using System.Text;
using System.Threading.Tasks;
using System.Windows;
using MySql.Data.MySqlClient;

namespace практика2часть
{
    public static class HttpServer
    {
        private static TcpListener listener;
        private static bool isRunning = false;

        private static string connectionString =
            "server=192.168.227.14;port=3306;Database=практика2часть;Uid=user04;Pwd=User04!Pass;";

        [STAThread]
        public static void Main()
        {
            var app = new Application();
            app.Run(new Window1());
        }

        public static void Start()
        {
            if (isRunning)
                return;

            try
            {
                listener = new TcpListener(
                    IPAddress.Loopback,
                    5051);

                listener.Start();

                isRunning = true;

                Task.Run(() => Listen());

                // Автоматически открываем API в браузере  
                Task.Delay(500).ContinueWith(t =>
                {
                    try
                    {
                        System.Diagnostics.Process.Start(
                            new System.Diagnostics.ProcessStartInfo
                            {
                                FileName =
                                    "http://127.0.0.1:5051/api/notes",

                                UseShellExecute = true
                            });
                    }
                    catch
                    {
                    }
                });
            }
            catch (Exception ex)
            {
                MessageBox.Show(
                    "API НЕ ЗАПУСТИЛСЯ:\n\n" +
                    ex.ToString(),
                    "Ошибка запуска",
                    MessageBoxButton.OK,
                    MessageBoxImage.Error);
            }
        }

        private static async Task Listen()
        {
            while (isRunning)
            {
                try
                {
                    TcpClient client =
                        await listener.AcceptTcpClientAsync();

                    _ = Task.Run(
                        () => ProcessRequest(client));
                }
                catch
                {
                    if (!isRunning)
                        break;
                }
            }
        }

        private static async Task ProcessRequest(
            TcpClient client)
        {
            try
            {
                using (client)
                using (NetworkStream stream =
                    client.GetStream())
                using (StreamReader reader =
                    new StreamReader(
                        stream,
                        Encoding.UTF8,
                        false,
                        1024,
                        true))
                {
                    string requestLine =
                        await reader.ReadLineAsync();

                    if (string.IsNullOrEmpty(requestLine))
                        return;

                    string[] parts =
                        requestLine.Split(' ');

                    if (parts.Length < 2)
                    {
                        await SendResponse(
                            stream,
                            400,
                            "Bad Request",
                            "{\"error\":\"Некорректный запрос\"}");

                        return;
                    }

                    string method = parts[0];
                    string fullPath = parts[1];

                    // Читаем HTTP-заголовки  
                    string line;

                    do
                    {
                        line =
                            await reader.ReadLineAsync();
                    }
                    while (!string.IsNullOrEmpty(line));

                    // Неправильный метод или путь  
                    if (method != "GET" ||
                        !fullPath.StartsWith("/api/notes"))
                    {
                        await SendResponse(
                            stream,
                            404,
                            "Not Found",
                            "{\"error\":\"Страница не найдена\"}");

                        return;
                    }

                    // Ошибка 400  
                    if (fullPath.Contains(
                            "ошибка_типа_данных") ||
                        fullPath.Contains(
                            "%D0%BE%D1%88%D0%B8%D0%B1%D0%BA%D0%B0"))
                    {
                        await SendResponse(
                            stream,
                            400,
                            "Bad Request",
                            "{\"error\":\"Ошибка типа данных\"}");

                        return;
                    }

                    try
                    {
                        StringBuilder json =
                            new StringBuilder();

                        json.Append("[\n");

                        using (MySqlConnection connection =
                            new MySqlConnection(
                                connectionString))
                        {
                            connection.Open();

                            string query =
                                "SELECT id, name, inn, address, phone, type " +
                                "FROM contractors";

                            using (MySqlCommand command =
                                new MySqlCommand(
                                    query,
                                    connection))

                            using (MySqlDataReader dbReader =
                                command.ExecuteReader())
                            {
                                bool first = true;

                                while (dbReader.Read())
                                {
                                    if (!first)
                                        json.Append(",\n");

                                    first = false;

                                    string id =
                                        dbReader["id"].ToString();

                                    string name =
                                        dbReader["name"].ToString();

                                    string inn =
                                        dbReader.IsDBNull(
                                            dbReader.GetOrdinal(
                                                "inn"))
                                            ? ""
                                            : dbReader["inn"]
                                                .ToString();

                                    string address =
                                        dbReader["address"]
                                            .ToString();

                                    string phone =
                                        dbReader["phone"]
                                            .ToString();

                                    string type =
                                        dbReader["type"]
                                            .ToString();

                                    json.Append(
                                        "  {\n" +

                                        "    \"id\": \"" +
                                        EscapeJson(id) +
                                        "\",\n" +

                                        "    \"title_user\": \"" +
                                        EscapeJson(name) +
                                        "\",\n" +

                                        "    \"content\": \"ИНН: " +
                                        EscapeJson(inn) +
                                        ", Тел: " +
                                        EscapeJson(phone) +
                                        ", Тип: " +
                                        EscapeJson(type) +
                                        "\",\n" +

                                        "    \"formatted_date\": \"" +
                                        EscapeJson(address) +
                                        "\"\n" +

                                        "  }");
                                }
                            }
                        }

                        json.Append("\n]");

                        await SendResponse(
                            stream,
                            200,
                            "OK",
                            json.ToString());
                    }
                    catch
                    {
                        await SendResponse(
                            stream,
                            500,
                            "Internal Server Error",
                            "{\"error\":\"Ошибка подключения к базе данных\"}");
                    }
                }
            }
            catch
            {
                // Клиент закрыл соединение  
            }
        }

        private static async Task SendResponse(
            NetworkStream stream,
            int statusCode,
            string statusText,
            string body)
        {
            byte[] bodyBytes =
                Encoding.UTF8.GetBytes(body);

            string headers =
                "HTTP/1.1 " +
                statusCode +
                " " +
                statusText +
                "\r\n" +

                "Content-Type: " +
                "application/json; charset=utf-8\r\n" +

                "Content-Length: " +
                bodyBytes.Length +
                "\r\n" +

                "Connection: close\r\n" +

                "\r\n";

            byte[] headerBytes =
                Encoding.UTF8.GetBytes(headers);

            await stream.WriteAsync(
                headerBytes,
                0,
                headerBytes.Length);

            await stream.WriteAsync(
                bodyBytes,
                0,
                bodyBytes.Length);
        }

        private static string EscapeJson(
            string value)
        {
            if (value == null)
                return "";

            return value
                .Replace("\\", "\\\\")
                .Replace("\"", "\\\"")
                .Replace("\r", "\\r")
                .Replace("\n", "\\n");
        }

        public static void Stop()
        {
            try
            {
                isRunning = false;

                if (listener != null)
                {
                    listener.Stop();
                    listener = null;
                }
            }
            catch
            {
            }
        }
    }
}

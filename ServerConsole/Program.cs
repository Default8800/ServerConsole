using System;
using System.Net;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

using System.Net;
using System.Text;

using System;
using System.Net;
using System.Text;
using System.Threading.Tasks;
using ServerConsole.DataClasses;
using System.Net.Sockets;
using System.Text.Json;
using System.Net.Http;
using Microsoft.Extensions.Options;

class Program
{
    private static HttpListener _server;
    private static TcpListener _tcpServer;
    private static bool _isRunning = true;
    private static int _numRequest = 0;
    private static DbMethods db;
    static async Task Main(string[] args)
    {
        _server = new HttpListener();
        // Добавляем префиксы для API
        _server.Prefixes.Add("http://127.0.0.1:8888/");
        _server.Prefixes.Add("http://localhost:8888/");

        _tcpServer = new TcpListener(IPAddress.Any, 8889);
        DbConnect();

        _server.Start();
        _tcpServer.Start();

        Console.WriteLine("HTTP Сервер запущен на http://localhost:8888/");
        Console.WriteLine("TCP Сервер запущен на порту 8889");
        Console.WriteLine("Нажмите Ctrl+C для остановки сервера");

        // Обработка Ctrl+C
        Console.CancelKeyPress += (sender, e) =>
        {
            e.Cancel = true;
            _isRunning = false;
            _server.Stop();
            _tcpServer.Stop();
            Console.WriteLine("Сервер остановлен");
        };

        // Запускаем обработку запросов
        await Task.WhenAll(HandleHttpRequestsAsync(), HandleTcpRequestsAsync());
    }

    private static async Task HandleHttpRequestsAsync()
    {
        while (_isRunning)
        {
            try
            {
                var context = await _server.GetContextAsync();
                _ = Task.Run(() => ProcessHttpRequestAsync(context));
            }
            catch (HttpListenerException)
            {
                // Сервер был остановлен
                break;
            }
            catch (Exception ex)
            {
                Console.WriteLine($"❌ Ошибка обработки HTTP запроса: {ex.Message}");
            }
        }
    }


    
    public interface IResponseWriter
    {
        Task WriteAsync(string data);
        Task SendErrorAsync(string message);
        Task FlushAsync();
    }

    // Реализация для HTTP
    public class HttpResponseWriter : IResponseWriter
    {
        private readonly HttpListenerResponse _response;
        private bool _responseSent = false;

        public HttpResponseWriter(HttpListenerResponse response)
        {
            _response = response;
            _response.ContentType = "application/json; charset=utf-8";

            // CORS заголовки
            _response.Headers.Add("Access-Control-Allow-Origin", "*");
            _response.Headers.Add("Access-Control-Allow-Methods", "GET, POST, PUT, DELETE, OPTIONS");
            _response.Headers.Add("Access-Control-Allow-Headers", "Content-Type, Accept, Authorization, X-Requested-With");
        }

        public async Task WriteAsync(string data)
        {
            if (_responseSent) return;

            byte[] buffer = Encoding.UTF8.GetBytes(data);
            _response.ContentLength64 = buffer.Length;
            await _response.OutputStream.WriteAsync(buffer, 0, buffer.Length);
        }

        public async Task SendErrorAsync(string message)
        {
            if (_responseSent) return;

            _response.StatusCode = 400;
            var errorResponse = new { Status = "error", Message = message };
            string json = JsonSerializer.Serialize(errorResponse);
            await WriteAsync(json);
            await CloseAsync();
        }

        public Task FlushAsync()
        {
            // Для HTTP не требуется отдельный flush
            return Task.CompletedTask;
        }

        public async Task CloseAsync()
        {
            if (!_responseSent)
            {
                _responseSent = true;
                _response.Close();
            }
        }
    }

    // Реализация для TCP
    public class TcpResponseWriter : IResponseWriter
    {
        private readonly StreamWriter _writer;

        public TcpResponseWriter(StreamWriter writer)
        {
            _writer = writer;
        }

        public async Task WriteAsync(string data)
        {
            await _writer.WriteAsync(data);
        }

        public async Task SendErrorAsync(string message)
        {
            var errorResponse = new { Status = "error", Message = message };
            string json = JsonSerializer.Serialize(errorResponse);
            await WriteAsync(json);
            await FlushAsync();
        }

        public async Task FlushAsync()
        {
            await _writer.FlushAsync();
        }
    }


    private static async Task ProcessHttpRequestAsync(HttpListenerContext context)
    {
        HttpListenerRequest request = context.Request;
        HttpListenerResponse response = context.Response;

        var responseWriter = new HttpResponseWriter(response);

        try
        {
            Interlocked.Increment(ref _numRequest);
            Console.WriteLine($"\n🌐 HTTP Запрос №{_numRequest}/Время: {DateTime.Now:HH:mm:ss}");
            Console.WriteLine($"🔗 URL: {request.Url}");
            Console.WriteLine($"📡 Метод: {request.HttpMethod}");
            Console.WriteLine($"👤 Клиент: {request.RemoteEndPoint}");

            // Обрабатываем preflight запросы (OPTIONS)
            if (request.HttpMethod == "OPTIONS")
            {
                response.StatusCode = 200;
                response.ContentLength64 = 0;
                response.Close();
                return;
            }

            // Проверяем метод запроса
            if (request.HttpMethod != "POST")
            {
                await responseWriter.SendErrorAsync("Только POST запросы поддерживаются");
                return;
            }

            // Читаем тело запроса
            string requestBody = await ReadRequestBodyAsync(request);
            Console.WriteLine($"📝 HTTP Тело запроса: {requestBody}");

            if (string.IsNullOrEmpty(requestBody))
            {
                await responseWriter.SendErrorAsync("Пустое тело запроса");
                return;
            }

            // Десериализуем JSON
            RequestsClass requests;
            try
            {
                var options = new JsonSerializerOptions
                {
                    PropertyNameCaseInsensitive = true
                };
                requests = JsonSerializer.Deserialize<RequestsClass>(requestBody, options);
            }
            catch (JsonException ex)
            {
                await responseWriter.SendErrorAsync($"Неверный формат JSON: {ex.Message}");
                return;
            }

            if (requests == null)
            {
                await responseWriter.SendErrorAsync("Неверный формат запроса");
                return;
            }

            // Обрабатываем запрос
            await ProcessRequestAsync(requests, responseWriter);
            await responseWriter.CloseAsync();
        }
        catch (Exception ex)
        {
            Console.WriteLine($"❌ Ошибка обработки HTTP запроса: {ex.Message}");
            await responseWriter.SendErrorAsync("Внутренняя ошибка сервера");
        }
    }

    private static async Task ProcessTcpRequestAsync(TcpClient tcpClient)
    {
        try
        {
            Interlocked.Increment(ref _numRequest);
            Console.WriteLine($"\n🔌 TCP Запрос №{_numRequest}/Время: {DateTime.Now:HH:mm:ss}");
            Console.WriteLine($"🌐 Клиент: {tcpClient.Client.RemoteEndPoint}");

            using (tcpClient)
            using (var stream = tcpClient.GetStream())
            using (var reader = new StreamReader(stream, Encoding.UTF8))
            using (var writer = new StreamWriter(stream, Encoding.UTF8))
            {
                var responseWriter = new TcpResponseWriter(writer);

                // Читаем данные от клиента
                StringBuilder requestBuilder = new StringBuilder();
                char[] buffer = new char[8000];
                int bytesRead;

                // Устанавливаем таймаут для чтения
                stream.ReadTimeout = 5000;

                try
                {
                    while ((bytesRead = await reader.ReadAsync(buffer, 0, buffer.Length)) > 0)
                    {
                        requestBuilder.Append(buffer, 0, bytesRead);

                        // Если получен полный JSON (проверяем по балансу скобок)
                        string currentData = requestBuilder.ToString();
                        if (IsValidJson(currentData))
                        {
                            break;
                        }
                    }
                }
                catch (IOException)
                {
                    // Таймаут или разрыв соединения - обрабатываем полученные данные
                }

                string requestBody = requestBuilder.ToString();
                Console.WriteLine($"📝 TCP Данные: {requestBody}");

                if (string.IsNullOrEmpty(requestBody))
                {
                    await responseWriter.SendErrorAsync("Пустое тело запроса");
                    return;
                }

                RequestsClass requests;
                try
                {
                    requests = JsonSerializer.Deserialize<RequestsClass>(requestBody);
                }
                catch (JsonException ex)
                {
                    await responseWriter.SendErrorAsync($"Неверный формат JSON: {ex.Message}");
                    return;
                }

                if (requests == null)
                {
                    await responseWriter.SendErrorAsync("Неверный формат запроса");
                    return;
                }

                // Обрабатываем запрос
                await ProcessRequestAsync(requests, responseWriter);
            }
        }
        catch (Exception ex)
        {
            Console.WriteLine($"❌ Ошибка обработки TCP запроса: {ex.Message}");
        }
    }

    private static async Task<string> ReadRequestBodyAsync(HttpListenerRequest request)
    {
        if (!request.HasEntityBody)
            return string.Empty;

        try
        {
            using (var reader = new StreamReader(request.InputStream, request.ContentEncoding ?? Encoding.UTF8))
            {
                return await reader.ReadToEndAsync();
            }
        }
        catch (Exception ex)
        {
            Console.WriteLine($"❌ Ошибка чтения тела запроса: {ex.Message}");
            return string.Empty;
        }
    }

    private static bool IsValidJson(string jsonString)
    {
        if (string.IsNullOrWhiteSpace(jsonString))
            return false;

        try
        {
            using (JsonDocument.Parse(jsonString))
            {
                return true;
            }
        }
        catch (JsonException)
        {
            return false;
        }
    }

    
    private static async Task ProcessRequestAsync(RequestsClass requests, IResponseWriter writer)
    {
        try
        {
            Console.WriteLine($"🔧 Обработка запроса: {requests.ToString()}");

            switch (requests.NameRequests)
            {
                case "AddData":
                    if (requests.TypeObjects == "RegisterValues" && requests.ObjectList1 != null)
                    {
                        await db.AddData(requests.TypeObjects, requests.ObjectList1, writer);
                    }
                    else if (requests.ObjectsList != null)
                    {
                        await db.AddData(requests.TypeObjects, requests.ObjectsList, writer);
                    }
                    else
                    {
                        await writer.SendErrorAsync("Неверные параметры для AddData");
                    }
                    break;

                case "DeleteData":
                    if (requests.ObjectsList != null)
                    {
                        await db.DeleteItem(requests.TypeObjects, requests.ObjectsList, writer);
                    }
                    else
                    {
                        await writer.SendErrorAsync("Неверные параметры для DeleteData");
                    }
                    break;

                case "UpdateData":
                    if (requests.ObjectsList != null)
                    {
                        await db.UpdateData(requests.TypeObjects, requests.ObjectsList, writer);
                    }
                    else
                    {
                        await writer.SendErrorAsync("Неверные параметры для UpdateData");
                    }
                    break;

                case "GetAllData":
                    await GetAllDataAsync(writer);
                    break;

                case "GetOneDataForUpdate":
                    if (requests.ObjectsList != null)
                    {
                        await db.GetItemById(requests.TypeObjects, requests.ObjectsList, writer);
                    }
                    else
                    {
                        await writer.SendErrorAsync("Неверные параметры для GetOneDataForUpdate");
                    }
                    break;

                default:
                    await writer.SendErrorAsync($"Неизвестный запрос: {requests.NameRequests}");
                    break;
            }
        }
        catch (Exception ex)
        {
            Console.WriteLine($"❌ Ошибка обработки запроса: {ex.Message}");
            await writer.SendErrorAsync($"Ошибка обработки: {ex.Message}");
        }
    }

    private static void DbConnect()
    {
        db = new DbMethods();
    }
    private static async Task HandleTcpRequestsAsync()
    {
        while (_isRunning)
        {
            try
            {
                var tcpClient = await _tcpServer.AcceptTcpClientAsync();
                _ = Task.Run(async () => await ProcessTcpRequestAsync(tcpClient));
            }
            catch (ObjectDisposedException) when (!_isRunning)
            {
                // Игнорируем исключение при остановке сервера
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Ошибка TCP: {ex.Message}");
            }
        }
    }

    //private static async Task ProcessTcpRequestAsync(TcpClient tcpClient)
    //{
    //    try
    //    {
    //        Interlocked.Increment(ref _numRequest);
    //        Console.WriteLine($"\n🔌 TCP Запрос №{_numRequest}/Время: {DateTime.Now:HH:mm:ss}");
    //        Console.WriteLine($"🌐 Клиент: {tcpClient.Client.RemoteEndPoint}");

    //        using (tcpClient)
    //        using (var stream = tcpClient.GetStream())
    //        using (var reader = new StreamReader(stream, Encoding.UTF8))
    //        using (var writer = new StreamWriter(stream, Encoding.UTF8))
    //        {
    //            // Читаем данные от клиента
    //            StringBuilder requestBuilder = new StringBuilder();
    //            char[] buffer = new char[8000];
    //            int bytesRead;

    //            // Устанавливаем таймаут для чтения
    //            stream.ReadTimeout = 5000;

    //            try
    //            {
    //                while ((bytesRead = await reader.ReadAsync(buffer, 0, buffer.Length)) > 0)
    //                {
    //                    requestBuilder.Append(buffer, 0, bytesRead);

    //                    // Если получен полный JSON (проверяем по балансу скобок)
    //                    string currentData = requestBuilder.ToString();
    //                    if (IsValidJson(currentData))
    //                    {
    //                        break;
    //                    }
    //                }
    //            }
    //            catch (IOException)
    //            {
    //                // Таймаут или разрыв соединения - обрабатываем полученные данные
    //            }

    //            string requestBody = requestBuilder.ToString();
    //            RequestsClass requests = JsonSerializer.Deserialize<RequestsClass>(requestBody);
    //            Console.WriteLine($"📝 TCP Данные: {requests.ToString()}");

    //            switch(requests.NameRequests)
    //            {
    //                case "AddData":
    //                    if(requests.TypeObjects == "RegisterValues")
    //                    {
    //                        await db.AddData(requests.TypeObjects, requests.ObjectList1, writer);
    //                    }
    //                    else
    //                    await db.AddData(requests.TypeObjects, requests.ObjectsList, writer);
    //                    break;
    //                case "DeleteData":
    //                    await db.DeleteItem(requests.TypeObjects, requests.ObjectsList, writer);
    //                    break;
    //                case "UpdateData":
    //                    await db.UpdateData(requests.TypeObjects, requests.ObjectsList, writer);
                        
    //                    break;
    //                case "GetAllData":
    //                    await GetAllDataAsync(writer);
    //                    break;
    //                case "GetOneDataForUpdate":
    //                    await db.GetItemById(requests.TypeObjects, requests.ObjectsList,writer);
    //                    break;
                    
    //            }



                
    //        }
    //    }
    //    catch (Exception ex)
    //    {
    //        Console.WriteLine($"❌ Ошибка обработки TCP запроса: {ex.Message}");
    //    }
    //}



    //public static async Task GetAllDataAsync(IResponseWriter writer)
    //{
    //    try
    //    {
    //        List<Devices> _devices = await db.GetAllDataDevicesFromDb();
    //        List<Interfaces> _interfaces = await db.GetAllDataInterfaceFromDb();
    //        List<Registers> _registers = await db.GetAllDataRegistersFromDb();
    //        List<RegisterValues> _registerValues = await db.GetAllDataRegisterValuesFromDb();
    //        List<Logs> _logs = await db.GetAllDataLogsFromDb();

    //        var response = new ResponseGetAllData(_interfaces, _devices, _registers, _registerValues, _logs);

    //        var options = new JsonSerializerOptions
    //        {
    //            WriteIndented = false,
    //        };

    //        byte[] jsonData = JsonSerializer.SerializeToUtf8Bytes(response, options);

    //        await writer.BaseStream.WriteAsync(jsonData, 0, jsonData.Length);
    //        await writer.FlushAsync();

    //        Console.WriteLine($"✅ JSON Ответ отправлен ({jsonData.Length} bytes)");
    //    }
    //    catch (Exception ex)
    //    {
    //        Console.WriteLine($"❌ Ошибка в GetAllData: {ex.Message}");

    //        try
    //        {
    //            var errorResponse = new { Status = "error", Message = "Ошибка при отправке данных GetAllData" };
    //            byte[] errorData = JsonSerializer.SerializeToUtf8Bytes(errorResponse);
    //            await writer.BaseStream.WriteAsync(errorData, 0, errorData.Length);
    //            await writer.FlushAsync();
    //        }
    //        catch
    //        {
    //            // Игнорируем ошибки при отправке ошибки
    //        }
    //    }
    //}
    public static async Task GetAllDataAsync(IResponseWriter writer)
    {
        try
        {
            var allData = new
            {
                Interfaces = await db.GetAllDataInterfaceFromDb(),
                Devices = await db.GetAllDataDevicesFromDb(),
                Registers = await db.GetAllDataRegistersFromDb(),
                RegisterValues = await db.GetAllDataRegisterValuesFromDb(),
                Logs = await db.GetAllDataLogsFromDb()
            };

            string json = JsonSerializer.Serialize(allData, new JsonSerializerOptions
            {
                WriteIndented = false
            });

            await writer.WriteAsync(json);
            await writer.FlushAsync();

            Console.WriteLine("✅ Все данные отправлены");
        }
        catch (Exception ex)
        {
            Console.WriteLine($"❌ Ошибка получения всех данных: {ex.Message}");
            await writer.SendErrorAsync($"Ошибка получения данных: {ex.Message}");
        }
    }


    //private static bool IsValidJson(string json)
    //{
    //    if (string.IsNullOrWhiteSpace(json))
    //        return false;

    //    int braceCount = 0;
    //    int bracketCount = 0;
    //    bool inString = false;
    //    char prevChar = '\0';

    //    foreach (char c in json)
    //    {
    //        if (c == '"' && prevChar != '\\')
    //        {
    //            inString = !inString;
    //        }
    //        else if (!inString)
    //        {
    //            if (c == '{') braceCount++;
    //            else if (c == '}') braceCount--;
    //            else if (c == '[') bracketCount++;
    //            else if (c == ']') bracketCount--;
    //        }

    //        prevChar = c;
    //    }

    //    return braceCount == 0 && bracketCount == 0 && !inString;
    //}



    private static async Task HandleRequestsAsync()
    {
        while (_isRunning)
        {
            try
            {
                // Асинхронно ожидаем входящее подключение
                var context = await _server.GetContextAsync();

                // Обрабатываем каждый запрос в отдельной задаче
                _ = Task.Run(async () => await ProcessRequestAsync(context));
            }
            catch (HttpListenerException ex) when (!_isRunning)
            {
                // Игнорируем исключение при остановке сервера
                Console.WriteLine("Сервер остановлен");
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Ошибка: {ex.Message}");
            }
        }
    }

    private static async Task ProcessRequestAsync(HttpListenerContext context)
    {
        var request = context.Request;
        var response = context.Response;

        try
        {
            Console.WriteLine($"\nЗапрос №{_numRequest}/Время: {DateTime.Now:HH:mm:ss}");
            Console.WriteLine($"🌐 Клиент: {request.RemoteEndPoint}");
            Console.WriteLine($"🔗 URL: {request.Url}");
            Console.WriteLine($"📋 Метод: {request.HttpMethod}");

            
            // Читаем тело запроса (если есть)
            string requestBody = "";
            if (request.HasEntityBody)
            {
                using (var reader = new System.IO.StreamReader(request.InputStream, request.ContentEncoding))
                {
                    requestBody = await reader.ReadToEndAsync();
                    Console.WriteLine($"📝 Тело запроса: {requestBody}");
                }
            }
            else
            {

            }

            // Формируем ответ
            string responseText = GenerateResponse(request);
            byte[] buffer = Encoding.UTF8.GetBytes(responseText);

            response.ContentType = "application/json";
            response.ContentEncoding = Encoding.UTF8;
            response.ContentLength64 = buffer.Length;
            response.StatusCode = 200;

            // Отправляем ответ
            using (var output = response.OutputStream)
            {
                await output.WriteAsync(buffer, 0, buffer.Length);
                await output.FlushAsync();
            }

            Console.WriteLine($"✅ Ответ отправлен ({buffer.Length} bytes)");
        }
        catch (Exception ex)
        {
            Console.WriteLine($"❌ Ошибка обработки запроса: {ex.Message}");
            response.StatusCode = 500;
            response.Close();
        }
    }

    private static string GenerateResponse(HttpListenerRequest request)
    {
        return $@"{{
    ""status"": ""success"",
    ""message"": ""Запрос обработан"",
    ""timestamp"": ""{DateTime.Now:yyyy-MM-dd HH:mm:ss}"",
    ""client"": ""{request.RemoteEndPoint}"",
    ""method"": ""{request.HttpMethod}"",
    ""url"": ""{request.Url}"",
    ""headers"": {JsonSerializeHeaders(request.Headers)}
}}";
    }

    private static string JsonSerializeHeaders(System.Collections.Specialized.NameValueCollection headers)
    {
        var dict = new System.Collections.Generic.Dictionary<string, string>();
        foreach (string key in headers.Keys)
        {
            dict[key] = headers[key];
        }
        return System.Text.Json.JsonSerializer.Serialize(dict);
    }
}


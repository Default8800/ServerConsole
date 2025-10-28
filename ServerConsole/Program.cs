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
        _server.Prefixes.Add("http://127.0.0.1:8888/connection/");
        _server.Prefixes.Add("http://localhost:8888/connection/");


        _tcpServer = new TcpListener(IPAddress.Any, 8889);
        DbConnect();

        _server.Start();
        _tcpServer.Start();

        Console.WriteLine("Сервер запущен\nНажмите Ctrl+C для остановки сервера");

        // Обработка Ctrl+C
        Console.CancelKeyPress += (sender, e) =>
        {
            e.Cancel = true;
            _isRunning = false;
            _server.Stop();
            Console.WriteLine("Сервер остановлен");
        };

        // Запускаем обработку запросов
        await Task.WhenAll(HandleRequestsAsync(), HandleTcpRequestsAsync());
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
                RequestsClass requests = JsonSerializer.Deserialize<RequestsClass>(requestBody);
                Console.WriteLine($"📝 TCP Данные: {requests.ToString()}");

                switch(requests.NameRequests)
                {
                    case "AddData":
                        if(requests.TypeObjects == "RegisterValues")
                        {
                            await db.AddData(requests.TypeObjects, requests.ObjectList1, writer);
                        }
                        else
                        await db.AddData(requests.TypeObjects, requests.ObjectsList, writer);
                        break;
                    case "DeleteData":
                        await db.DeleteItem(requests.TypeObjects, requests.ObjectsList, writer);
                        break;
                    case "UpdateData":
                        await db.UpdateData(requests.TypeObjects, requests.ObjectsList, writer);
                        
                        break;
                    case "GetAllData":
                        await GetAllDataAsync(writer);
                        break;
                    case "GetOneDataForUpdate":
                        await db.GetItemById(requests.TypeObjects, requests.ObjectsList,writer);
                        break;
                    
                }



                
            }
        }
        catch (Exception ex)
        {
            Console.WriteLine($"❌ Ошибка обработки TCP запроса: {ex.Message}");
        }
    }



    private static async Task GetAllDataAsync(StreamWriter writer)
    {
        try
        {
            List<Devices> _devices = await db.GetAllDataDevicesFromDb();
            List<Interfaces> _interfaces = await db.GetAllDataInterfaceFromDb();
            List<Registers> _registers = await db.GetAllDataRegistersFromDb();
            List<RegisterValues> _registerValues = await db.GetAllDataRegisterValuesFromDb();
            List<Logs> _logs = await db.GetAllDataLogsFromDb();

            var response = new ResponseGetAllData(_interfaces, _devices, _registers, _registerValues, _logs);

            var options = new JsonSerializerOptions
            {
                WriteIndented = false,
            };

            byte[] jsonData = JsonSerializer.SerializeToUtf8Bytes(response, options);

            await writer.BaseStream.WriteAsync(jsonData, 0, jsonData.Length);
            await writer.FlushAsync();

            Console.WriteLine($"✅ JSON Ответ отправлен ({jsonData.Length} bytes)");
        }
        catch (Exception ex)
        {
            Console.WriteLine($"❌ Ошибка в GetAllData: {ex.Message}");

            try
            {
                var errorResponse = new { Status = "error", Message = "Ошибка при отправке данных GetAllData" };
                byte[] errorData = JsonSerializer.SerializeToUtf8Bytes(errorResponse);
                await writer.BaseStream.WriteAsync(errorData, 0, errorData.Length);
                await writer.FlushAsync();
            }
            catch
            {
                // Игнорируем ошибки при отправке ошибки
            }
        }
    }


    private static bool IsValidJson(string json)
    {
        if (string.IsNullOrWhiteSpace(json))
            return false;

        int braceCount = 0;
        int bracketCount = 0;
        bool inString = false;
        char prevChar = '\0';

        foreach (char c in json)
        {
            if (c == '"' && prevChar != '\\')
            {
                inString = !inString;
            }
            else if (!inString)
            {
                if (c == '{') braceCount++;
                else if (c == '}') braceCount--;
                else if (c == '[') bracketCount++;
                else if (c == ']') bracketCount--;
            }

            prevChar = c;
        }

        return braceCount == 0 && bracketCount == 0 && !inString;
    }



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


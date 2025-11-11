using Microsoft.EntityFrameworkCore;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Security.Cryptography.X509Certificates;
using System.Text;
using System.Text.Json;
using System.Threading.Tasks;
using static Program;

namespace ServerConsole.DataClasses
{
    internal class DbMethods : IDisposable
    {

        private readonly ApplicationContext _context;

        public DbMethods()
        {
            _context = new ApplicationContext();
            try
            {
                _context.Database.EnsureCreated();
                AddLogs("База данных и таблицы созданы", "Успех");
                Console.WriteLine("База данных и таблицы созданы");

                // Проверяем создание таблиц
                CheckTablesExist();
            }
            catch (Exception ex)
            {
                AddLogs("Ошибка создания базы:", "Фатальная ошибка");
                Console.WriteLine($"Ошибка создания базы: {ex.Message}");
            }

        }

        private void CheckTablesExist()
        {
            try
            {
                // Проверяем таблицы
                var interfacesCount = _context.Interfaces.Count();
                var devicesCount = _context.Devices.Count();
                var registersCount = _context.Registers.Count();
                var registerValuesCount = _context.RegisterValues.Count();

                Console.WriteLine($"Таблицы: Interfaces({interfacesCount}), Devices({devicesCount}), Registers({registersCount}), RegisterValues({registerValuesCount})");

                // Добавляем данные если таблицы пустые
                if (interfacesCount == 0) AddTestData();
            }
            catch (Exception ex)
            {
                AddLogs($"Ошибка: {ex.Message}, пересоздаем БД", "Ошибка");
                Console.WriteLine($"Ошибка: {ex.Message}, пересоздаем БД");
                _context.Database.EnsureDeleted();
                _context.Database.EnsureCreated();
                AddTestData();
            }
        }

        private void AddTestData()//это было написано через иишку , потому что было лень придумывать тестовые данные
        {
            try
            {
                // Интерфейсы
                var interfaces = new List<Interfaces>
    {
        new Interfaces("Интерфейс 1", "Описание 1"),
        new Interfaces("Интерфейс 2", "Описание 2"),
        new Interfaces("Интерфейс 3", "Описание 3")
    };
                _context.Interfaces.AddRange(interfaces);
                _context.SaveChanges(); // Здесь генерируются реальные ID

                // Теперь используем реальные ID интерфейсов
                var devices = new List<Devices>
    {
        new Devices(interfaces[0].Id, "Устройство 1", "Описание устройства 1", true, "Круг ●", 50, 10, 10, "#FF0000"),
        new Devices(interfaces[1].Id, "Устройство 2", "Описание устройства 2", true, "Квадрат ■", 60, 50, 50, "#0000FF"),
        new Devices(interfaces[2].Id, "Устройство 3", "Описание устройства 3", false, "Треугольник ▲", 40, 100, 100, "#008000")
    };
                _context.Devices.AddRange(devices);
                _context.SaveChanges();

                // Регистры (предполагая, что они связаны с устройствами)
                var registers = new List<Registers>
    {
        new Registers(devices[0].Id, "Регистр 1", "Описание регистра 1"),
        new Registers(devices[1].Id, "Регистр 2", "Описание регистра 2"),
        new Registers(devices[2].Id, "Регистр 3", "Описание регистра 3")
    };
                _context.Registers.AddRange(registers);
                _context.SaveChanges();

                // Значения регистров
                var random = new Random();
                var registerValues = new List<RegisterValues>();
                var now = DateTime.UtcNow;

                // Используем реальные ID регистров
                for (int i = 0; i < registers.Count; i++)
                {
                    for (int j = 0; j < 3; j++)
                    {
                        registerValues.Add(new RegisterValues
                        {
                            RegisterId = registers[i].Id,
                            Value = (float)(random.NextDouble() * 100),
                            Timestamp = now.AddHours(-j)
                        });
                    }
                }
                _context.RegisterValues.AddRange(registerValues);
                _context.SaveChanges();

                AddLogs("Тестовые данные добавлены во все таблицы", "Успех");
                Console.WriteLine(" Тестовые данные добавлены во все таблицы");
                var interfacesCount = _context.Interfaces.Count();
                var devicesCount = _context.Devices.Count();
                var registersCount = _context.Registers.Count();
                var registerValuesCount = _context.RegisterValues.Count();

                Console.WriteLine($"Таблицы: Interfaces({interfacesCount}), Devices({devicesCount}), Registers({registersCount}), RegisterValues({registerValuesCount})");
            }
            catch (Exception ex)
            {
                AddLogs($"Ошибка добавления тестовых данных: {ex.Message}", "Ошибка");
                Console.WriteLine($" Ошибка добавления тестовых данных: {ex.Message}");
            }
        }

        // Добавление данных
        public async Task AddData(string typeObjects, List<object> Item, IResponseWriter writer)
        {
            try
            {
                switch (typeObjects)
                {
                    case "Interfaces":
                        if (Item is List<object> objectList && objectList.Count >= 4)
                        {
                            var interfaceObj = new Interfaces
                            {
                                Name = GetSafeString(objectList[1]),
                                Description = GetSafeString(objectList[2]),
                                EditingDate = DateTime.UtcNow
                            };

                            
                            await _context.Interfaces.AddAsync(interfaceObj);
                            await _context.SaveChangesAsync();

                            
                            Console.WriteLine($"Создан интерфейс с ID: {interfaceObj.Id}");

                           

                            GetAllDataAsync(writer);
                            AddLogs($"Добавлен интерфейс: {interfaceObj.Name}", "Успех");
                            Console.WriteLine($" Добавлен интерфейс: {interfaceObj.Name}");
                        }
                        break;
                    case "Devices":
                        if (Item is List<object> objectList1 && objectList1.Count >= 4)
                        {
                            int interfaceId = GetSafeInt(objectList1[1]);

                            // Проверяем существует ли такой интерфейс
                            bool interfaceExists = await _context.Interfaces
                                .AnyAsync(i => i.Id == interfaceId);

                            if (!interfaceExists)
                            {
                                string errorMsg = $"Интерфейс с ID {interfaceId} не существует";
                                AddLogs(errorMsg, "Ошибка");
                                Console.WriteLine(errorMsg);
                                return;
                            }

                            var deviceObj = new Devices
                            {
                                InterfaceId = interfaceId,
                                Name = GetSafeString(objectList1[2]),
                                Description = GetSafeString(objectList1[3]),
                                IsEnabled = objectList1.Count > 4 ? GetSafeBool(objectList1[4]) : true,
                                FigureType = objectList1.Count > 5 ? GetSafeString(objectList1[5]) : "Circle",
                                Size = objectList1.Count > 6 ? GetSafeInt(objectList1[6]) : 50,
                                PosX = objectList1.Count > 7 ? GetSafeInt(objectList1[7]) : 0,
                                PosY = objectList1.Count > 8 ? GetSafeInt(objectList1[8]) : 0,
                                Color = objectList1.Count > 9 ? GetSafeString(objectList1[9]) : "#000000",
                                EditingDate = DateTime.UtcNow
                            };

                            await _context.Devices.AddAsync(deviceObj);
                            await _context.SaveChangesAsync();
                            GetAllDataAsync(writer);
                            AddLogs($"Добавлено устройство: {deviceObj.Name}", "Успех");
                            Console.WriteLine($"Добавлено устройство: {deviceObj.Name}");
                        }
                        break;
                    case "Registers":
                        if (Item is List<object> objectList2 && objectList2.Count >= 5)
                        {
                            int deviceId = GetSafeInt(objectList2[1]);
                            float initialValue = GetSafeFloat(objectList2[4]);

                            bool deviceExists = await _context.Devices
                                .AnyAsync(d => d.Id == deviceId);

                            if (!deviceExists)
                            {
                                string errorMsg = $"❌ Устройство с ID {deviceId} не существует";
                                AddLogs(errorMsg, "Ошибка");
                                Console.WriteLine(errorMsg);
                                return;
                            }

                            var registerObj = new Registers
                            {
                                DeviceId = deviceId,
                                Name = GetSafeString(objectList2[2]),
                                Description = GetSafeString(objectList2[3]),
                                EditingDate = DateTime.UtcNow
                            };

                            await _context.Registers.AddAsync(registerObj);
                            await _context.SaveChangesAsync();

                            // АВТОМАТИЧЕСКИ ДОБАВЛЯЕМ ЗНАЧЕНИЕ РЕГИСТРА
                            var registerValue = new RegisterValues
                            {
                                RegisterId = registerObj.Id,
                                Value = initialValue,
                                Timestamp = DateTime.UtcNow
                            };

                            await _context.RegisterValues.AddAsync(registerValue);
                            await _context.SaveChangesAsync();
                            GetAllDataAsync(writer);
                            string successMsg = $"Регистр создан и начальное значение {initialValue} добавлено";
                            AddLogs(successMsg, "Успех");
                            Console.WriteLine(successMsg);
                        }
                        break;

                    case "RegisterValues":
                        // Если пришел список значений (пачка)
                        if (Item is List<object> objectList3 && objectList3.Count > 0)
                        {
                            // Проверяем первый элемент чтобы понять формат
                            if (objectList3.Count >= 2)
                            {
                                // Это пачка значений: [[registerId1, value1], [registerId2, value2], ...]
                                await AddMultipleRegisterValues(objectList3);
                            }
                            else if (objectList3[0] is List<object>)
                            {
                                // Это одиночное значение: [registerId, value]
                                await AddSingleRegisterValue(objectList3);
                            }
                        }
                        break;

                    case "Logs":
                        if (Item is List<object> objectList4 && objectList4.Count >= 3)
                        {
                            var logObj = new Logs
                            {
                                Message = GetSafeString(objectList4[1]),
                                Type = GetSafeString(objectList4[2]),
                                Timestamp = objectList4.Count > 3 ? GetSafeDateTime(objectList4[3]) : DateTime.UtcNow
                            };

                            await _context.Logs.AddAsync(logObj);
                            await _context.SaveChangesAsync();

                            AddLogs($"Добавлен лог: {logObj.Message}", "Успех");
                            Console.WriteLine($"Добавлен лог: {logObj.Message}");
                        }
                        break;
                }
            }
            catch (Exception ex)
            {
                string errorMsg = $"Ошибка добавления: {ex.Message}";
                AddLogs(errorMsg, "Ошибка");
                Console.WriteLine($"❌ {errorMsg}");
                await SendErrorResponseAsync(writer, ex.Message);
            }
        }

        private async Task AddSingleRegisterValue(List<object> objectList3)
        {
            try
            {
                int registerId = GetSafeInt(objectList3[0]);

                bool registerExists = await _context.Registers.AnyAsync(r => r.Id == registerId);
                if (!registerExists)
                {
                    string errorMsg = $"Регистр с ID {registerId} не существует";
                    AddLogs(errorMsg, "Ошибка");
                    Console.WriteLine($"❌ {errorMsg}");
                    return;
                }

                var registerValueObj = new RegisterValues
                {
                    RegisterId = registerId,
                    Value = GetSafeFloat(objectList3[1]),
                    Timestamp = objectList3.Count > 2 ? GetSafeDateTime(objectList3[2]) : DateTime.UtcNow
                };

                await _context.RegisterValues.AddAsync(registerValueObj);
                await _context.SaveChangesAsync();

                string successMsg = $"Добавлено значение {registerValueObj.Value} для регистра {registerId}";
                AddLogs(successMsg, "Успех");
                Console.WriteLine($"✅ {successMsg}");
            }
            catch (Exception ex)
            {
                string errorMsg = $"Ошибка добавления значения регистра: {ex.Message}";
                AddLogs(errorMsg, "Ошибка");
                Console.WriteLine($"❌ {errorMsg}");
            }
        }
        private async Task AddMultipleRegisterValues(List<object> batchData)
        {
            try
            {
                

                var registerValuesToAdd = new List<RegisterValues>();

                foreach (var item in batchData)
                {

                    // Преобразуем элемент в строку и парсим как JSON массив
                    string jsonArray = item.ToString();
                    var array = Newtonsoft.Json.JsonConvert.DeserializeObject<object[]>(jsonArray);

                    
                    if (array.Length >= 2)
                    {
                        var registerId = GetSafeInt(array[0]);
                        var value = GetSafeFloat(array[1]);

                        // Проверяем существование регистра
                        bool registerExists = await _context.Registers.AnyAsync(r => r.Id == registerId);
                        if (!registerExists)
                        {
                            string warnMsg = $"Регистр с ID {registerId} не существует, пропускаем";
                            AddLogs(warnMsg, "Предупреждение");
                            Console.WriteLine($" {warnMsg}");
                            continue;
                        }

                        var registerValueObj = new RegisterValues
                        {
                            RegisterId = registerId,
                            Value = value,
                            Timestamp =  DateTime.UtcNow
                        };

                        registerValuesToAdd.Add(registerValueObj);
                    }
                }

                if (registerValuesToAdd.Count > 0)
                {
                    await _context.RegisterValues.AddRangeAsync(registerValuesToAdd);
                    await _context.SaveChangesAsync();

                    string successMsg = $"Добавлено {registerValuesToAdd.Count} значений регистров";
                    AddLogs(successMsg, "Успех");
                    Console.WriteLine($" {successMsg}");
                }
            }
            catch (Exception ex)
            {
                string errorMsg = $"Ошибка добавления пачки значений: {ex.Message}";
                AddLogs(errorMsg, "Ошибка");
                Console.WriteLine($" {errorMsg}");
            }
        }
        

        private static async Task SendErrorResponseAsync(IResponseWriter writer, string error)
        {
            var response = new { Status = "error", Message = error };
            string json = JsonSerializer.Serialize(response);
            await writer.WriteAsync(json);
            await writer.FlushAsync();
        }


        private float GetSafeFloat(object value)
        {
            if (value == null) return 0f;
            if (float.TryParse(value.ToString(), out float result)) return result;
            return 0f;
        }

        private DateTime GetSafeDateTime(object value)
        {
            if (value == null) return DateTime.UtcNow;
            if (DateTime.TryParse(value.ToString(), out DateTime result)) return result;
            return DateTime.UtcNow;
        }
        private bool GetSafeBool(object obj)
        {
            if (obj == null) return true; 

            if (obj is bool boolValue)
                return boolValue;

            if (obj is string stringValue)
            {
                if (bool.TryParse(stringValue, out bool result))
                    return result;
                return stringValue.ToLower() == "true" || stringValue == "1";
            }

            
            if (obj is JsonElement jsonElement)
            {
                if (jsonElement.ValueKind == JsonValueKind.True) return true;
                if (jsonElement.ValueKind == JsonValueKind.False) return false;
                if (jsonElement.ValueKind == JsonValueKind.String)
                    return jsonElement.GetString().ToLower() == "true";
                if (jsonElement.ValueKind == JsonValueKind.Number)
                    return jsonElement.GetInt32() == 1;
            }

            return true; 
        }
        private static string GetSafeString(object obj) => obj?.ToString() ?? string.Empty;

        #region Получение всех интерфейсов
        public async Task<List<Interfaces>> GetAllDataInterfaceFromDb()
        {
            try
            {
                var result = await _context.Interfaces
                            .OrderBy(i => i.Id)
                            .AsNoTracking()
                            .ToListAsync();

                AddLogs($"Получено {result.Count} интерфейсов", "Успех");
                return result;
            }
            catch (Exception ex)
            {
                AddLogs($"Ошибка получения интерфейсов: {ex.Message}", "Ошибка");
                return new List<Interfaces>();
            }
        }

        public async Task<List<Devices>> GetAllDataDevicesFromDb()
        {
            try
            {
                var result = await _context.Devices
                            .OrderBy(d => d.Id)
                            .AsNoTracking()
                            .ToListAsync();

                AddLogs($"Получено {result.Count} устройств", "Успех");
                return result;
            }
            catch (Exception ex)
            {
                AddLogs($"Ошибка получения устройств: {ex.Message}", "Ошибка");
                return new List<Devices>();
            }
        }

        public async Task<List<Registers>> GetAllDataRegistersFromDb()
        {
            try
            {
                var result = await _context.Registers
                            .OrderBy(r => r.Id)
                            .AsNoTracking()
                            .ToListAsync();

                AddLogs($"Получено {result.Count} регистров", "Успех");
                return result;
            }
            catch (Exception ex)
            {
                AddLogs($"Ошибка получения регистров: {ex.Message}", "Ошибка");
                return new List<Registers>();
            }
        }

        public async Task<List<RegisterValues>> GetAllDataRegisterValuesFromDb()
        {
            try
            {
                var result = await _context.RegisterValues
                            .OrderBy(rv => rv.Id)
                            .AsNoTracking()
                            .ToListAsync();

                AddLogs($"Получено {result.Count} значений регистров", "Успех");
                return result;
            }
            catch (Exception ex)
            {
                AddLogs($"Ошибка получения значений регистров: {ex.Message}", "Ошибка");
                return new List<RegisterValues>();
            }
        }

        public async Task<List<Logs>> GetAllDataLogsFromDb()
        {
            try
            {
                var result = await _context.Logs
                            .OrderBy(l => l.Id)
                            .AsNoTracking()
                            .ToListAsync();

                AddLogs($"Получено {result.Count} логов", "Успех");
                return result;
            }
            catch (Exception ex)
            {
                AddLogs($"Ошибка получения логов: {ex.Message}", "Ошибка");
                return new List<Logs>();
            }
        }
        #endregion

        public async Task GetItemById(string typeObjects, List<object> Item, IResponseWriter writer)
        {
            try
            {
                var response = new ResponseGetOneItem(" ", new object());
                object _item = new object();
                List<object> _objectListRegValue = new List<object>();

                switch (typeObjects)
                {
                    case "Interfaces":
                        if (int.TryParse(Item[0].ToString(), out int interfaceId))
                        {
                            _item = await _context.Interfaces
                                .AsNoTracking()
                                .FirstOrDefaultAsync(i => i.Id == interfaceId);
                            response = new ResponseGetOneItem("Interfaces", _item);

                            if (_item != null)
                                AddLogs($"Получен интерфейс с ID {interfaceId}", "Успех");
                        }
                        break;

                    case "Devices":
                        if (int.TryParse(Item[0].ToString(), out int devicesId))
                        {
                            _item = await _context.Devices
                                .AsNoTracking()
                                .FirstOrDefaultAsync(i => i.Id == devicesId);
                            response = new ResponseGetOneItem("Devices", _item);

                            if (_item != null)
                                AddLogs($"Получено устройство с ID {devicesId}", "Успех");
                        }
                        break;

                    case "Registers":
                        if (int.TryParse(Item[0].ToString(), out int registersId))
                        {
                            _item = await _context.Registers
                                .AsNoTracking()
                                .FirstOrDefaultAsync(i => i.Id == registersId);
                            var registerValuesList = await _context.RegisterValues
                                .Where(rv => rv.RegisterId == registersId)
                                .OrderByDescending(rv => rv.Timestamp)
                                .AsNoTracking()
                                .ToListAsync();

                            _objectListRegValue = registerValuesList.Cast<object>().ToList();
                            response = new ResponseGetOneItem("Registers", _item, _objectListRegValue);

                            if (_item != null)
                                AddLogs($"Получен регистр с ID {registersId}", "Успех");
                        }
                        break;

                    case "RegisterValues":
                        if (int.TryParse(Item[0].ToString(), out int registerValuesId))
                        {
                            _item = await _context.RegisterValues
                                .AsNoTracking()
                                .FirstOrDefaultAsync(i => i.Id == registerValuesId);

                            response = new ResponseGetOneItem("RegisterValues", _item, _objectListRegValue);

                            if (_item != null)
                                AddLogs($"Получено значение регистра с ID {registerValuesId}", "Успех");
                        }
                        break;

                    case "Logs":
                        if (int.TryParse(Item[0].ToString(), out int logsId))
                        {
                            _item = await _context.Logs
                                .AsNoTracking()
                                .FirstOrDefaultAsync(i => i.Id == logsId);

                            response = new ResponseGetOneItem("Logs", _item);

                            if (_item != null)
                                AddLogs($"Получен лог с ID {logsId}", "Успех");
                        }
                        break;

                    default:
                        string errorMsg = $"Неизвестный тип объекта: {typeObjects}";
                        AddLogs(errorMsg, "Ошибка");
                        Console.WriteLine($"{errorMsg}");
                        await writer.SendErrorAsync(errorMsg);
                        return;
                }

                // Проверяем, найден ли объект
                if (_item == null)
                {
                    string notFoundMsg = $"Объект с ID {Item[0]} не найден";
                    AddLogs(notFoundMsg, "Предупреждение");
                    var notFoundResponse = new { Status = "not_found", Message = notFoundMsg };
                    string notFoundJson = JsonSerializer.Serialize(notFoundResponse);
                    await writer.WriteAsync(notFoundJson);
                    await writer.FlushAsync();
                    return;
                }

                var options = new JsonSerializerOptions
                {
                    WriteIndented = false,
                };

                string jsonData = JsonSerializer.Serialize(response, options);
                await writer.WriteAsync(jsonData);
                await writer.FlushAsync();

                AddLogs($"JSON Ответ отправлен ({jsonData.Length} bytes)", "Успех");
                Console.WriteLine($"✅ JSON Ответ отправлен ({jsonData.Length} bytes)");
            }
            catch (Exception ex)
            {
                string errorMsg = $"Ошибка в GetOneItem: {ex.Message}";
                AddLogs(errorMsg, "Ошибка");
                Console.WriteLine($"❌ {errorMsg}");

                // Отправляем ошибку клиенту через IResponseWriter
                await writer.SendErrorAsync(ex.Message);
            }
        }

        //public async Task GetItemById(string typeObjects, List<object> Item, IResponseWriter writer)
        //{
        //    try
        //    {
        //        var response = new ResponseGetOneItem(" ", new object());
        //        object _item = new object();
        //        List<object> _objectListRegValue = new List<object>();

        //        switch (typeObjects)
        //        {
        //            case "Interfaces":
        //                if (int.TryParse(Item[0].ToString(), out int interfaceId))
        //                {
        //                    _item = await _context.Interfaces
        //                        .AsNoTracking()
        //                        .FirstOrDefaultAsync(i => i.Id == interfaceId);
        //                    response = new ResponseGetOneItem("Interfaces", _item);

        //                    if (_item != null)
        //                        AddLogs($"Получен интерфейс с ID {interfaceId}", "Успех");
        //                }
        //                break;

        //            case "Devices":
        //                if (int.TryParse(Item[0].ToString(), out int devicesId))
        //                {
        //                    _item = await _context.Devices
        //                        .AsNoTracking()
        //                        .FirstOrDefaultAsync(i => i.Id == devicesId);
        //                    response = new ResponseGetOneItem("Devices", _item);

        //                    if (_item != null)
        //                        AddLogs($"Получено устройство с ID {devicesId}", "Успех");
        //                }
        //                break;

        //            case "Registers":
        //                if (int.TryParse(Item[0].ToString(), out int registersId))
        //                {
        //                    _item = await _context.Registers
        //                        .AsNoTracking()
        //                        .FirstOrDefaultAsync(i => i.Id == registersId);
        //                    var registerValuesList = await _context.RegisterValues
        //                        .OrderBy(d => d.RegisterId)
        //                        .AsNoTracking()
        //                        .ToListAsync();

        //                    _objectListRegValue = registerValuesList.Cast<object>().ToList();
        //                    response = new ResponseGetOneItem("Registers", _item, _objectListRegValue);

        //                    if (_item != null)
        //                        AddLogs($"Получен регистр с ID {registersId}", "Успех");
        //                }
        //                break;

        //            case "RegisterValues":
        //                if (int.TryParse(Item[0].ToString(), out int registerValuesId))
        //                {
        //                    _item = await _context.RegisterValues
        //                        .AsNoTracking()
        //                        .FirstOrDefaultAsync(i => i.Id == registerValuesId);

        //                    response = new ResponseGetOneItem("RegisterValues", _item, _objectListRegValue);

        //                    if (_item != null)
        //                        AddLogs($"Получено значение регистра с ID {registerValuesId}", "Успех");
        //                }
        //                break;

        //            case "Logs":
        //                if (int.TryParse(Item[0].ToString(), out int logsId))
        //                {
        //                    _item = await _context.Logs
        //                        .AsNoTracking()
        //                        .FirstOrDefaultAsync(i => i.Id == logsId);

        //                    response = new ResponseGetOneItem("Logs", _item);

        //                    if (_item != null)
        //                        AddLogs($"Получен лог с ID {logsId}", "Успех");
        //                }
        //                break;

        //            default:
        //                string errorMsg = $"Неизвестный тип объекта: {typeObjects}";
        //                AddLogs(errorMsg, "Ошибка");
        //                Console.WriteLine($"❌ {errorMsg}");
        //                var errorResponse = new { Status = "error", Message = errorMsg };
        //                byte[] errorData = JsonSerializer.SerializeToUtf8Bytes(errorResponse);
        //                await writer.BaseStream.WriteAsync(errorData, 0, errorData.Length);
        //                await writer.FlushAsync();
        //                return;
        //        }

        //        // Проверяем, найден ли объект
        //        if (_item == null)
        //        {
        //            string notFoundMsg = $"Объект с ID {Item[0]} не найден";
        //            AddLogs(notFoundMsg, "Предупреждение");
        //            var notFoundResponse = new { Status = "not_found", Message = notFoundMsg };
        //            byte[] notFoundData = JsonSerializer.SerializeToUtf8Bytes(notFoundResponse);
        //            await writer.BaseStream.WriteAsync(notFoundData, 0, notFoundData.Length);
        //            await writer.FlushAsync();
        //            return;
        //        }

        //        var options = new JsonSerializerOptions
        //        {
        //            WriteIndented = false,
        //        };

        //        byte[] jsonData = JsonSerializer.SerializeToUtf8Bytes(response, options);

        //        await writer.BaseStream.WriteAsync(jsonData, 0, jsonData.Length);
        //        await writer.FlushAsync();

        //        AddLogs($"JSON Ответ отправлен ({jsonData.Length} bytes)", "Успех");
        //        Console.WriteLine($"✅ JSON Ответ отправлен ({jsonData.Length} bytes)");
        //    }
        //    catch (Exception ex)
        //    {
        //        string errorMsg = $"Ошибка в GetOneItem: {ex.Message}";
        //        AddLogs(errorMsg, "Ошибка");
        //        Console.WriteLine($"❌ {errorMsg}");

        //        // Отправляем ошибку клиенту
        //        try
        //        {
        //            var errorResponse = new { Status = "error", Message = ex.Message };
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





        private int GetSafeInt(object obj)
        {
            if (obj == null) return 0;

            Console.WriteLine($"🔍 GetSafeInt получил: {obj} типа {obj.GetType()}");

            try
            {
                // Обработка JsonElement
                if (obj is JsonElement jsonElement)
                {
                    Console.WriteLine($"🔍 JsonElement.ValueKind: {jsonElement.ValueKind}");

                    return jsonElement.ValueKind switch
                    {
                        JsonValueKind.Number => jsonElement.GetInt32(),
                        JsonValueKind.String => int.TryParse(jsonElement.GetString(), out int result) ? result : 0,
                        JsonValueKind.True => 1,
                        JsonValueKind.False => 0,
                        _ => 0
                    };
                }

                // Обработка других типов
                return obj switch
                {
                    int i => i,
                    long l => (int)l,
                    string s when int.TryParse(s, out int result) => result,
                    _ => 0
                };
            }
            catch (Exception ex)
            {
                Console.WriteLine($"❌ Ошибка в GetSafeInt: {ex.Message}");
                return 0;
            }
        }

        // Обновление интерфейса
        public async Task UpdateData(string typeObjects, List<object> Item, IResponseWriter writer)
        {
            try
            {
                switch (typeObjects)
                {
                    case "Interfaces":
                        if (Item is List<object> objectList && objectList.Count >= 4)
                        {
                            try
                            {
                                int id = objectList[0] switch
                                {
                                    JsonElement { ValueKind: JsonValueKind.Number } jsonElement => jsonElement.GetInt32(),
                                    JsonElement { ValueKind: JsonValueKind.String } jsonElement => int.TryParse(jsonElement.GetString(), out int result) ? result : 0,
                                    int i => i,
                                    long l => (int)l,
                                    _ => 0
                                };

                                // НАХОДИМ существующую запись
                                var existingInterface = await _context.Interfaces
                                    .FirstOrDefaultAsync(i => i.Id == id);

                                if (existingInterface != null)
                                {
                                    //тут само обновление пошло
                                    existingInterface.Name = GetSafeString(objectList[1]);
                                    existingInterface.Description = GetSafeString(objectList[2]);
                                    existingInterface.EditingDate = DateTime.UtcNow;

                                    int changes = await _context.SaveChangesAsync();

                                    AddLogs($"Обновлен интерфейс с ID {id}", "Успех");
                                    Console.WriteLine($"✅ Обновлен интерфейс с ID {id}");
                                    await Program.GetAllDataAsync(writer);
                                }
                                else
                                {
                                    string errorMsg1 = $"Интерфейс с ID {id} не найден";
                                    AddLogs(errorMsg1, "Ошибка");
                                    Console.WriteLine($"❌ {errorMsg1}");
                                }
                            }
                            catch (Exception ex)
                            {
                                string errorMsg2 = $"Ошибка при обновлении интерфейса: {ex.Message}";
                                AddLogs(errorMsg2, "Ошибка");
                                Console.WriteLine($"❌ {errorMsg2}");
                            }
                        }
                        else
                        {
                            string errorMsg3 = "Недостаточно данных для обновления интерфейса";
                            AddLogs(errorMsg3, "Ошибка");
                            Console.WriteLine($"❌ {errorMsg3}");
                        }
                        break;

                    case "Devices":
                        if (Item is List<object> objectList1)
                        {
                            try
                            {
                                int id = objectList1[0] switch
                                {
                                    JsonElement { ValueKind: JsonValueKind.Number } jsonElement => jsonElement.GetInt32(),
                                    JsonElement { ValueKind: JsonValueKind.String } jsonElement => int.TryParse(jsonElement.GetString(), out int result) ? result : 0,
                                    int i => i,
                                    long l => (int)l,
                                    _ => 0
                                };

                                // НАХОДИМ существующую запись
                                var existingDevice = await _context.Devices
                                    .FirstOrDefaultAsync(i => i.Id == id);

                                if (existingDevice != null)
                                {
                                    // Проверяем существует ли интерфейс
                                    int interfaceId = GetSafeInt(objectList1[1]);
                                    bool interfaceExists = await _context.Interfaces.AnyAsync(i => i.Id == interfaceId);

                                    if (!interfaceExists)
                                    {
                                        string errorMsg1 = $"Интерфейс с ID {interfaceId} не существует";
                                        AddLogs(errorMsg1, "Ошибка");
                                        Console.WriteLine($"❌ {errorMsg1}");
                                        return;
                                    }

                                    //тут само обновление пошло
                                    existingDevice.InterfaceId = interfaceId;
                                    existingDevice.Name = GetSafeString(objectList1[2]);
                                    existingDevice.Description = GetSafeString(objectList1[3]);
                                    existingDevice.EditingDate = DateTime.UtcNow;
                                    existingDevice.IsEnabled = objectList1.Count > 4 ? GetSafeBool(objectList1[4]) : existingDevice.IsEnabled;
                                    existingDevice.FigureType = objectList1.Count > 5 ? GetSafeString(objectList1[5]) : existingDevice.FigureType;
                                    existingDevice.Size = objectList1.Count > 6 ? GetSafeInt(objectList1[6]) : existingDevice.Size;
                                    existingDevice.PosX = objectList1.Count > 7 ? GetSafeInt(objectList1[7]) : existingDevice.PosX;
                                    existingDevice.PosY = objectList1.Count > 8 ? GetSafeInt(objectList1[8]) : existingDevice.PosY;
                                    existingDevice.Color = objectList1.Count > 9 ? GetSafeString(objectList1[9]) : existingDevice.Color;

                                    int changes = await _context.SaveChangesAsync();

                                    AddLogs($"Обновлено устройство с ID {id}", "Успех");
                                    Console.WriteLine($"✅ Обновлено устройство с ID {id}");
                                    await Program.GetAllDataAsync(writer);
                                }
                                else
                                {
                                    string errorMsg2 = $"Девайс с ID {id} не найден";
                                    AddLogs(errorMsg2, "Ошибка");
                                    Console.WriteLine($"❌ {errorMsg2}");
                                }
                            }
                            catch (Exception ex)
                            {
                                string errorMsg3 = $"Ошибка при обновлении Девайса: {ex.Message}";
                                AddLogs(errorMsg3, "Ошибка");
                                Console.WriteLine($"❌ {errorMsg3}");
                            }
                        }
                        else
                        {
                            string errorMsg4 = "Недостаточно данных для обновления Девайса";
                            AddLogs(errorMsg4, "Ошибка");
                            Console.WriteLine($"❌ {errorMsg4}");
                        }
                        break;

                    case "Registers":
                        if (Item is List<object> objectList2 && objectList2.Count >= 5)
                        {
                            try
                            {
                                int id = objectList2[0] switch
                                {
                                    JsonElement { ValueKind: JsonValueKind.Number } jsonElement => jsonElement.GetInt32(),
                                    JsonElement { ValueKind: JsonValueKind.String } jsonElement => int.TryParse(jsonElement.GetString(), out int result) ? result : 0,
                                    int i => i,
                                    long l => (int)l,
                                    _ => 0
                                };

                                // НАХОДИМ существующую запись
                                var existingRegister = await _context.Registers
                                    .FirstOrDefaultAsync(r => r.Id == id);

                                if (existingRegister != null)
                                {
                                    // Проверяем существует ли устройство
                                    int deviceId = GetSafeInt(objectList2[1]);
                                    bool deviceExists = await _context.Devices.AnyAsync(d => d.Id == deviceId);

                                    if (!deviceExists)
                                    {
                                        string errorMsg1 = $"Устройство с ID {deviceId} не существует";
                                        AddLogs(errorMsg1, "Ошибка");
                                        Console.WriteLine($"❌ {errorMsg1}");
                                        return;
                                    }

                                    // Получаем новое значение регистра из запроса
                                    float newValue = GetSafeFloat(objectList2[4]);

                                    // Обновляем данные регистра
                                    existingRegister.DeviceId = deviceId;
                                    existingRegister.Name = GetSafeString(objectList2[2]);
                                    existingRegister.Description = GetSafeString(objectList2[3]);
                                    existingRegister.EditingDate = DateTime.UtcNow;

                                    await _context.SaveChangesAsync();

                                    // АВТОМАТИЧЕСКИ ДОБАВЛЯЕМ НОВОЕ ЗНАЧЕНИЕ РЕГИСТРА
                                    var registerValue = new RegisterValues
                                    {
                                        RegisterId = existingRegister.Id,
                                        Value = newValue,
                                        Timestamp = DateTime.UtcNow
                                    };

                                    await _context.RegisterValues.AddAsync(registerValue);
                                    await _context.SaveChangesAsync();

                                    string successMsg = $"Регистр обновлен и новое значение {newValue} добавлено";
                                    AddLogs(successMsg, "Успех");
                                    Console.WriteLine($"✅ {successMsg}");
                                    await Program.GetAllDataAsync(writer);
                                }
                                else
                                {
                                    string errorMsg2 = $"Регистр с ID {id} не найден";
                                    AddLogs(errorMsg2, "Ошибка");
                                    Console.WriteLine($"❌ {errorMsg2}");
                                }
                            }
                            catch (Exception ex)
                            {
                                string errorMsg3 = $"Ошибка при обновлении регистра: {ex.Message}";
                                AddLogs(errorMsg3, "Ошибка");
                                Console.WriteLine($"❌ {errorMsg3}");
                            }
                        }
                        else
                        {
                            string errorMsg4 = "Недостаточно данных для обновления регистра. Ожидается: [id, deviceId, name, description, value]";
                            AddLogs(errorMsg4, "Ошибка");
                            Console.WriteLine($"❌ {errorMsg4}");
                        }
                        break;

                    case "RegisterValues":
                        if (Item is List<object> objectList3 && objectList3.Count >= 3)
                        {
                            try
                            {
                                int id = objectList3[0] switch
                                {
                                    JsonElement { ValueKind: JsonValueKind.Number } jsonElement => jsonElement.GetInt32(),
                                    JsonElement { ValueKind: JsonValueKind.String } jsonElement => int.TryParse(jsonElement.GetString(), out int result) ? result : 0,
                                    int i => i,
                                    long l => (int)l,
                                    _ => 0
                                };

                                // НАХОДИМ существующую запись
                                var existingRegisterValue = await _context.RegisterValues
                                    .FirstOrDefaultAsync(rv => rv.Id == id);

                                if (existingRegisterValue != null)
                                {
                                    // Проверяем существует ли регистр
                                    int registerId = GetSafeInt(objectList3[1]);
                                    bool registerExists = await _context.Registers.AnyAsync(r => r.Id == registerId);

                                    if (!registerExists)
                                    {
                                        string errorMsg1 = $"Регистр с ID {registerId} не существует";
                                        AddLogs(errorMsg1, "Ошибка");
                                        Console.WriteLine($"❌ {errorMsg1}");
                                        return;
                                    }

                                    // Обновляем данные
                                    existingRegisterValue.RegisterId = registerId;
                                    existingRegisterValue.Value = GetSafeFloat(objectList3[2]);
                                    existingRegisterValue.Timestamp = objectList3.Count > 3 ? GetSafeDateTime(objectList3[3]) : DateTime.UtcNow;

                                    int changes = await _context.SaveChangesAsync();

                                    AddLogs($"Обновлено значение регистра с ID {id}", "Успех");
                                    Console.WriteLine($"✅ Обновлено значение регистра с ID {id}");
                                }
                                else
                                {
                                    string errorMsg2 = $"Значение регистра с ID {id} не найдено";
                                    AddLogs(errorMsg2, "Ошибка");
                                    Console.WriteLine($"❌ {errorMsg2}");
                                }
                            }
                            catch (Exception ex)
                            {
                                string errorMsg3 = $"Ошибка при обновлении значения регистра: {ex.Message}";
                                AddLogs(errorMsg3, "Ошибка");
                                Console.WriteLine($"❌ {errorMsg3}");
                            }
                        }
                        else
                        {
                            string errorMsg4 = "Недостаточно данных для обновления значения регистра";
                            AddLogs(errorMsg4, "Ошибка");
                            Console.WriteLine($"❌ {errorMsg4}");
                        }
                        break;

                    case "Logs":
                        if (Item is List<object> objectList4 && objectList4.Count >= 3)
                        {
                            try
                            {
                                int id = objectList4[0] switch
                                {
                                    JsonElement { ValueKind: JsonValueKind.Number } jsonElement => jsonElement.GetInt32(),
                                    JsonElement { ValueKind: JsonValueKind.String } jsonElement => int.TryParse(jsonElement.GetString(), out int result) ? result : 0,
                                    int i => i,
                                    long l => (int)l,
                                    _ => 0
                                };

                                // НАХОДИМ существующую запись
                                var existingLog = await _context.Logs
                                    .FirstOrDefaultAsync(l => l.Id == id);

                                if (existingLog != null)
                                {
                                    // Обновляем данные
                                    existingLog.Message = GetSafeString(objectList4[1]);
                                    existingLog.Type = GetSafeString(objectList4[2]);
                                    existingLog.Timestamp = objectList4.Count > 3 ? GetSafeDateTime(objectList4[3]) : DateTime.UtcNow;

                                    int changes = await _context.SaveChangesAsync();

                                    AddLogs($"Обновлен лог с ID {id}", "Успех");
                                    Console.WriteLine($"✅ Обновлен лог с ID {id}");
                                }
                                else
                                {
                                    string errorMsg1 = $"Лог с ID {id} не найден";
                                    AddLogs(errorMsg1, "Ошибка");
                                    Console.WriteLine($"❌ {errorMsg1}");
                                }
                            }
                            catch (Exception ex)
                            {
                                string errorMsg2 = $"Ошибка при обновлении лога: {ex.Message}";
                                AddLogs(errorMsg2, "Ошибка");
                                Console.WriteLine($"❌ {errorMsg2}");
                            }
                        }
                        else
                        {
                            string errorMsg3 = "Недостаточно данных для обновления лога";
                            AddLogs(errorMsg3, "Ошибка");
                            Console.WriteLine($"❌ {errorMsg3}");
                        }
                        break;

                    default:
                        string errorMsg = $"Неизвестный тип объекта: {typeObjects}";
                        AddLogs(errorMsg, "Ошибка");
                        Console.WriteLine($"❌ {errorMsg}");
                        await SendErrorResponseAsync(writer, errorMsg);
                        break;
                }
            }
            catch (Exception ex)
            {
                string errorMsg = $"Ошибка Обновления: {ex.Message}";
                AddLogs(errorMsg, "Ошибка");
                Console.WriteLine($"❌ {errorMsg}");
                await SendErrorResponseAsync(writer, ex.Message);
            }
        }

        public async Task AddLogs(string message, string type) // отдельно вывел метод добавления логов в бд для удобства 
        {
            if (message.Length > 0 && type.Length >0)
            {
                var logObj = new Logs
                {
                    Message = message,
                    Type = type,
                };

                await _context.Logs.AddAsync(logObj);
                await _context.SaveChangesAsync();
            }
        }
        // Удаление интерфейса
        public async Task DeleteItem(string typeObjects, List<object> Item, IResponseWriter writer)
        {
            try
            {
                switch (typeObjects)
                {
                    case "Interfaces":
                        if (Item is List<object> objectList)
                        {
                            try
                            {
                                int id = objectList[0] switch
                                {
                                    JsonElement { ValueKind: JsonValueKind.Number } jsonElement => jsonElement.GetInt32(),
                                    JsonElement { ValueKind: JsonValueKind.String } jsonElement => int.TryParse(jsonElement.GetString(), out int result) ? result : 0,
                                    int i => i,
                                    long l => (int)l,
                                    _ => 0
                                };

                                // НАХОДИМ существующую запись
                                var existingInterface = await _context.Interfaces
                                    .FirstOrDefaultAsync(i => i.Id == id);

                                if (existingInterface != null)
                                {
                                    _context.Interfaces.Remove(existingInterface);
                                    await _context.SaveChangesAsync();
                                    Console.WriteLine($" Интерфейс с ID {id} успешно удален");
                                    AddLogs($"Интерфейс с ID {id} успешно удален", "Успех");



                                    



                                    //удаляем все связанные девайсы и тд 
                                    var existingDevices = await _context.Devices
                                                        .Where(i => i.InterfaceId == id)
                                                        .ToListAsync();

                                    if (existingDevices.Any())
                                    {
                                        _context.Devices.RemoveRange(existingDevices);
                                        await _context.SaveChangesAsync();
                                        Console.WriteLine($" Удалено {existingDevices.Count} связанных устройств");
                                    }


                                    for(int a = 0; a< existingDevices.Count();a++)
                                    {
                                        var existingRegisters = await _context.Registers
                                                        .Where(i => i.DeviceId == existingDevices[a].Id)
                                                        .ToListAsync();

                                        if (existingRegisters.Any())
                                        {
                                            _context.Registers.RemoveRange(existingRegisters);
                                            await _context.SaveChangesAsync();
                                            Console.WriteLine($" Удалено {existingRegisters.Count} связанных регистров");

                                            for (int b = 0;b < existingRegisters.Count(); b++)
                                            {
                                                var existingRegistersValue = await _context.RegisterValues
                                                                .Where(i => i.RegisterId == existingRegisters[b].Id)
                                                                .ToListAsync();

                                                if (existingRegisters.Any())
                                                {
                                                    _context.Registers.RemoveRange(existingRegisters);
                                                    await _context.SaveChangesAsync();
                                                    Console.WriteLine($" Удалено {existingRegisters.Count} связанных значений регистров регистров");


                                                }
                                            }
                                        }
                                    }
                                    await Program.GetAllDataAsync(writer);
                                }
                                else
                                {
                                    AddLogs($"Интерфейс с ID {id} не найден", "Ошибка");
                                    Console.WriteLine($" Интерфейс с ID {id} не найден");
                                }
                            }
                            catch (Exception ex)
                            {
                                AddLogs($"Ошибка при удалении интерфейса: {ex.Message}", "Ошибка");
                                Console.WriteLine($" Ошибка при удалении интерфейса: {ex.Message}");
                            }
                        }
                        else
                        {
                            AddLogs($"Недостаточно данных для удаления интерфейса", "Ошибка");
                            Console.WriteLine(" Недостаточно данных для удаления интерфейса");
                        }
                        break;

                    case "Devices":
                        if (Item is List<object> objectList1)
                        {
                            try
                            {
                                int id = objectList1[0] switch
                                {
                                    JsonElement { ValueKind: JsonValueKind.Number } jsonElement => jsonElement.GetInt32(),
                                    JsonElement { ValueKind: JsonValueKind.String } jsonElement => int.TryParse(jsonElement.GetString(), out int result) ? result : 0,
                                    int i => i,
                                    long l => (int)l,
                                    _ => 0
                                };

                                // НАХОДИМ существующую запись
                                var existingDevices = await _context.Devices
                                    .FirstOrDefaultAsync(i => i.Id == id);

                                if (existingDevices != null)
                                {
                                    _context.Devices.Remove(existingDevices);
                                    await _context.SaveChangesAsync();
                                    AddLogs($"Устройство с ID {id} успешно удалено", "Успех");
                                    Console.WriteLine($"Устройство с ID {id} успешно удалено");

                                    //удаляем все связанные девайсы и тд 
                                    var existingRegisters = await _context.Registers
                                                        .Where(i => i.DeviceId == id)
                                                        .ToListAsync();

                                    if (existingRegisters.Any())
                                    {
                                        _context.Registers.RemoveRange(existingRegisters);
                                        await _context.SaveChangesAsync();
                                        Console.WriteLine($" Удалено {existingRegisters.Count} связанных регистров");
                                    }


                                    for (int a = 0; a < existingRegisters.Count(); a++)
                                    {
                                        var existingRegistersValue = await _context.RegisterValues
                                                        .Where(i => i.RegisterId == existingRegisters[a].Id)
                                                        .ToListAsync();

                                        if (existingRegistersValue.Any())
                                        {
                                            _context.RegisterValues.RemoveRange(existingRegistersValue);
                                            await _context.SaveChangesAsync();
                                            Console.WriteLine($" Удалено {existingRegisters.Count} связанных значений регистров");

                                            
                                        }
                                    }

                                    await Program.GetAllDataAsync(writer);
                                }
                                else
                                {
                                    AddLogs($"Устройство с ID {id} не найдено", "Ошибка");
                                    Console.WriteLine($"Устройство с ID {id} не найдено");
                                }
                            }
                            catch (Exception ex)
                            {
                                AddLogs($"Ошибка при удалении устройства: {ex.Message}", "Ошибка");
                                Console.WriteLine($"Ошибка при удалении устройства: {ex.Message}");
                            }
                        }
                        else
                        {
                            AddLogs($"Недостаточно данных для удаления устройства", "Ошибка");
                            Console.WriteLine("Недостаточно данных для удаления устройства");
                        }
                        break;

                    case "Registers":
                        if (Item is List<object> objectList2)
                        {
                            try
                            {
                                int id = objectList2[0] switch
                                {
                                    JsonElement { ValueKind: JsonValueKind.Number } jsonElement => jsonElement.GetInt32(),
                                    JsonElement { ValueKind: JsonValueKind.String } jsonElement => int.TryParse(jsonElement.GetString(), out int result) ? result : 0,
                                    int i => i,
                                    long l => (int)l,
                                    _ => 0
                                };

                                // НАХОДИМ существующую запись
                                var existingRegister = await _context.Registers
                                    .FirstOrDefaultAsync(r => r.Id == id);

                                if (existingRegister != null)
                                {
                                    _context.Registers.Remove(existingRegister);
                                    await _context.SaveChangesAsync();
                                    AddLogs($"Регистр с ID {id} успешно удален", "Успех");
                                    Console.WriteLine($"Регистр с ID {id} успешно удален");

                                    //удаляем все связанные девайсы и тд 
                                    var existingRegisterValues = await _context.RegisterValues
                                                        .Where(i => i.RegisterId == id)
                                                        .ToListAsync();

                                    if (existingRegisterValues.Any())
                                    {
                                        _context.RegisterValues.RemoveRange(existingRegisterValues);
                                        await _context.SaveChangesAsync();
                                        Console.WriteLine($" Удалено {existingRegisterValues.Count} связанных значений регистров");
                                    }


                                    

                                    await Program.GetAllDataAsync(writer);
                                }
                                else
                                {
                                    AddLogs($"Регистр с ID {id} не найден", "Ошибка");
                                    Console.WriteLine($"Регистр с ID {id} не найден");
                                }
                            }
                            catch (Exception ex)
                            {
                                AddLogs($"Ошибка при удалении регистра: {ex.Message}", "Ошибка");
                                Console.WriteLine($"Ошибка при удалении регистра: {ex.Message}");
                            }
                        }
                        else
                        {
                            AddLogs($"Недостаточно данных для удаления регистра", "Ошибка");
                            Console.WriteLine("❌ Недостаточно данных для удаления регистра");
                        }
                        break;

                    case "RegisterValues":
                        if (Item is List<object> objectList3)
                        {
                            try
                            {
                                int id = objectList3[0] switch
                                {
                                    JsonElement { ValueKind: JsonValueKind.Number } jsonElement => jsonElement.GetInt32(),
                                    JsonElement { ValueKind: JsonValueKind.String } jsonElement => int.TryParse(jsonElement.GetString(), out int result) ? result : 0,
                                    int i => i,
                                    long l => (int)l,
                                    _ => 0
                                };

                                // НАХОДИМ существующую запись
                                var existingRegisterValue = await _context.RegisterValues
                                    .FirstOrDefaultAsync(rv => rv.Id == id);

                                if (existingRegisterValue != null)
                                {
                                    _context.RegisterValues.Remove(existingRegisterValue);
                                    await _context.SaveChangesAsync();
                                    AddLogs($"Значение регистра с ID {id} успешно удалено", "Успех");
                                    Console.WriteLine($"Значение регистра с ID {id} успешно удалено");
                                }
                                else
                                {
                                    AddLogs($"Значение регистра с ID {id} не найдено", "Ошибка");
                                    Console.WriteLine($"Значение регистра с ID {id} не найдено");
                                }
                            }
                            catch (Exception ex)
                            {
                                AddLogs($"Ошибка при удалении значения регистра: {ex.Message}", "Ошибка");
                                Console.WriteLine($"Ошибка при удалении значения регистра: {ex.Message}");
                            }
                        }
                        else
                        {
                            AddLogs($"Недостаточно данных для удаления значения регистра", "Ошибка");
                            Console.WriteLine("Недостаточно данных для удаления значения регистра");
                        }
                        break;

                    case "Logs":
                        if (Item is List<object> objectList4)
                        {
                            try
                            {
                                int id = objectList4[0] switch
                                {
                                    JsonElement { ValueKind: JsonValueKind.Number } jsonElement => jsonElement.GetInt32(),
                                    JsonElement { ValueKind: JsonValueKind.String } jsonElement => int.TryParse(jsonElement.GetString(), out int result) ? result : 0,
                                    int i => i,
                                    long l => (int)l,
                                    _ => 0
                                };

                                // НАХОДИМ существующую запись
                                var existingLog = await _context.Logs
                                    .FirstOrDefaultAsync(l => l.Id == id);

                                if (existingLog != null)
                                {
                                    _context.Logs.Remove(existingLog);
                                    await _context.SaveChangesAsync();
                                    
                                }
                                else
                                {
                                    
                                }
                            }
                            catch (Exception ex)
                            {
                                
                            }
                        }
                        else
                        {
                            
                        }
                        break;

                    default:
                        
                        await SendErrorResponseAsync(writer, $"Неизвестный тип объекта: {typeObjects}");
                        break;
                }
            }
            catch (Exception ex)
            {
                AddLogs($"Ошибка удаления: {ex.Message}", "Ошибка");
                Console.WriteLine($"Ошибка удаления: {ex.Message}");
                await SendErrorResponseAsync(writer, ex.Message);
            }
        }

        

        public void Dispose()
        {
            _context?.Dispose();
        }
    }
}

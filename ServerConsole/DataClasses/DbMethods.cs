using Microsoft.EntityFrameworkCore;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Text.Json;
using System.Threading.Tasks;

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
                Console.WriteLine("✅ База данных и таблицы созданы");

                // Проверяем создание таблиц
                CheckTablesExist();
            }
            catch (Exception ex)
            {
                Console.WriteLine($"❌ Ошибка создания базы: {ex.Message}");
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
                Console.WriteLine($"❌ Ошибка: {ex.Message}, пересоздаем БД...");
                _context.Database.EnsureDeleted();
                _context.Database.EnsureCreated();
                AddTestData();
            }
        }

        private void AddTestData()//это было написано через иишку , потому что было лень придумывать тестовые данные
        {
            // Интерфейсы
            var interfaces = new List<Interfaces>
    {
        new Interfaces("Интерфейс 1", "Описание 1"),
        new Interfaces("Интерфейс 2", "Описание 2"),
        new Interfaces("Интерфейс 3", "Описание 3")
    };
            _context.Interfaces.AddRange(interfaces);
            _context.SaveChanges();

            // Устройства
            var devices = new List<Devices>
    {
        new Devices(1, "Устройство 1", "Описание устройства 1", true, "Круг ●", 50, 10, 10, "#FF0000"),
        new Devices(2, "Устройство 2", "Описание устройства 2", true, "Квадрат ■", 60, 50, 50, "#0000FF"),
        new Devices(3, "Устройство 3", "Описание устройства 3", false, "Треугольник ▲", 40, 100, 100, "#008000")
    };
            _context.Devices.AddRange(devices);
            _context.SaveChanges();

            // Регистры
            var registers = new List<Registers>
    {
        new Registers(1, "Регистр 1", "Описание регистра 1"),
        new Registers(2, "Регистр 2", "Описание регистра 2"),
        new Registers(3, "Регистр 3", "Описание регистра 3")
    };
            _context.Registers.AddRange(registers);
            _context.SaveChanges();

            // Значения регистров
            var random = new Random();
            var registerValues = new List<RegisterValues>();
            var now = DateTime.UtcNow;

            for (int registerId = 1; registerId <= 3; registerId++)
            {
                for (int i = 0; i < 3; i++)
                {
                    registerValues.Add(new RegisterValues
                    {
                        RegisterId = registerId,
                        Value = (float)(random.NextDouble() * 100),
                        Timestamp = now.AddHours(-i)
                    });
                }
            }
            _context.RegisterValues.AddRange(registerValues);
            _context.SaveChanges();

            Console.WriteLine(" Тестовые данные добавлены во все таблицы");
        }

        // Добавление интерфейса
        public async Task AddData(string typeObjects, List<object> Item, StreamWriter writer)
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
                                Console.WriteLine($"❌ Интерфейс с ID {interfaceId} не существует");
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
                                Console.WriteLine($"❌ Устройство с ID {deviceId} не существует");
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

                            Console.WriteLine($"✅ Регистр создан и начальное значение {initialValue} добавлено");
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
                        }
                        break;
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine($" Ошибка добавления: {ex.Message}");
                await SendErrorResponseAsync(writer, ex.Message);
            }

        }

        private async Task AddSingleRegisterValue(List<object> objectList3)
        {
            int registerId = GetSafeInt(objectList3[0]);

            bool registerExists = await _context.Registers.AnyAsync(r => r.Id == registerId);
            if (!registerExists)
            {
                Console.WriteLine($" Регистр с ID {registerId} не существует");
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
            Console.WriteLine($" Добавлено значение {registerValueObj.Value} для регистра {registerId}");
        }
        private async Task AddMultipleRegisterValues(List<object> batchData)
        {
            try
            {
                var registerValuesToAdd = new List<RegisterValues>();

                foreach (var item in batchData)
                {
                    if (item is List<object> valueData && valueData.Count >= 2)
                    {
                        int registerId = GetSafeInt(valueData[0]);
                        float value = GetSafeFloat(valueData[1]);

                        // Проверяем существование регистра
                        bool registerExists = await _context.Registers.AnyAsync(r => r.Id == registerId);
                        if (!registerExists)
                        {
                            Console.WriteLine($" Регистр с ID {registerId} не существует, пропускаем");
                            continue;
                        }

                        var registerValueObj = new RegisterValues
                        {
                            RegisterId = registerId,
                            Value = value,
                            Timestamp = valueData.Count > 2 ? GetSafeDateTime(valueData[2]) : DateTime.UtcNow
                        };

                        registerValuesToAdd.Add(registerValueObj);
                    }
                }

                if (registerValuesToAdd.Count > 0)
                {
                    await _context.RegisterValues.AddRangeAsync(registerValuesToAdd);
                    await _context.SaveChangesAsync();
                    Console.WriteLine($" Добавлено {registerValuesToAdd.Count} значений регистров");


                }
            }
            catch (Exception ex)
            {
                Console.WriteLine($" Ошибка добавления пачки значений: {ex.Message}");
            }
        }

        private static async Task SendErrorResponseAsync(StreamWriter writer, string error)
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
            return await _context.Interfaces
                        .OrderBy(i => i.Id)
                        .AsNoTracking() 
                        .ToListAsync();

                

            
            
        }

        public async Task<List<Devices>> GetAllDataDevicesFromDb()
        {
            return await _context.Devices
                        .OrderBy(d => d.Id)
                        .AsNoTracking()
                        .ToListAsync();
            

        }

        public async Task<List<Registers>> GetAllDataRegistersFromDb()
        {
            return await _context.Registers
                        .OrderBy(r => r.Id)
                        .AsNoTracking()
                        .ToListAsync();
        }

        public async Task<List<RegisterValues>> GetAllDataRegisterValuesFromDb()
        {
            return await _context.RegisterValues
                        .OrderBy(rv => rv.Id)
                        .AsNoTracking()
                        .ToListAsync();
        }

        public async Task<List<Logs>> GetAllDataLogsFromDb()
        {
            return await _context.Logs
                        .OrderBy(l => l.Id)
                        .AsNoTracking()
                        .ToListAsync();
        }
        #endregion


        public async Task GetItemById(string typeObjects, List<object> Item, StreamWriter writer)
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
                        }
                        break;

                    case "Devices":
                        if (int.TryParse(Item[0].ToString(), out int devicesId))
                        {
                            _item = await _context.Devices
                                .AsNoTracking()
                                .FirstOrDefaultAsync(i => i.Id == devicesId);
                            response = new ResponseGetOneItem("Devices", _item);
                        }
                        break;

                    case "Registers":
                        if (int.TryParse(Item[0].ToString(), out int registersId))
                        {
                            _item = await _context.Registers
                                .AsNoTracking()
                                .FirstOrDefaultAsync(i => i.Id == registersId); 
                            var registerValuesList = await _context.RegisterValues
                                .OrderBy(d => d.RegisterId)
                                .AsNoTracking()
                                .ToListAsync();

                            
                            _objectListRegValue = registerValuesList.Cast<object>().ToList();
                            response = new ResponseGetOneItem("Registers", _item, _objectListRegValue);
                        }
                        break;

                    case "RegisterValues":
                        if (int.TryParse(Item[0].ToString(), out int registerValuesId))
                        {
                            _item = await _context.RegisterValues
                                .AsNoTracking()
                                .FirstOrDefaultAsync(i => i.Id == registerValuesId);

                            

                            response = new ResponseGetOneItem("RegisterValues", _item, _objectListRegValue);
                        }
                        break;

                    case "Logs":
                        if (int.TryParse(Item[0].ToString(), out int logsId))
                        {
                            _item = await _context.Logs
                                .AsNoTracking()
                                .FirstOrDefaultAsync(i => i.Id == logsId);

                            
                            response = new ResponseGetOneItem("Logs", _item);
                        }
                        break;

                    default:
                        Console.WriteLine($"❌ Неизвестный тип объекта: {typeObjects}");
                        var errorResponse = new { Status = "error", Message = $"Неизвестный тип объекта: {typeObjects}" };
                        byte[] errorData = JsonSerializer.SerializeToUtf8Bytes(errorResponse);
                        await writer.BaseStream.WriteAsync(errorData, 0, errorData.Length);
                        await writer.FlushAsync();
                        return;
                }

                // Проверяем, найден ли объект
                if (_item == null)
                {
                    var notFoundResponse = new { Status = "not_found", Message = $"Объект с ID {Item[0]} не найден" };
                    byte[] notFoundData = JsonSerializer.SerializeToUtf8Bytes(notFoundResponse);
                    await writer.BaseStream.WriteAsync(notFoundData, 0, notFoundData.Length);
                    await writer.FlushAsync();
                    return;
                }

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
                Console.WriteLine($"❌ Ошибка в GetOneItem: {ex.Message}");

                // Отправляем ошибку клиенту
                try
                {
                    var errorResponse = new { Status = "error", Message = ex.Message };
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
        public async Task UpdateData(string typeObjects, List<object> Item, StreamWriter writer)
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
                                }
                                else
                                {
                                    Console.WriteLine($"❌ Интерфейс с ID {id} не найден");
                                }
                            }
                            catch (Exception ex)
                            {
                                Console.WriteLine($"❌ Ошибка при обновлении интерфейса: {ex.Message}");
                            }
                        }
                        else
                        {
                            Console.WriteLine("❌ Недостаточно данных для обновления интерфейса");
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
                                        Console.WriteLine($"❌ Интерфейс с ID {interfaceId} не существует");
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
                                }
                                else
                                {
                                    Console.WriteLine($"❌ Девайс с ID {id} не найден");
                                }
                            }
                            catch (Exception ex)
                            {
                                Console.WriteLine($"❌ Ошибка при обновлении Девайса: {ex.Message}");
                            }
                        }
                        else
                        {
                            Console.WriteLine("❌ Недостаточно данных для обновления Девайса");
                        }
                        break;

                    case "Registers":
                        if (Item is List<object> objectList2 && objectList2.Count >= 5) // теперь нужно минимум 5 элементов
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
                                        Console.WriteLine($"❌ Устройство с ID {deviceId} не существует");
                                        return;
                                    }

                                    // Получаем новое значение регистра из запроса
                                    float newValue = GetSafeFloat(objectList2[4]); // 5-й элемент - новое значение

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
                                        Value = newValue, // передаем реальное значение из запроса
                                        Timestamp = DateTime.UtcNow
                                    };

                                    await _context.RegisterValues.AddAsync(registerValue);
                                    await _context.SaveChangesAsync();

                                    Console.WriteLine($"✅ Регистр обновлен и новое значение {newValue} добавлено");
                                }
                                else
                                {
                                    Console.WriteLine($"❌ Регистр с ID {id} не найден");
                                }
                            }
                            catch (Exception ex)
                            {
                                Console.WriteLine($"❌ Ошибка при обновлении регистра: {ex.Message}");
                            }
                        }
                        else
                        {
                            Console.WriteLine("❌ Недостаточно данных для обновления регистра. Ожидается: [id, deviceId, name, description, value]");
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
                                        Console.WriteLine($"❌ Регистр с ID {registerId} не существует");
                                        return;
                                    }

                                    // Обновляем данные
                                    existingRegisterValue.RegisterId = registerId;
                                    existingRegisterValue.Value = GetSafeFloat(objectList3[2]);
                                    existingRegisterValue.Timestamp = objectList3.Count > 3 ? GetSafeDateTime(objectList3[3]) : DateTime.UtcNow;

                                    int changes = await _context.SaveChangesAsync();
                                }
                                else
                                {
                                    Console.WriteLine($"❌ Значение регистра с ID {id} не найдено");
                                }
                            }
                            catch (Exception ex)
                            {
                                Console.WriteLine($"❌ Ошибка при обновлении значения регистра: {ex.Message}");
                            }
                        }
                        else
                        {
                            Console.WriteLine("❌ Недостаточно данных для обновления значения регистра");
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
                                }
                                else
                                {
                                    Console.WriteLine($"❌ Лог с ID {id} не найден");
                                }
                            }
                            catch (Exception ex)
                            {
                                Console.WriteLine($"❌ Ошибка при обновлении лога: {ex.Message}");
                            }
                        }
                        else
                        {
                            Console.WriteLine("❌ Недостаточно данных для обновления лога");
                        }
                        break;

                    default:
                        Console.WriteLine($"❌ Неизвестный тип объекта: {typeObjects}");
                        await SendErrorResponseAsync(writer, $"Неизвестный тип объекта: {typeObjects}");
                        break;
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine($"❌ Ошибка Обновления: {ex.Message}");
                await SendErrorResponseAsync(writer, ex.Message);
            }
        }

        // Удаление интерфейса
        public async Task DeleteItem(string typeObjects, List<object> Item, StreamWriter writer)
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
                                    Console.WriteLine($"✅ Интерфейс с ID {id} успешно удален");
                                }
                                else
                                {
                                    Console.WriteLine($"❌ Интерфейс с ID {id} не найден");
                                }
                            }
                            catch (Exception ex)
                            {
                                Console.WriteLine($"❌ Ошибка при удалении интерфейса: {ex.Message}");
                            }
                        }
                        else
                        {
                            Console.WriteLine("❌ Недостаточно данных для удаления интерфейса");
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
                                    Console.WriteLine($"✅ Устройство с ID {id} успешно удалено");
                                }
                                else
                                {
                                    Console.WriteLine($"❌ Устройство с ID {id} не найдено");
                                }
                            }
                            catch (Exception ex)
                            {
                                Console.WriteLine($"❌ Ошибка при удалении устройства: {ex.Message}");
                            }
                        }
                        else
                        {
                            Console.WriteLine("❌ Недостаточно данных для удаления устройства");
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
                                    Console.WriteLine($"✅ Регистр с ID {id} успешно удален");
                                }
                                else
                                {
                                    Console.WriteLine($"❌ Регистр с ID {id} не найден");
                                }
                            }
                            catch (Exception ex)
                            {
                                Console.WriteLine($"❌ Ошибка при удалении регистра: {ex.Message}");
                            }
                        }
                        else
                        {
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
                                    Console.WriteLine($"✅ Значение регистра с ID {id} успешно удалено");
                                }
                                else
                                {
                                    Console.WriteLine($"❌ Значение регистра с ID {id} не найдено");
                                }
                            }
                            catch (Exception ex)
                            {
                                Console.WriteLine($"❌ Ошибка при удалении значения регистра: {ex.Message}");
                            }
                        }
                        else
                        {
                            Console.WriteLine("❌ Недостаточно данных для удаления значения регистра");
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
                                    Console.WriteLine($"✅ Лог с ID {id} успешно удален");
                                }
                                else
                                {
                                    Console.WriteLine($"❌ Лог с ID {id} не найден");
                                }
                            }
                            catch (Exception ex)
                            {
                                Console.WriteLine($"❌ Ошибка при удалении лога: {ex.Message}");
                            }
                        }
                        else
                        {
                            Console.WriteLine("❌ Недостаточно данных для удаления лога");
                        }
                        break;

                    default:
                        Console.WriteLine($"❌ Неизвестный тип объекта: {typeObjects}");
                        await SendErrorResponseAsync(writer, $"Неизвестный тип объекта: {typeObjects}");
                        break;
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine($"❌ Ошибка удаления: {ex.Message}");
                await SendErrorResponseAsync(writer, ex.Message);
            }
        }

        

        public void Dispose()
        {
            _context?.Dispose();
        }
    }
}

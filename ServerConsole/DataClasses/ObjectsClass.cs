using System;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using System.Collections.Generic;

using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace ServerConsole.DataClasses
{
    public class Devices
    {
        [Key]
        [DatabaseGenerated(DatabaseGeneratedOption.Identity)]
        public int Id { get; set; }

        [Required]
        public int InterfaceId { get; set; }

        [Required]
        [MaxLength(100)]
        public string Name { get; set; } = string.Empty;

        [MaxLength(500)]
        public string Description { get; set; } = string.Empty;

        [Required]
        public bool IsEnabled { get; set; } = true;

        [Required]
        public DateTime EditingDate { get; set; } = DateTime.UtcNow;

        [Required]
        [MaxLength(50)]
        public string FigureType { get; set; } = "Circle";

        [Required]
        public int Size { get; set; } = 50;

        [Required]
        public int PosX { get; set; } = 0;

        [Required]
        public int PosY { get; set; } = 0;

        [Required]
        [MaxLength(50)]
        public string Color { get; set; } = "#000000";

        [ForeignKey("InterfaceId")]
        public virtual Interfaces Interface { get; set; }

        // ДОБАВЬТЕ ЭТО СВОЙСТВО
        public virtual ICollection<Registers> Registers { get; set; } = new List<Registers>();

        public Devices() { }

        public Devices(int interfaceId, string name, string description, bool isEnabled,
                       string figureType, int size, int posX, int posY, string color)
        {
            InterfaceId = interfaceId;
            Name = name;
            Description = description;
            IsEnabled = isEnabled;
            FigureType = figureType;
            Size = size;
            PosX = posX;
            PosY = posY;
            Color = color;
            EditingDate = DateTime.UtcNow;
        }
    }
    public class Interfaces
    {
        [Key]
        [DatabaseGenerated(DatabaseGeneratedOption.Identity)]
        public int Id { get; set; } //уникальный идентификатор

        [Required]
        [MaxLength(100)]
        public string Name { get; set; } = string.Empty;//название интерфейса

        [MaxLength(500)]
        public string Description { get; set; } = string.Empty;//описание интерфейса

        [Required]
        public DateTime EditingDate { get; set; } = DateTime.UtcNow;//последняя дата изменения/создания

        // ДОБАВЬТЕ ЭТО СВОЙСТВО - коллекция устройств
        public virtual ICollection<Devices> Devices { get; set; } = new List<Devices>();


        // Конструкторы
        public Interfaces() { }

        public Interfaces(string name, string description)
        {
            Name = name;
            Description = description;
            EditingDate = DateTime.UtcNow;
        }

        public Interfaces(int id, string name, string description, DateTime editingDate)
        {
            Id = id;
            Name = name;
            Description = description;
            EditingDate = editingDate;
        }

    }

    public class Registers
    {
        [Key]
        [DatabaseGenerated(DatabaseGeneratedOption.Identity)]
        public int Id { get; set; }

        [Required]
        public int DeviceId { get; set; }

        [Required]
        [MaxLength(100)]
        public string Name { get; set; } = string.Empty;

        [MaxLength(500)]
        public string Description { get; set; } = string.Empty;

        [Required]
        public DateTime EditingDate { get; set; } = DateTime.UtcNow;

        [ForeignKey("DeviceId")]
        public virtual Devices Device { get; set; }

        // ДОБАВЬТЕ ЭТО СВОЙСТВО
        public virtual ICollection<RegisterValues> RegisterValues { get; set; } = new List<RegisterValues>();

        public Registers() { }

        public Registers(int deviceId, string name, string description)
        {
            DeviceId = deviceId;
            Name = name;
            Description = description;
            EditingDate = DateTime.UtcNow;
        }
    }


    public class RegisterValues
    {
        [Key]
        [DatabaseGenerated(DatabaseGeneratedOption.Identity)]
        public int Id { get; set; } // уникальный идентификатор

        [Required]
        public int RegisterId { get; set; } // ссылка на регистр

        [Required]
        public float Value { get; set; } // значение регистра

        [Required]
        public DateTime Timestamp { get; set; } = DateTime.UtcNow; // время записи значения

        [ForeignKey("RegisterId")]
        public virtual Registers Register { get; set; }

        // Конструкторы
        public RegisterValues() { }

        public RegisterValues(int registerId, float value)
        {
            RegisterId = registerId;
            Value = value;
            Timestamp = DateTime.UtcNow;
        }

        public RegisterValues(int registerId, float value, DateTime timestamp)
        {
            RegisterId = registerId;
            Value = value;
            Timestamp = timestamp;
        }
    }

    public class Logs
    {
        [Key]
        [DatabaseGenerated(DatabaseGeneratedOption.Identity)]
        public int Id { get; set; } // уникальный идентификатор

        [Required]
        public DateTime Timestamp { get; set; } = DateTime.UtcNow; // время записи сообщения

        [Required]
        [MaxLength(1000)]
        public string Message { get; set; } = string.Empty; // сообщение

        [Required]
        [MaxLength(50)]
        public string Type { get; set; } = "Info"; // тип сообщения

        // Конструкторы
        public Logs() { }

        public Logs(string message, string type)
        {
            Message = message;
            Type = type;
            Timestamp = DateTime.UtcNow;
        }

        public Logs(string message, string type, DateTime timestamp)
        {
            Message = message;
            Type = type;
            Timestamp = timestamp;
        }
    }

    class RequestsClass
    {
        private string _nameRequests = string.Empty; //название запроса
        private string _typeObjects = string.Empty; //название объекта
        private List<object> _objectsList = new List<object>(); //параметры объекста
        private List<object> _objectList1 = new List<object>(); //объекты
        public string NameRequests
        {
            get { return _nameRequests; }
            set { _nameRequests = value; }
        }

        public string TypeObjects
        {
            get { return _typeObjects; }
            set { _typeObjects = value; }
        }

        public List<object> ObjectsList
        {
            get { return _objectsList; }
            set { _objectsList = value; }
        }
        public List<object> ObjectList1
        {
            get { return _objectList1; }
            set { _objectList1 = value; }
        }
        public RequestsClass()
        {

        }
        public RequestsClass(string nameRequests, string typeObjects, List<object> objectsList1)
        {
            this.NameRequests = nameRequests;
            this.TypeObjects = typeObjects;
            this.ObjectList1 = objectsList1;
        }


        public override string ToString()
        {
            if(this.ObjectsList.Count == 0)
            {
                return $"Название запроса: {this.NameRequests}, Тип объекта: {this.TypeObjects}, Кол-во объектов: {this.ObjectList1.Count}";
            }
            else
            {
                string _st = "";
                for (int i = 0; i < ObjectsList.Count(); i++)
                {
                    if(_st.Length==0)
                    {
                        _st += ObjectsList[i] ;
                    }
                    else
                    {
                        _st += ", "+ObjectsList[i] ;
                    }
                    
                }
                return $"Название запроса: {this.NameRequests}, Тип объекта: {this.TypeObjects}, Кол-во параметров: {this.ObjectsList.Count}, Параметры: {_st}";
            }
            
        }
        
    }

    class ResponseGetOneItem
    {
        private string _typeObjects = String.Empty;
        private object _item = new object();
        private List<object> _objectsListRegistrsValue = new List<object>();//кесли возвращаем значения регистра
        public string TypeObjects
        {
            get { return _typeObjects; }
            set { _typeObjects = value; }
        }

        public object Item
        {
            get { return _item; }
            set { _item = value; }
        }
        public List<object> ObjectListRegistrsValue
        {
            get { return _objectsListRegistrsValue; }
            set { _objectsListRegistrsValue = value; }
        }
        public ResponseGetOneItem(string typeObjects, object item)//дефолтный конструктор , который используется в 90% случаев
        {
            this.TypeObjects = typeObjects;
            this.Item = item;
        }

        public ResponseGetOneItem(string typeObjects, object item, List<object> objectListRegistersValue)//конструктор для возврата значений регистра;
        {
            this.TypeObjects = typeObjects;
            this.Item = item;
            this.ObjectListRegistrsValue = objectListRegistersValue;
        }
    }

    
    class ResponseGetAllData
    {
        private List<Interfaces> _objectListInterfaces = new List<Interfaces>();
        private List<Devices> _objectListDevices = new List<Devices>();
        private List<Registers> _objectListRegisters = new List<Registers>();
        private List<RegisterValues> _objectListRegisterValues = new List<RegisterValues>();
        private List<Logs> _objectListLogs = new List<Logs>();

        public List<Interfaces> ObjectsListInterfaces
        {
            get { return _objectListInterfaces; }
            set { _objectListInterfaces = value; }
        }

        public List<Devices> ObjectsListDevices
        {
            get { return _objectListDevices; }
            set { _objectListDevices = value; }
        }

        public List<Registers> ObjectsListRegisters
        {
            get { return _objectListRegisters; }
            set { _objectListRegisters = value; }
        }

        public List<RegisterValues> ObjectsListRegisterValues
        {
            get { return _objectListRegisterValues; }
            set { _objectListRegisterValues = value; }
        }

        public List<Logs> ObjectsListLogs
        {
            get { return _objectListLogs; }
            set { _objectListLogs = value; }
        }

        // Конструктор по умолчанию
        public ResponseGetAllData()
        {
        }

        // Существующий конструктор с параметрами
        public ResponseGetAllData(List<Interfaces> objectsListInterfaces, List<Devices> objectListDevices)
        {
            this.ObjectsListInterfaces = objectsListInterfaces;
            this.ObjectsListDevices = objectListDevices;
        }

        // Новый конструктор для всех типов
        public ResponseGetAllData(
            List<Interfaces> objectsListInterfaces,
            List<Devices> objectListDevices,
            List<Registers> objectListRegisters,
            List<RegisterValues> objectListRegisterValues,
            List<Logs> objectListLogs)
        {
            this.ObjectsListInterfaces = objectsListInterfaces;
            this.ObjectsListDevices = objectListDevices;
            this.ObjectsListRegisters = objectListRegisters;
            this.ObjectsListRegisterValues = objectListRegisterValues;
            this.ObjectsListLogs = objectListLogs;
        }
    }
}

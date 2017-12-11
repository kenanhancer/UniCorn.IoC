using System;
using System.Collections.Generic;
using System.Linq;
using System.ServiceModel;
using System.Threading.Tasks;

namespace UniCorn.IoC.UnitTests
{
    public interface ILoginService
    {
        ILoginValidator LoginValidator { get; set; }
        string LoginType { get; set; }
        string Login(string userKey, string password);
        void Logout(string sessionKey);
    }

    public interface ILoginValidator
    {
        bool ValidateLogin(string userName, string password);
    }

    public class EmailLoginValidator : ILoginValidator
    {
        public bool ValidateLogin(string userName, string password)
        {
            return true;
        }
    }

    public class PhoneLoginValidator : ILoginValidator
    {
        public bool ValidateLogin(string userName, string password)
        {
            return true;
        }
    }

    public class EmailLoginService : ILoginService
    {
        private string loginType;
        public string LoginType
        {
            get
            {
                return "Email";
            }

            set
            {
                loginType = value;
            }
        }

        public ILoginValidator LoginValidator { get; set; }

        public EmailLoginService(ILoginValidator loginValidator)
        {
            LoginValidator = loginValidator;
        }

        public string Login(string userKey, string password)
        {
            Console.WriteLine("Email login " + userKey);

            return Guid.NewGuid().ToString("N");
        }

        public void Logout(string sessionKey)
        {
            Console.WriteLine(sessionKey + " Logout.");
        }
    }

    public class PhoneLoginService : ILoginService
    {
        private string loginType;
        public string LoginType
        {
            get
            {
                return "Phone";
            }

            set
            {
                loginType = value;
            }
        }

        public ILoginValidator LoginValidator { get; set; }

        public PhoneLoginService(ILoginValidator loginValidator)
        {
            LoginValidator = loginValidator;
        }

        public string Login(string userKey, string password)
        {
            Console.WriteLine("Phone login " + userKey);

            return Guid.NewGuid().ToString("N");
        }

        public void Logout(string sessionKey)
        {
            Console.WriteLine(sessionKey + " Logout.");
        }
    }




    public interface IShape
    {
    }

    public class Circle : IShape
    {
    }

    public class Square : IShape
    {
    }

    interface IService { }
    class TestService : IService { }

    interface IClient { IService Service { get; } }
    class TestClient : IClient
    {
        public IService Service { get; private set; }
        public TestClient(IService service) { Service = service; }
    }




    public class NewInterceptor : IInterceptor
    {
        public void Intercept(IInvocation invocation)
        {
            invocation.Proceed();
        }
    }


    [ServiceContract]
    public interface ILoginServiceProxy : ILoginService
    {
        [OperationContract]
        string Login(string userKey, string password);
        [OperationContract]
        void Logout(string sessionKey);
    }

    public class LoginServiceProxy : IProxyType, ILoginServiceProxy
    {
        public static ProxyMethod get_LoginValidator_Proxy;

        public static ProxyMethod set_LoginValidator_Proxy;

        public static ProxyMethod get_LoginType_Proxy;

        public static ProxyMethod set_LoginType_Proxy;

        public static ProxyMethod Login_Proxy;

        public static ProxyMethod Logout_Proxy;

        IInterceptor _interceptor;
        ILoginService _service;

        static LoginServiceProxy()
        {
            Dictionary<string, ProxyMethod> dictionary = ProxyTypeUtility.CreateMethodInvokers(typeof(ILoginService));

            string key;
            foreach (var item in dictionary)
            {
                key = item.Key;

                if (key.StartsWith("get_LoginValidator_"))
                    get_LoginValidator_Proxy = item.Value;
                else if (key.StartsWith("set_LoginValidator_"))
                    set_LoginValidator_Proxy = item.Value;
                else if (key.StartsWith("get_LoginType_"))
                    get_LoginType_Proxy = item.Value;
                else if (key.StartsWith("set_LoginType_"))
                    set_LoginType_Proxy = item.Value;
                else if (key.StartsWith("Login_"))
                    Login_Proxy = item.Value;
                else if (key.StartsWith("Logout_"))
                    Logout_Proxy = item.Value;
            }

            //dictionary.TryGetValue("get_LoginValidator", out get_LoginValidator_Proxy);
            //dictionary.TryGetValue("set_LoginValidator", out set_LoginValidator_Proxy);
            //dictionary.TryGetValue("get_LoginType", out get_LoginType_Proxy);
            //dictionary.TryGetValue("set_LoginType", out set_LoginType_Proxy);
            //dictionary.TryGetValue("Login", out Login_Proxy);
            //dictionary.TryGetValue("Logout", out Logout_Proxy);
        }

        public LoginServiceProxy(IInterceptor interceptor, ILoginService service)
        {
            _interceptor = interceptor;
            _service = service;
        }

        public string LoginType
        {
            get
            {
                if (_interceptor != null)
                {
                    object[] parameters = new object[0];
                    IInvocation invocation = new StandardInvocation(parameters, _service, get_LoginType_Proxy);
                    _interceptor.Intercept(invocation);
                    return (string)invocation.ReturnValue;
                }
                return _service.LoginType;
            }
            set
            {
                if (_interceptor != null)
                {
                    object[] parameters = new object[] { value };
                    IInvocation invocation = new StandardInvocation(parameters, _service, set_LoginType_Proxy);
                    _interceptor.Intercept(invocation);
                    return;
                }
                _service.LoginType = value;
            }
        }

        public ILoginValidator LoginValidator
        {
            get
            {
                if (_interceptor != null)
                {
                    object[] parameters = new object[0];
                    IInvocation ınvocation = new StandardInvocation(parameters, _service, get_LoginValidator_Proxy);
                    _interceptor.Intercept(ınvocation);
                    return (ILoginValidator)ınvocation.ReturnValue;
                }
                return _service.LoginValidator;
            }
            set
            {
                if (_interceptor != null)
                {
                    object[] parameters = new object[] { value };
                    IInvocation invocation = new StandardInvocation(parameters, _service, set_LoginValidator_Proxy);
                    _interceptor.Intercept(invocation);
                    return;
                }
                _service.LoginValidator = value;
            }
        }

        public string Login(string userName, string password)
        {
            if (_interceptor != null)
            {
                object[] parameters = new object[] { userName, password };

                IInvocation invocation = new StandardInvocation(parameters, _service, Login_Proxy);
                _interceptor.Intercept(invocation);
                return (string)invocation.ReturnValue;
            }
            return _service.Login(userName, password);
        }

        public void Logout(string sessionKey)
        {
            if (_interceptor != null)
            {
                object[] parameters = new object[] { sessionKey };
                IInvocation invocation = new StandardInvocation(parameters, _service, Logout_Proxy);
                _interceptor.Intercept(invocation);
                return;
            }
            _service.Logout(sessionKey);
        }

    }

    public interface IOrm
    {
        IEnumerable<dynamic> Query(string commandText = "", object args = null);
        //IEnumerable<object> Query(Type type, string commandText = "", Options options = null, object args = null);
        IEnumerable<T> Query<T>(string commandText = "", object args = null) where T : new();

        string[] GetTableColumns(string tableName);

        string GetDatabaseName();
        TimeSpan DatabaseDuration();
        DatabaseTypee DbType();
    }

    public class Orm : IOrm
    {
        public virtual TimeSpan DatabaseDuration()
        {
            return TimeSpan.Zero;
        }

        public DatabaseTypee DbType()
        {
            return DatabaseTypee.Oracle;
        }

        public virtual string GetDatabaseName()
        {
            return null;
        }

        public virtual string[] GetTableColumns(string tableName)
        {
            return null;
        }

        public virtual IEnumerable<dynamic> Query(string commandText = "", object args = null)
        {
            return null;
        }

        public virtual IEnumerable<T> Query<T>(string commandText = "", object args = null) where T : new()
        {
            return null;
        }
    }

    public enum DatabaseTypee
    {
        Oracle,
        SqlServer
    }

    public class Orm_Proxy : IProxyType, IOrm
    {
        public static ProxyMethod Query_16841201;

        public static ProxyMethod Query_108852912;

        public static ProxyMethod GetTableColumns_ProxyMethod;

        public static ProxyMethod GetDatabaseName_ProxyMethod;

        public static ProxyMethod DatabaseDuration_ProxyMethod;

        public static ProxyMethod DbType_ProxyMethod;

        private IInterceptor interceptor;

        private IOrm service;

        public Orm_Proxy(IInterceptor ınterceptor, IOrm orm)
        {
            this.interceptor = ınterceptor;
            this.service = orm;
        }

        static Orm_Proxy()
        {
            Dictionary<string, ProxyMethod> dictionary = ProxyTypeUtility.CreateMethodInvokers(typeof(IOrm));

            string key;
            foreach (var item in dictionary)
            {
                key = item.Key;

                if (key.StartsWith("GetTableColumns_"))
                    GetTableColumns_ProxyMethod = item.Value;
                else if (key.StartsWith("GetDatabaseName_"))
                    GetDatabaseName_ProxyMethod = item.Value;
                else if (key.StartsWith("DatabaseDuration_"))
                    DatabaseDuration_ProxyMethod = item.Value;
                else if (key.StartsWith("DbType_"))
                    DbType_ProxyMethod = item.Value;
            }

            //dictionary.TryGetValue("Query_16841201", out Orm_Proxy.Query_16841201);
            //dictionary.TryGetValue("Query_108852912", out Orm_Proxy.Query_108852912);
            //dictionary.TryGetValue("GetTableColumns_20936295", out Orm_Proxy.GetTableColumns_20936295);
            //dictionary.TryGetValue("GetDatabaseName_18681950", out Orm_Proxy.GetDatabaseName_18681950);
            //dictionary.TryGetValue("DatabaseDuration_6367254", out Orm_Proxy.DatabaseDuration_6367254);
        }

        public IEnumerable<object> Query(string text, object obj)
        {
            if (this.interceptor != null)
            {
                IInvocation ınvocation = new StandardInvocation(new object[]
                {
                text,
                obj
                }, this.service, Orm_Proxy.Query_16841201);
                this.interceptor.Intercept(ınvocation);
                return (IEnumerable<object>)ınvocation.ReturnValue;
            }
            return (this.service == null) ? null : this.service.Query(text, obj);
        }

        public IEnumerable<T> Query<T>(string text, object obj) where T : new()
        {
            if (this.interceptor != null)
            {
                IInvocation ınvocation = new StandardInvocation(new object[]
                {
                text,
                obj
                }, this.service, Orm_Proxy.Query_108852912);
                this.interceptor.Intercept(ınvocation);
                return (IEnumerable<T>)ınvocation.ReturnValue;
            }
            return (this.service == null) ? null : this.service.Query<T>(text, obj);
        }

        public string[] GetTableColumns(string text)
        {
            if (this.interceptor != null)
            {
                IInvocation ınvocation = new StandardInvocation(new object[]
                {
                text
                }, this.service, Orm_Proxy.GetTableColumns_ProxyMethod);
                this.interceptor.Intercept(ınvocation);
                return (string[])ınvocation.ReturnValue;
            }
            return (this.service == null) ? null : this.service.GetTableColumns(text);
        }

        public string GetDatabaseName()
        {
            if (this.interceptor != null)
            {
                object[] parameters = new object[0];
                IInvocation ınvocation = new StandardInvocation(parameters, this.service, Orm_Proxy.GetDatabaseName_ProxyMethod);
                this.interceptor.Intercept(ınvocation);
                return (string)ınvocation.ReturnValue;
            }
            return (this.service == null) ? null : this.service.GetDatabaseName();
        }

        public TimeSpan DatabaseDuration()
        {
            if (this.interceptor != null)
            {
                object[] parameters = new object[0];
                IInvocation ınvocation = new StandardInvocation(parameters, this.service, Orm_Proxy.DatabaseDuration_ProxyMethod);
                this.interceptor.Intercept(ınvocation);
                return (ınvocation.ReturnValue == null) ? TimeSpan.Zero : (TimeSpan)ınvocation.ReturnValue;
            }
            return (this.service == null) ? TimeSpan.Zero : this.service.DatabaseDuration();
        }

        public DatabaseTypee DbType()
        {
            if (this.interceptor != null)
            {
                object[] parameters = new object[0];
                IInvocation ınvocation = new StandardInvocation(parameters, this.service, Orm_Proxy.DbType_ProxyMethod);
                this.interceptor.Intercept(ınvocation);
                return (ınvocation.ReturnValue == null) ? DatabaseTypee.Oracle : ((DatabaseTypee)ınvocation.ReturnValue);
            }
            return (this.service == null) ? (DatabaseTypee)0 : this.service.DbType();
        }
    }

    public class Orm_Proxy2 : Orm, IProxyType
    {
        public static ProxyMethod DatabaseDuration_62422776;

        public static ProxyMethod DbType_49692969;

        public static ProxyMethod GetDatabaseName_11372239;

        public static ProxyMethod GetTableColumns_22617969;

        public static ProxyMethod Query_37518860;

        public static ProxyMethod Query_110622692;

        private IInterceptor interceptor;

        private Orm service;

        public Orm_Proxy2(IInterceptor ınterceptor, Orm orm)
        {
            this.interceptor = ınterceptor;
            this.service = orm;
        }

        static Orm_Proxy2()
        {
            Dictionary<string, ProxyMethod> dictionary = ProxyTypeUtility.CreateMethodInvokers(typeof(Orm));

            string key;
            foreach (var item in dictionary)
            {
                key = item.Key;

                if (key.StartsWith("GetTableColumns_"))
                    GetTableColumns_22617969 = item.Value;
                else if (key.StartsWith("GetDatabaseName_"))
                    GetDatabaseName_11372239 = item.Value;
                else if (key.StartsWith("DatabaseDuration_"))
                    DatabaseDuration_62422776 = item.Value;
                else if (key.StartsWith("DbType_"))
                    DbType_49692969 = item.Value;
            }

            //dictionary.TryGetValue("DatabaseDuration_62422776", out Orm_Proxy2.DatabaseDuration_62422776);
            //dictionary.TryGetValue("DbType_49692969", out Orm_Proxy2.DbType_49692969);
            //dictionary.TryGetValue("GetDatabaseName_11372239", out Orm_Proxy2.GetDatabaseName_11372239);
            //dictionary.TryGetValue("GetTableColumns_22617969", out Orm_Proxy2.GetTableColumns_22617969);
            //dictionary.TryGetValue("Query_37518860", out Orm_Proxy2.Query_37518860);
            //dictionary.TryGetValue("Query_110622692", out Orm_Proxy2.Query_110622692);
        }

        public override TimeSpan DatabaseDuration()
        {
            if (this.interceptor != null)
            {
                object[] parameters = new object[0];
                IInvocation ınvocation = new StandardInvocation(parameters, this.service, Orm_Proxy2.DatabaseDuration_62422776);
                this.interceptor.Intercept(ınvocation);
                return (ınvocation.ReturnValue == null) ? TimeSpan.MinValue : ((TimeSpan)ınvocation.ReturnValue);
            }
            return (this.service == null) ? TimeSpan.MinValue : this.service.DatabaseDuration();
        }

        public new DatabaseTypee DbType()
        {
            if (this.interceptor != null)
            {
                object[] parameters = new object[0];
                IInvocation ınvocation = new StandardInvocation(parameters, this.service, Orm_Proxy2.DbType_49692969);
                this.interceptor.Intercept(ınvocation);
                return (ınvocation.ReturnValue == null) ? DatabaseTypee.Oracle : ((DatabaseTypee)ınvocation.ReturnValue);
            }
            return (this.service == null) ? DatabaseTypee.Oracle : this.service.DbType();
        }

        public new string GetDatabaseName()
        {
            if (this.interceptor != null)
            {
                object[] parameters = new object[0];
                IInvocation ınvocation = new StandardInvocation(parameters, this.service, Orm_Proxy2.GetDatabaseName_11372239);
                this.interceptor.Intercept(ınvocation);
                return (ınvocation.ReturnValue == null) ? null : ((string)ınvocation.ReturnValue);
            }
            return (this.service == null) ? null : this.service.GetDatabaseName();
        }

        public new string[] GetTableColumns(string text)
        {
            if (this.interceptor != null)
            {
                IInvocation ınvocation = new StandardInvocation(new object[]
                {
                text
                }, this.service, Orm_Proxy2.GetTableColumns_22617969);
                this.interceptor.Intercept(ınvocation);
                return (ınvocation.ReturnValue == null) ? null : ((string[])ınvocation.ReturnValue);
            }
            return (this.service == null) ? null : this.service.GetTableColumns(text);
        }

        public new IEnumerable<object> Query(string text, object obj)
        {
            if (this.interceptor != null)
            {
                IInvocation ınvocation = new StandardInvocation(new object[]
                {
                text,
                obj
                }, this.service, Orm_Proxy2.Query_37518860);
                this.interceptor.Intercept(ınvocation);
                return (ınvocation.ReturnValue == null) ? null : ((IEnumerable<object>)ınvocation.ReturnValue);
            }
            return (this.service == null) ? null : this.service.Query(text, obj);
        }

        public new IEnumerable<T> Query<T>(string text, object obj) where T : new()
        {
            if (this.interceptor != null)
            {
                IInvocation ınvocation = new StandardInvocation(new object[]
                {
                text,
                obj
                }, this.service, Orm_Proxy2.Query_110622692);
                this.interceptor.Intercept(ınvocation);
                return (ınvocation.ReturnValue == null) ? null : ((IEnumerable<T>)ınvocation.ReturnValue);
            }
            return (this.service == null) ? null : this.service.Query<T>(text, obj);
        }
    }

}
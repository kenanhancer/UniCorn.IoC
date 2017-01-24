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
    class SomeService : IService { }

    interface IClient { IService Service { get; } }
    class SomeClient : IClient
    {
        public IService Service { get; private set; }
        public SomeClient(IService service) { Service = service; }
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
            Dictionary<string, ProxyMethod> dictionary = ProxyTypeFactory.CreateMethodInvokers(typeof(ILoginService));

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
    }

    public class Orm : IOrm
    {
        public IEnumerable<dynamic> Query(string commandText = "", object args = null)
        {
            return null;
        }

        public IEnumerable<T> Query<T>(string commandText = "", object args = null) where T : new()
        {
            return null;
        }
    }

}
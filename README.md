# UniCorn.IoC
UniCorn.IoC is a lightweight, fast and full-featured IoC Container for .NET Core

##How To Install It?
Install from `Nuget`, you should write Package Manager Console below code and `Uni.IoC` will be installed automatically.
```
Install-Package Uni.IoC
```
By the way, you can also reach `Uni.IoC` `NuGet` package from https://www.nuget.org/packages/Uni.IoC/ address.

##How Do You Use It?
It is easy to use. Let's have a look at a simple example.

Firstly, UniIoC object is created named container. And register a interface to a concrete type. Lastly, resolve instance according to registered interface as shown below code.

```csharp
UniIoC container = new UniIoC();

container.Register(ServiceCriteria.For<IShape>().ImplementedBy<Circle>());

var shape = container.Resolve<IShape>();
```

##Register and Resolve concrete types without interface
You can register concrete types without interface as below;

```csharp
UniIoC container = new UniIoC();

container.Register(ServiceCriteria.For<Circle>());
container.Register(ServiceCriteria.For<Square>());

IShape circle = container.Resolve<Circle>();
IShape square = container.Resolve<Square>();
```

##Resolving different concrete types which implement same interface
If you have different concrete types which imlement same interface, you can register them with different names. As you can see below sample code, there is one `IShape` interface and two concrete types `Circle`, `Square` which use that interface.

###Test Service Types
```csharp
public interface IShape
{
}

public class Circle : IShape
{
}

public class Square : IShape
{
}
```

###Usage in application
Circle and Square concrete types should be registered with name because of same interface(IShape), so that UniCorn.IoC can resolve services.
```csharp
UniIoC container = new UniIoC();

container.Register(ServiceCriteria.For<IShape>().ImplementedBy<Circle>().Named("Circle"));
container.Register(ServiceCriteria.For<IShape>().ImplementedBy<Square>().Named("Square"));

var circle = container.Resolve<IShape>("Circle");
var square = container.Resolve<IShape>("Square");
```

##Register and Resolve Complex types
There are two concrete types which implement ILoginService interface and those have constructor which has parameter named loginValidator. UniCorn.IoC can register and resolve these complex types as below. I will use following test code for use cases.

###Test Service Types
```csharp
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
```

###Usage in application
This sample usage will register two concrete types named EmailLoginService and PhoneLoginService with EmailLoginValidator and PhoneLoginValidator dependencies respectively. Those two concrete types are using same interface, so we entitled them Email and Phone respectively so that, we can just resolve services with name.

```csharp
UniIoC container = new UniIoC();

container.Register(ServiceCriteria.For<ILoginValidator>().ImplementedBy<EmailLoginValidator>().Named("EmailLoginValidator"));
container.Register(ServiceCriteria.For<ILoginValidator>().ImplementedBy<PhoneLoginValidator>().Named("PhoneLoginValidator"));

container.Register(ServiceCriteria.For<ILoginService>().ImplementedBy<EmailLoginService>().Named("Email").Dependencies(new { loginValidator = new EmailLoginValidator() }));
container.Register(ServiceCriteria.For<ILoginService>().ImplementedBy<PhoneLoginService>().Named("Phone").Dependencies(new { loginValidator = new PhoneLoginValidator() }));

PhoneLoginService phoneLoginService = container.Resolve<PhoneLoginService>("Phone");
ILoginService emailLoginService = container.Resolve<ILoginService>("Email");

string sessionKey1 = phoneLoginService.Login("user1", "password1");
string sessionKey2 = emailLoginService.Login("user2", "password2");
```

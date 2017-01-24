using System;
using System.Collections.Generic;
using Xunit;

namespace UniCorn.IoC.UnitTests
{
    public class UniIoCTest
    {
        [Fact]
        public void TestMethod1()
        {
            UniIoC container = new UniIoC();

            container.Register(ServiceCriteria.For<ILoginValidator>().ImplementedBy<EmailLoginValidator>().Named("EmailLoginValidator"));
            container.Register(ServiceCriteria.For<ILoginValidator>().ImplementedBy<PhoneLoginValidator>().Named("PhoneLoginValidator"));

            container.Register(ServiceCriteria.For<ILoginService>().ImplementedBy<EmailLoginService>().Named("Email").Dependencies(new { loginValidator = new EmailLoginValidator() }));
            container.Register(ServiceCriteria.For<ILoginService>().ImplementedBy<PhoneLoginService>().Named("Phone").Dependencies(new { loginValidator = new PhoneLoginValidator() }));

            PhoneLoginService phoneLoginService = container.Resolve<PhoneLoginService>("Phone");
            ILoginService emailLoginService = container.Resolve<ILoginService>("Email");

            Assert.NotNull(phoneLoginService);
            Assert.NotNull(emailLoginService);

            string sessionKey1 = phoneLoginService.Login("", "");
            string sessionKey2 = emailLoginService.Login("", "");

            Assert.False(String.IsNullOrEmpty(sessionKey1));
            Assert.False(String.IsNullOrEmpty(sessionKey2));
        }

        [Fact]
        public void TestMethod2()
        {
            UniIoC container = new UniIoC();

            container.Register(ServiceCriteria.For<ILoginValidator>().ImplementedBy<EmailLoginValidator>().Named("EmailLoginValidator"));
            container.Register(ServiceCriteria.For<ILoginValidator>().ImplementedBy<PhoneLoginValidator>().Named("PhoneLoginValidator"));

            IDictionary<string, object> argumentsEmail = new Dictionary<string, object> { { "loginValidator", new EmailLoginValidator() } };
            IDictionary<string, object> argumentsPhone = new Dictionary<string, object> { { "loginValidator", new PhoneLoginValidator() } };

            container.Register(ServiceCriteria.For<ILoginService>().ImplementedBy<EmailLoginService>().Named("Email").Dependencies(argumentsEmail));
            container.Register(ServiceCriteria.For<ILoginService>().ImplementedBy<PhoneLoginService>().Named("Phone").Dependencies(argumentsPhone));

            PhoneLoginService phoneLoginService = container.Resolve<PhoneLoginService>("Phone");
            ILoginService emailLoginService = container.Resolve<ILoginService>("Email");

            Assert.NotNull(phoneLoginService);
            Assert.NotNull(emailLoginService);

            string sessionKey1 = phoneLoginService.Login("", "");
            string sessionKey2 = emailLoginService.Login("", "");

            Assert.False(String.IsNullOrEmpty(sessionKey1));
            Assert.False(String.IsNullOrEmpty(sessionKey2));
        }

        [Fact]
        public void TestMethod3()
        {
            UniIoC container = new UniIoC();

            container.Register(ServiceCriteria.For<ILoginValidator>().ImplementedBy<EmailLoginValidator>().Named("EmailLoginValidator"));
            container.Register(ServiceCriteria.For<ILoginValidator>().ImplementedBy<PhoneLoginValidator>().Named("PhoneLoginValidator"));

            container.Register(ServiceCriteria.For<ILoginService>().ImplementedBy<EmailLoginService>().Named("Email").Dependencies(new { loginValidator = container.Resolve<ILoginValidator>("EmailLoginValidator") }));
            container.Register(ServiceCriteria.For<ILoginService>().ImplementedBy<PhoneLoginService>().Named("Phone").Dependencies(new { loginValidator = container.Resolve<ILoginValidator>("PhoneLoginValidator") }));

            PhoneLoginService phoneLoginService = container.Resolve<PhoneLoginService>("Phone");
            ILoginService emailLoginService = container.Resolve<ILoginService>("Email");

            Assert.NotNull(phoneLoginService);
            Assert.NotNull(emailLoginService);

            string sessionKey1 = phoneLoginService.Login("", "");
            string sessionKey2 = emailLoginService.Login("", "");

            Assert.False(String.IsNullOrEmpty(sessionKey1));
            Assert.False(String.IsNullOrEmpty(sessionKey2));
        }

        [Fact]
        public void TestMethod4()
        {
            UniIoC container = new UniIoC();

            ILoginValidator emailLoginValidator = new EmailLoginValidator();
            ILoginValidator phoneLoginValidator = new PhoneLoginValidator();

            container.Register(ServiceCriteria.For<ILoginValidator>().ImplementedBy<EmailLoginValidator>().Named("EmailLoginValidator").Instance(emailLoginValidator));
            container.Register(ServiceCriteria.For<ILoginValidator>().ImplementedBy<PhoneLoginValidator>().Named("PhoneLoginValidator").Instance(phoneLoginValidator));

            container.Register(ServiceCriteria.For<ILoginService>().ImplementedBy<EmailLoginService>().Named("Email").Dependencies(new { loginValidator = container.Resolve<ILoginValidator>("EmailLoginValidator") }));
            container.Register(ServiceCriteria.For<ILoginService>().ImplementedBy<PhoneLoginService>().Named("Phone").Dependencies(new { loginValidator = container.Resolve<ILoginValidator>("PhoneLoginValidator") }));

            PhoneLoginService phoneLoginService1 = container.Resolve<PhoneLoginService>("Phone");
            PhoneLoginService phoneLoginService2 = container.Resolve<PhoneLoginService>("Phone");
            EmailLoginService emailLoginService = container.Resolve<EmailLoginService>("Email");

            Assert.NotNull(phoneLoginService1);
            Assert.NotNull(phoneLoginService2);
            Assert.NotNull(emailLoginService);

            string sessionKey1 = phoneLoginService1.Login("", "");
            string sessionKey2 = phoneLoginService2.Login("", "");
            string sessionKey3 = emailLoginService.Login("", "");

            Assert.False(String.IsNullOrEmpty(sessionKey1));
            Assert.False(String.IsNullOrEmpty(sessionKey2));
            Assert.False(String.IsNullOrEmpty(sessionKey3));

            Assert.NotEqual(phoneLoginService1.LoginValidator, phoneLoginValidator);
            Assert.NotEqual(phoneLoginService2.LoginValidator, phoneLoginValidator);
            Assert.NotEqual(emailLoginService.LoginValidator, emailLoginValidator);

            Assert.IsType<PhoneLoginService>(phoneLoginService1);
            Assert.IsType<PhoneLoginService>(phoneLoginService2);
            Assert.IsType<EmailLoginService>(emailLoginService);
            Assert.IsAssignableFrom<ILoginService>(phoneLoginService1);
            Assert.IsAssignableFrom<ILoginService>(phoneLoginService2);
            Assert.IsAssignableFrom<ILoginService>(emailLoginService);
        }

        [Fact]
        public void TestMethod5()
        {
            UniIoC container = new UniIoC();

            ILoginValidator emailLoginValidator = new EmailLoginValidator();
            ILoginValidator phoneLoginValidator = new PhoneLoginValidator();

            container.Register(ServiceCriteria.For<ILoginValidator>().ImplementedBy<EmailLoginValidator>().Named("EmailLoginValidator").Instance(emailLoginValidator));
            container.Register(ServiceCriteria.For<ILoginValidator>().ImplementedBy<PhoneLoginValidator>().Named("PhoneLoginValidator").Instance(phoneLoginValidator));

            container.Register(ServiceCriteria.For<ILoginService>().ImplementedBy<EmailLoginService>().Named("Email").OnInstanceCreating(f => new EmailLoginService(f.Resolve<ILoginValidator>("EmailLoginValidator"))));
            container.Register(ServiceCriteria.For<ILoginService>().ImplementedBy<PhoneLoginService>().Named("Phone").OnInstanceCreating(f => new PhoneLoginService(f.Resolve<ILoginValidator>("PhoneLoginValidator"))));

            ILoginService phoneLoginService1 = container.Resolve<ILoginService>("Phone");
            ILoginService phoneLoginService2 = container.Resolve<ILoginService>("Phone");
            ILoginService emailLoginService = container.Resolve<ILoginService>("Email");

            Assert.NotNull(phoneLoginService1);
            Assert.NotNull(phoneLoginService2);
            Assert.NotNull(emailLoginService);

            string sessionKey1 = phoneLoginService1.Login("", "");
            string sessionKey2 = phoneLoginService2.Login("", "");
            string sessionKey3 = emailLoginService.Login("", "");

            Assert.False(String.IsNullOrEmpty(sessionKey1));
            Assert.False(String.IsNullOrEmpty(sessionKey2));
            Assert.False(String.IsNullOrEmpty(sessionKey3));

            Assert.NotEqual(phoneLoginService1.LoginValidator, phoneLoginValidator);
            Assert.NotEqual(phoneLoginService2.LoginValidator, phoneLoginValidator);
            Assert.NotEqual(emailLoginService.LoginValidator, emailLoginValidator);

            Assert.IsType<PhoneLoginService>(phoneLoginService1);
            Assert.IsType<PhoneLoginService>(phoneLoginService2);
            Assert.IsType<EmailLoginService>(emailLoginService);
            Assert.IsAssignableFrom<ILoginService>(phoneLoginService1);
            Assert.IsAssignableFrom<ILoginService>(phoneLoginService2);
            Assert.IsAssignableFrom<ILoginService>(emailLoginService);
        }

        [Fact]
        public void TestMethod6()
        {
            UniIoC container = new UniIoC();

            container.Register(ServiceCriteria.For<ILoginValidator>().ImplementedBy<EmailLoginValidator>().Named("EmailLoginValidator"));
            container.Register(ServiceCriteria.For<ILoginValidator>().ImplementedBy<PhoneLoginValidator>().Named("PhoneLoginValidator"));

            container.Register(ServiceCriteria.For<ILoginService>().ImplementedBy<EmailLoginService>().Named("Email").OnInstanceCreating(f => new EmailLoginService(f.Resolve<ILoginValidator>("EmailLoginValidator"))));
            container.Register(ServiceCriteria.For<ILoginService>().ImplementedBy<PhoneLoginService>().Named("Phone").OnInstanceCreating(f => new PhoneLoginService(f.Resolve<ILoginValidator>("PhoneLoginValidator"))));

            ILoginService phoneLoginService1 = container.Resolve<ILoginService>("Phone");
            ILoginService phoneLoginService2 = container.Resolve<ILoginService>("Phone");
            ILoginService emailLoginService = container.Resolve<ILoginService>("Email");

            Assert.NotNull(phoneLoginService1);
            Assert.NotNull(phoneLoginService2);
            Assert.NotNull(emailLoginService);

            string sessionKey1 = phoneLoginService1.Login("", "");
            string sessionKey2 = phoneLoginService2.Login("", "");
            string sessionKey3 = emailLoginService.Login("", "");

            Assert.False(String.IsNullOrEmpty(sessionKey1));
            Assert.False(String.IsNullOrEmpty(sessionKey2));
            Assert.False(String.IsNullOrEmpty(sessionKey3));

            Assert.NotEqual(phoneLoginService1.LoginValidator, phoneLoginService2.LoginValidator);

            Assert.IsType<PhoneLoginService>(phoneLoginService1);
            Assert.IsType<PhoneLoginService>(phoneLoginService2);
            Assert.IsType<EmailLoginService>(emailLoginService);
            Assert.IsAssignableFrom<ILoginService>(phoneLoginService1);
            Assert.IsAssignableFrom<ILoginService>(phoneLoginService2);
            Assert.IsAssignableFrom<ILoginService>(emailLoginService);
        }

        [Fact]
        public void TestMethod7()
        {
            UniIoC container = new UniIoC();

            container.Register(ServiceCriteria.For<ILoginValidator>().ImplementedBy<EmailLoginValidator>().Named("EmailLoginValidator").LifeCycle(LifeCycleEnum.Singleton));
            container.Register(ServiceCriteria.For<ILoginValidator>().ImplementedBy<PhoneLoginValidator>().Named("PhoneLoginValidator").LifeCycle(LifeCycleEnum.Singleton));

            container.Register(ServiceCriteria.For<ILoginService>().ImplementedBy<EmailLoginService>().Named("Email").OnInstanceCreating(f => new EmailLoginService(f.Resolve<ILoginValidator>("EmailLoginValidator"))));
            container.Register(ServiceCriteria.For<ILoginService>().ImplementedBy<PhoneLoginService>().Named("Phone").OnInstanceCreating(f => new PhoneLoginService(f.Resolve<ILoginValidator>("PhoneLoginValidator"))));

            ILoginService phoneLoginService1 = container.Resolve<ILoginService>("Phone");
            ILoginService phoneLoginService2 = container.Resolve<ILoginService>("Phone");
            ILoginService emailLoginService = container.Resolve<ILoginService>("Email");

            Assert.NotNull(phoneLoginService1);
            Assert.NotNull(phoneLoginService2);
            Assert.NotNull(emailLoginService);

            string sessionKey1 = phoneLoginService1.Login("", "");
            string sessionKey2 = phoneLoginService2.Login("", "");
            string sessionKey3 = emailLoginService.Login("", "");

            Assert.False(String.IsNullOrEmpty(sessionKey1));
            Assert.False(String.IsNullOrEmpty(sessionKey2));
            Assert.False(String.IsNullOrEmpty(sessionKey3));

            Assert.Equal(phoneLoginService1.LoginValidator, phoneLoginService2.LoginValidator);

            Assert.IsType<PhoneLoginService>(phoneLoginService1);
            Assert.IsType<PhoneLoginService>(phoneLoginService2);
            Assert.IsType<EmailLoginService>(emailLoginService);
            Assert.IsAssignableFrom<ILoginService>(phoneLoginService1);
            Assert.IsAssignableFrom<ILoginService>(phoneLoginService2);
            Assert.IsAssignableFrom<ILoginService>(emailLoginService);
        }

        [Fact]
        public void TestMethod8()
        {
            UniIoC container = new UniIoC();

            container.Register(ServiceCriteria.For<ILoginValidator>().ImplementedBy<EmailLoginValidator>().Named("EmailLoginValidator").LifeCycle(LifeCycleEnum.Singleton));
            container.Register(ServiceCriteria.For<ILoginValidator>().ImplementedBy<PhoneLoginValidator>().Named("PhoneLoginValidator").LifeCycle(LifeCycleEnum.Singleton));

            container.Register(ServiceCriteria.For<ILoginService>().ImplementedBy<EmailLoginService>().Named("Email").Dependencies(new { loginValidator = new EmailLoginValidator() }).OnIntercepting(f => f.Proceed()));
            container.Register(ServiceCriteria.For<ILoginService>().ImplementedBy<PhoneLoginService>().Named("Phone").OnInstanceCreating(f => new PhoneLoginService(f.Resolve<ILoginValidator>("PhoneLoginValidator"))));

            ILoginService phoneLoginService1 = container.Resolve<ILoginService>("Phone");
            ILoginService phoneLoginService2 = container.Resolve<ILoginService>("Phone");
            ILoginService emailLoginService = container.Resolve<ILoginService>("Email");

            Assert.NotNull(phoneLoginService1);
            Assert.NotNull(phoneLoginService2);
            Assert.NotNull(emailLoginService);

            string sessionKey1 = phoneLoginService1.Login("", "");
            string sessionKey2 = phoneLoginService2.Login("", "");
            string sessionKey3 = emailLoginService.Login("", "");

            Assert.False(String.IsNullOrEmpty(sessionKey1));
            Assert.False(String.IsNullOrEmpty(sessionKey2));
            Assert.False(String.IsNullOrEmpty(sessionKey3));

            Assert.Equal(phoneLoginService1.LoginValidator, phoneLoginService2.LoginValidator);

            Assert.IsType<PhoneLoginService>(phoneLoginService1);
            Assert.IsType<PhoneLoginService>(phoneLoginService2);
            Assert.IsNotType<EmailLoginService>(emailLoginService);
            Assert.IsAssignableFrom<ILoginService>(phoneLoginService1);
            Assert.IsAssignableFrom<ILoginService>(phoneLoginService2);
            Assert.IsAssignableFrom<ILoginService>(emailLoginService);
        }

        [Fact]
        public void TestMethod9()
        {
            UniIoC container = new UniIoC();

            container.Register(ServiceCriteria.For<ILoginValidator>().ImplementedBy<EmailLoginValidator>().Named("EmailLoginValidator").LifeCycle(LifeCycleEnum.Singleton));
            container.Register(ServiceCriteria.For<ILoginValidator>().ImplementedBy<PhoneLoginValidator>().Named("PhoneLoginValidator").LifeCycle(LifeCycleEnum.Singleton));

            container.Register(ServiceCriteria.For<ILoginService>().ImplementedBy<EmailLoginService>().Named("Email").Dependencies(new { loginValidator = new EmailLoginValidator() }).OnIntercepting(OnIntercepting));
            container.Register(ServiceCriteria.For<ILoginService>().ImplementedBy<PhoneLoginService>().Named("Phone").OnInstanceCreating(f => new PhoneLoginService(f.Resolve<ILoginValidator>("PhoneLoginValidator"))));

            ILoginService phoneLoginService1 = container.Resolve<ILoginService>("Phone");
            ILoginService phoneLoginService2 = container.Resolve<ILoginService>("Phone");
            ILoginService emailLoginService = container.Resolve<ILoginService>("Email");

            Assert.NotNull(phoneLoginService1);
            Assert.NotNull(phoneLoginService2);
            Assert.NotNull(emailLoginService);

            string sessionKey1 = phoneLoginService1.Login("", "");
            string sessionKey2 = phoneLoginService2.Login("", "");
            string sessionKey3 = emailLoginService.Login("", "");

            Assert.False(String.IsNullOrEmpty(sessionKey1));
            Assert.False(String.IsNullOrEmpty(sessionKey2));
            Assert.False(String.IsNullOrEmpty(sessionKey3));

            Assert.Equal(phoneLoginService1.LoginValidator, phoneLoginService2.LoginValidator);

            Assert.IsType<PhoneLoginService>(phoneLoginService1);
            Assert.IsType<PhoneLoginService>(phoneLoginService2);
            Assert.IsNotType<EmailLoginService>(emailLoginService);
            Assert.IsAssignableFrom<ILoginService>(phoneLoginService1);
            Assert.IsAssignableFrom<ILoginService>(phoneLoginService2);
            Assert.IsAssignableFrom<ILoginService>(emailLoginService);
        }

        public void OnIntercepting(IInvocation invocation)
        {
            Console.WriteLine("{0} method is invoked.", invocation.Method.Name);
            invocation.Proceed();
        }

        [Fact]
        public void TestMethod10()
        {
            //Eğer interceptor kullanılacaksa For<> methodu ile oluşturulacak class'ın interface'i belirtilmeli. Böylece arka tarafta oluşan proxy class interface'i implemente ettiği için resolve olduğunda tekrar interface dönüşümünde sıkıntı olmayacaktır. Eğer interfacesiz kullanılırsa proxy class For<> ile verilen concrete type'dan inherit olmadığı için resolve ederken concrete class'a convert olamadığı için runtime'da hata verilir.
            UniIoC container = new UniIoC();

            container.Register(ServiceCriteria.For<ILoginValidator>().ImplementedBy<EmailLoginValidator>().Named("EmailLoginValidator").LifeCycle(LifeCycleEnum.Singleton));
            container.Register(ServiceCriteria.For<ILoginValidator>().ImplementedBy<PhoneLoginValidator>().Named("PhoneLoginValidator").LifeCycle(LifeCycleEnum.Singleton));

            container.Register(ServiceCriteria.For<ILoginService>().ImplementedBy<EmailLoginService>().Named("Email").OnInstanceCreating(f => new EmailLoginService(new EmailLoginValidator())).OnIntercepting(OnIntercepting));
            container.Register(ServiceCriteria.For<ILoginService>().ImplementedBy<PhoneLoginService>().Named("Phone").OnInstanceCreating(f => new PhoneLoginService(f.Resolve<ILoginValidator>("PhoneLoginValidator"))));

            var phoneLoginService1 = container.Resolve<ILoginService>("Phone");
            var phoneLoginService2 = container.Resolve<ILoginService>("Phone");
            var emailLoginService = container.Resolve<ILoginService>("Email");

            Assert.NotNull(phoneLoginService1);
            Assert.NotNull(phoneLoginService2);
            Assert.NotNull(emailLoginService);

            string sessionKey1 = phoneLoginService1.Login("", "");
            string sessionKey2 = phoneLoginService2.Login("", "");
            string sessionKey3 = emailLoginService.Login("", "");

            Assert.False(String.IsNullOrEmpty(sessionKey1));
            Assert.False(String.IsNullOrEmpty(sessionKey2));
            Assert.False(String.IsNullOrEmpty(sessionKey3));

            Assert.Equal(phoneLoginService1.LoginValidator, phoneLoginService2.LoginValidator);

            Assert.IsType<PhoneLoginService>(phoneLoginService1);
            Assert.IsType<PhoneLoginService>(phoneLoginService2);
            Assert.IsNotType<EmailLoginService>(emailLoginService);
            Assert.IsAssignableFrom<ILoginService>(phoneLoginService1);
            Assert.IsAssignableFrom<ILoginService>(phoneLoginService2);
            Assert.IsAssignableFrom<ILoginService>(emailLoginService);
        }

        [Fact]
        public void TestMethod11()
        {
            UniIoC container = new UniIoC();

            container.Register(ServiceCriteria.For<IShape>().ImplementedBy<Circle>());

            IShape circle = container.Resolve<IShape>();

            Assert.NotNull(circle);

            Assert.IsType<Circle>(circle);
            Assert.IsAssignableFrom<IShape>(circle);
        }

        [Fact]
        public void TestMethod12()
        {
            UniIoC container = new UniIoC();

            container.Register(ServiceCriteria.For<IShape>().ImplementedBy<Circle>().Named("Circle"));
            container.Register(ServiceCriteria.For<IShape>().ImplementedBy<Square>().Named("Square"));

            IShape circle = container.Resolve<IShape>("Circle");
            IShape square = container.Resolve<IShape>("Square");

            Assert.NotNull(circle);
            Assert.NotNull(square);

            Assert.IsType<Circle>(circle);
            Assert.IsType<Square>(square);
            Assert.IsAssignableFrom<IShape>(circle);
            Assert.IsAssignableFrom<IShape>(square);
        }

        [Fact]
        public void TestMethod13()
        {
            UniIoC container = new UniIoC();

            container.Register(ServiceCriteria.For<NewInterceptor>());

            container.Register(ServiceCriteria.For<ILoginValidator>().ImplementedBy<EmailLoginValidator>().Named("EmailLoginValidator").LifeCycle(LifeCycleEnum.Singleton));

            container.Register(ServiceCriteria.For<ILoginService>().ImplementedBy<EmailLoginService>().Named("Email").OnInstanceCreating(f => new EmailLoginService(f.Resolve<ILoginValidator>("EmailLoginValidator"))).Interceptor<NewInterceptor>());



            ILoginService emailLoginService = container.Resolve<ILoginService>("Email");

            Assert.NotNull(emailLoginService);

            Assert.False(String.IsNullOrEmpty(emailLoginService.Login("hede", "hode")));
        }

        [Fact]
        public void TestMethod14()
        {
            ILoginService loginServiceProxy = new LoginServiceProxy(new NewInterceptor(), new PhoneLoginService(new PhoneLoginValidator()));

            string sessionKey = loginServiceProxy.Login("", "");
        }

        [Fact]
        public void TestMethod15()
        {
            UniIoC container = new UniIoC();

            container.Register(ServiceCriteria.For<IOrm>().ImplementedBy<Orm>().Named("northwindSqlite").LifeCycle(LifeCycleEnum.Singleton).OnInstanceCreating(f => new Orm()).OnIntercepting(f => { Console.WriteLine(f.Method.Name); }));

            IOrm a1 = container.Resolve<IOrm>("northwindSqlite");

            var a2 = a1.Query<Orm>();
        }

        [Fact]
        public void TestMethod16()
        {
            UniIoC container = new UniIoC();

            container.Register(ServiceCriteria.For<Circle>());
            container.Register(ServiceCriteria.For<Square>());

            IShape circle = container.Resolve<Circle>();
            IShape square = container.Resolve<Square>();

            Assert.NotNull(circle);
            Assert.NotNull(square);

            Assert.IsType<Circle>(circle);
            Assert.IsType<Square>(square);

            Assert.IsAssignableFrom<IShape>(circle);
            Assert.IsAssignableFrom<IShape>(square);
        }

        [Fact]
        public void TestMethod17()
        {
            UniIoC container = new UniIoC();

            container.Register<IClient, SomeClient>();
            container.Register<IService, SomeService>();

            IClient client = container.Resolve<IClient>();

            Assert.NotNull(client);

            Assert.IsType<SomeClient>(client);

            Assert.IsAssignableFrom<IClient>(client);
        }

        [Fact]
        public void TestMethod18()
        {
            UniIoC container = new UniIoC();

            container.Register<IClient, SomeClient>();
            container.Register<IService, SomeService>();
            container.Register<IService, SomeService>(f => f.RegisterBehavior(RegisterBehaviorEnum.Keep));

            IClient client = container.Resolve<IClient>();

            Assert.NotNull(client);

            Assert.IsType<SomeClient>(client);

            Assert.IsAssignableFrom<IClient>(client);

            container.UnRegister<IClient>();

            Assert.Throws<NullReferenceException>(() => container.Resolve<IClient>());
        }
    }
}
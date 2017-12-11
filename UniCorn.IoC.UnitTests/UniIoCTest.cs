using System;
using System.Collections.Generic;
using System.Linq;
using System.Linq.Expressions;
using UniCorn.BaseLibrary;
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

            container.Register<IClient, TestClient>();
            container.Register<IService, TestService>();

            IClient client = container.Resolve<IClient>();

            Assert.NotNull(client);

            Assert.IsType<TestClient>(client);

            Assert.IsAssignableFrom<IClient>(client);
        }

        [Fact]
        public void TestMethod18()
        {
            UniIoC container = new UniIoC();

            container.Register<IClient, TestClient>();
            container.Register<IService, TestService>();
            container.Register<IService, TestService>(f => f.RegisterBehavior(RegisterBehaviorEnum.Keep));

            IClient client = container.Resolve<IClient>();

            Assert.NotNull(client);

            Assert.IsType<TestClient>(client);

            Assert.IsAssignableFrom<IClient>(client);

            container.UnRegister<IClient>();

            Assert.Throws<NullReferenceException>(() => container.Resolve<IClient>());
        }

        [Fact]
        public void TestMethod19()
        {
            IUniIoC container = new UniIoC();

            Func<object[], object> anonymousInstantiatorFunc = typeof(EmailLoginService).CreateAnonymousInstantiator();

            var a1 = anonymousInstantiatorFunc(new object[] { new EmailLoginValidator() });
        }

        [Fact]
        public void TestMethod20()
        {
            IUniIoC container = new UniIoC();

            LambdaExpression anonymousInstantiatorExp = typeof(EmailLoginService).CreateAnonymousInstantiatorLambda();

            var a1 = anonymousInstantiatorExp.Compile().DynamicInvoke(new EmailLoginValidator());
        }

        [Fact]
        public void TestMethod21()
        {
            IUniIoC container = new UniIoC();

            Func<object[], object> anonymousInstantiatorExp1 = container.Resolve<Func<object[], object>>(typeof(EmailLoginService));

            Func<object[], object> anonymousInstantiatorExp2 = container.Resolve<Func<object[], object>>(typeof(EmailLoginService));

            var a1 = anonymousInstantiatorExp1(new object[] { new EmailLoginValidator() });
        }

        [Fact]
        public void TestMethod22()
        {
            IUniIoC container = new UniIoC();

            LambdaExpression anonymousInstantiatorExp = container.Resolve<LambdaExpression>(typeof(EmailLoginService));

            var a1 = anonymousInstantiatorExp.Compile().DynamicInvoke(new EmailLoginValidator());
        }

        class OrmInterceptor : IInterceptor
        {
            public void Intercept(IInvocation invocation)
            {
                if (invocation.Method.Name.ToLower() == "query")
                {
                    invocation.ReturnValue = new List<string> { "Hakan", "Soner" };
                }
                else if (invocation.Method.Name.ToLower() == "gettablecolumns")
                {
                    invocation.ReturnValue = new string[] { "ProductID", "Name", "UnitPrice", "UnitType" };
                }
                else if (invocation.Method.Name.ToLower() == "getdatabasename")
                {
                    invocation.ReturnValue = "Northwind";
                }
                else if (invocation.Method.Name == "DatabaseDuration")
                {
                    invocation.ReturnValue = TimeSpan.Zero;
                }
                else if (invocation.Method.Name == "DbType")
                {
                    invocation.ReturnValue = DatabaseTypee.SqlServer;
                }
            }
        }

        [Fact]
        public void TestMethod23()
        {
            IUniIoC container = new UniIoC();

            container.Register(ServiceCriteria.For<OrmInterceptor>());

            container.Register(ServiceCriteria.For<IOrm>().Interceptor<OrmInterceptor>());

            IOrm ormProxy = container.Resolve<IOrm>();

            IEnumerable<dynamic> result1 = ormProxy.Query("select name from products", new object[] { 123, "Silgi" });

            string[] result2 = ormProxy.GetTableColumns("products");

            string result3 = ormProxy.GetDatabaseName();

            string str1 = result2[0];

            TimeSpan ts1 = ormProxy.DatabaseDuration();

            DatabaseTypee dbType = ormProxy.DbType();
        }

        [Fact]
        public void TestMethod24()
        {
            IUniIoC container = new UniIoC();

            container.Register(ServiceCriteria.For<OrmInterceptor>());

            container.Register(ServiceCriteria.For<Orm>().OnIntercepting(invocation => invocation.Proceed()));

            Orm ormProxy = container.Resolve<Orm>();

            DatabaseTypee dbType = ormProxy.DbType();
        }

        [Fact]
        public void TestMethod25()
        {
            Orm_Proxy ormProxy = new Orm_Proxy(new OrmInterceptor(), null);

            TimeSpan ts1 = ormProxy.DatabaseDuration();

            DatabaseTypee dbType = ormProxy.DbType();
        }

        [Fact]
        public void TestMethod26()
        {
            Orm ormProxy = new Orm_Proxy2(new OrmInterceptor(), new Orm());

            TimeSpan ts1 = ormProxy.DatabaseDuration();

            DatabaseTypee dbType = ormProxy.DbType();
        }

        [Fact]
        public void TestMethod27()
        {
            Type ormProxyType = ProxyTypeUtility.CreateProxyType(typeof(IOrm));
        }

        [Fact]
        public void TestMethod28()
        {
            IUniIoC container = new UniIoC();

            container.Register(ServiceCriteria.For<OrmInterceptor>());

            //container.Register(ServiceCriteria.For<Orm>().Interceptor<OrmInterceptor>());

            container.Register(ServiceCriteria.For<Orm>().OnInstanceCreating(f => new Orm()).OnIntercepting(invocation => invocation.Proceed()));

            IOrm ormProxy = container.Resolve<Orm>();

            IEnumerable<dynamic> result1 = ormProxy.Query("select name from products", new object[] { 123, "Silgi" });

            string[] result2 = ormProxy.GetTableColumns("products");

            if (result2 != null)
            {
                string str1 = result2.FirstOrDefault();
            }

            string result3 = ormProxy.GetDatabaseName();

            TimeSpan ts1 = ormProxy.DatabaseDuration();

            DatabaseTypee dbType = ormProxy.DbType();
        }

        [Fact]
        public void TestMethod29()
        {
            IUniIoC container = new UniIoC();

            container.Register(ServiceCriteria.For<OrmInterceptor>());

            //container.Register(ServiceCriteria.For<Orm>().Interceptor<OrmInterceptor>());

            container.Register(ServiceCriteria.For<Orm>().OnInstanceCreating(f => new Orm()).Interceptor<OrmInterceptor>());

            IOrm ormProxy = container.Resolve<Orm>();

            IEnumerable<dynamic> result1 = ormProxy.Query("select name from products", new object[] { 123, "Silgi" });

            string[] result2 = ormProxy.GetTableColumns("products");

            string result3 = ormProxy.GetDatabaseName();

            string str1 = result2.FirstOrDefault();

            TimeSpan ts1 = ormProxy.DatabaseDuration();

            DatabaseTypee dbType = ormProxy.DbType();
        }

        [Fact]
        public void TestMethod30()
        {
            IOrm ormProxy = ProxyTypeUtility.CreateProxyInstance<IOrm>(new Orm(), new OrmInterceptor());

            TimeSpan ts1 = ormProxy.DatabaseDuration();

            DatabaseTypee dbType = ormProxy.DbType();
        }

        [Fact]
        public void TestMethod31()
        {
            IOrm ormProxy = ProxyTypeUtility.CreateProxyInstance<IOrm>(null, new OrmInterceptor());

            TimeSpan ts1 = ormProxy.DatabaseDuration();

            DatabaseTypee dbType = ormProxy.DbType();
        }

        [Fact]
        public void TestMethod32()
        {
            IOrm ormProxy = ProxyTypeUtility.CreateProxyInstance<Orm>(null, new OrmInterceptor());

            TimeSpan ts1 = ormProxy.DatabaseDuration();

            DatabaseTypee dbType = ormProxy.DbType();
        }

        [Fact]
        public void TestMethod33()
        {
            dynamic ormProxy = ProxyTypeUtility.CreateProxyInstance(typeof(IOrm), new Orm(), new OrmInterceptor());

            TimeSpan ts1 = ormProxy.DatabaseDuration();

            DatabaseTypee dbType = ormProxy.DbType();
        }
    }
}


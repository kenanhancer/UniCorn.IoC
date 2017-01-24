using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.Emit;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Runtime.Loader;
using System.Text;
using UniCorn.Core;
using Xunit;

namespace UniCorn.IoC.UnitTests
{
    public class CompileUnitTests
    {
        [Fact]
        public void CompileCSharpCodeAndSaveToFile()
        {
            string csharpCode = @"
using System;
using System.Collections.Generic;

public static class ConsoleApp1
{
    public static void Main()
    {
        List<string> names = new List<string>();
        Console.WriteLine(""Hello Roslyn."");
    }
}";

            SyntaxTree syntaxTree = CSharpSyntaxTree.ParseText(csharpCode);

            MetadataReference[] references = new MetadataReference[]
                    {
                        MetadataReference.CreateFromFile(typeof(object).GetTypeInfo().Assembly.Location),
                        MetadataReference.CreateFromFile(typeof(List<string>).GetTypeInfo().Assembly.Location)
                    };

            CSharpCompilation compilation = CSharpCompilation.Create("ConsoleApp1")
                                            .WithOptions(new CSharpCompilationOptions(OutputKind.DynamicallyLinkedLibrary))
                                            .AddReferences(references)
                                            .AddSyntaxTrees(syntaxTree);

            string fileName = "ConsoleApp1.dll";

            EmitResult emitResult = compilation.Emit(fileName);

            StringBuilder message = new StringBuilder();
            if (!emitResult.Success)
            {
                IEnumerable<Diagnostic> failures = emitResult.Diagnostics.Where(diagnostic =>
                        diagnostic.IsWarningAsError ||
                        diagnostic.Severity == DiagnosticSeverity.Error);

                foreach (Diagnostic diagnostic in failures)
                {
                    message.AppendFormat("{0}: {1}", diagnostic.Id, diagnostic.GetMessage());
                }
            }
            else
            {
                Assembly ConsoleApp1 = AssemblyLoadContext.Default.LoadFromAssemblyPath(Path.GetFullPath(fileName));

                ConsoleApp1?.GetType("ConsoleApp1")?.GetMethod("Main")?.Invoke(null, null);
            }
        }

        [Fact]
        public void CompileCSharpCodeInMemory1()
        {
            string csharpCode = @"
using System;
using System.Collections.Generic;

public static class ConsoleApp1
{
    public static void Main()
    {
        List<string> names = new List<string>();
        Console.WriteLine(""Hello Roslyn."");
    }
}";

            SyntaxTree syntaxTree = CSharpSyntaxTree.ParseText(csharpCode);

            MetadataReference[] references = new MetadataReference[]
                    {
                        MetadataReference.CreateFromFile(typeof(object).GetTypeInfo().Assembly.Location),
                        MetadataReference.CreateFromFile(typeof(List<string>).GetTypeInfo().Assembly.Location)
                    };

            CSharpCompilation compilation = CSharpCompilation.Create("ConsoleApp1")
                                            .WithOptions(new CSharpCompilationOptions(OutputKind.DynamicallyLinkedLibrary))
                                            .AddReferences(references)
                                            .AddSyntaxTrees(syntaxTree);

            StringBuilder message = new StringBuilder();

            using (var ms = new MemoryStream())
            {
                EmitResult emitResult = compilation.Emit(ms);

                if (!emitResult.Success)
                {
                    IEnumerable<Diagnostic> failures = emitResult.Diagnostics.Where(diagnostic =>
                        diagnostic.IsWarningAsError ||
                        diagnostic.Severity == DiagnosticSeverity.Error);

                    foreach (Diagnostic diagnostic in failures)
                    {
                        message.AppendFormat("{0}: {1}", diagnostic.Id, diagnostic.GetMessage());
                    }
                }
                else
                {
                    ms.Seek(0, SeekOrigin.Begin);

                    Assembly assembly = AssemblyLoadContext.Default.LoadFromStream(ms);
                    Type consoleAppType = assembly.GetType("ConsoleApp1");
                    MethodInfo mainMethod = consoleAppType.GetMethod("Main");

                    mainMethod.Invoke(null, null);
                }
            }
        }

        [Fact]
        public void CompileCSharpCodeInMemory2()
        {
            TypeEntity typeEntity = new TypeEntity { Name = "Person" };

            typeEntity.Fields.Add(new TypeEntity { Name = "PersonKey", DataType = typeof(string) });
            typeEntity.Fields.Add(new TypeEntity { Name = "FirstName", DataType = typeof(string) });
            typeEntity.Fields.Add(new TypeEntity { Name = "LastName", DataType = typeof(string) });
            typeEntity.Fields.Add(new TypeEntity { Name = "Address", DataType = typeof(string) });
            typeEntity.Fields.Add(new TypeEntity { Name = "Phone", DataType = typeof(string) });

            string csharp = UniCornTypeFactory.GeneratePocoCode(typeEntity, true, false);

            CSharpCompilation compilation = CSharpCompilation.Create("Person")
                                            .WithOptions(new CSharpCompilationOptions(OutputKind.DynamicallyLinkedLibrary))
                                            .AddReferences(MetadataReference.CreateFromFile(typeof(object).GetTypeInfo().Assembly.Location))
                                            .AddSyntaxTrees(CSharpSyntaxTree.ParseText(csharp));


            StringBuilder message = new StringBuilder();

            using (var ms = new MemoryStream())
            {
                EmitResult emitResult = compilation.Emit(ms);

                if (!emitResult.Success)
                {
                    IEnumerable<Diagnostic> failures = emitResult.Diagnostics.Where(diagnostic =>
                        diagnostic.IsWarningAsError ||
                        diagnostic.Severity == DiagnosticSeverity.Error);

                    foreach (Diagnostic diagnostic in failures)
                    {
                        message.AppendFormat("{0}: {1}", diagnostic.Id, diagnostic.GetMessage());
                    }
                }
                else
                {
                    ms.Seek(0, SeekOrigin.Begin);

                    Assembly assembly = AssemblyLoadContext.Default.LoadFromStream(ms);
                    Type personType = assembly.GetType("Person");

                    Func<object[], object> instanceDelegate = UniCornTypeFactory.CreateInstanceDelegate(personType);

                    object person1 = instanceDelegate(null);
                }
            }

        }
    }
}
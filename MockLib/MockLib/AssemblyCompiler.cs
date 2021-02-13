using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using System;
using System.Collections;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Reflection;

namespace MockLib
{
	public class AssemblyCompiler
	{
		private readonly IList<string> references;

		public AssemblyCompiler()
		{
			references = new List<string>();
			UseReference<object>();
			UseReference<AssemblyCompiler>();
		}

		public AssemblyCompiler UseReference<T>()
		{
			references.Add(typeof(T).GetTypeInfo().Assembly.Location);
			return this;
		}

		public Assembly Compile(string sourceCode, string assemblyName)
		{
			var tree = CSharpSyntaxTree.ParseText(sourceCode);
			var compilation = CreateCompileOptions(tree, assemblyName);
			return CreateAssembly(compilation);
		}

		private Compilation CreateCompileOptions(SyntaxTree tree, string assemblyName)
		{
			return CSharpCompilation.Create(
				assemblyName,
				new[] { tree },
				references.Distinct().Select(x => MetadataReference.CreateFromFile(x)).ToList(),
				new CSharpCompilationOptions(OutputKind.DynamicallyLinkedLibrary)
			);
		}

		private Assembly CreateAssembly(Compilation compilation)
		{
			var ms = new MemoryStream();
			var result = compilation.Emit(ms);

			if (!result.Success)
			{
				throw new Exception("Compilation failed.");
			}

			return Assembly.Load(ms.ToArray());
		}
	}
}

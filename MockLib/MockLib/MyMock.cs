using Microsoft.CodeAnalysis;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Linq.Expressions;
using System.Reflection;
using System.Text;

namespace MockLib
{
	public class MyMock<TMockable> where TMockable : class
	{
		private readonly Dictionary<string, Func<object>> methodInterceptors = new Dictionary<string, Func<object>>();

		private readonly System.Reflection.TypeInfo type = typeof(TMockable).GetTypeInfo();

		public TMockable Object => (TMockable)Activator.CreateInstance(GenerateType(), methodInterceptors);

		private Type GenerateType()
		{
			var typeToGenerate = typeof(TMockable);
			var sourceCode = new StringBuilder();
			var newTypeName = $"{type.Name}Proxy";
			var typeFullName = GetTypeFullName(typeToGenerate);

			var referenceTypes = GetMockableMethods()
				.Select(x => x.ReturnType)
				.Concat(GetMockableMethods()
					.SelectMany(xx => xx.GetParameters()
					.Select(aa => aa.ParameterType))
				);

			sourceCode.AppendLine("using System;");
			sourceCode.AppendLine("using System.Collections.Generic;");
			sourceCode.AppendLine($"using {typeToGenerate.Namespace};");
			sourceCode.AppendLine($"using {GetType().Namespace};");

			referenceTypes.Select(x => x.Namespace)
				.Distinct()
				.ToList()
				.ForEach(x => sourceCode.AppendLine($"using {x};"));

			sourceCode.AppendLine($"public class {newTypeName} : {typeFullName} {{");

			sourceCode.AppendLine($"private readonly Dictionary<string, Func<object>> methodInterceptors;");

			sourceCode.AppendLine($"public {newTypeName}(Dictionary<string, Func<object>> methodInterceptors) {{ this.methodInterceptors = methodInterceptors; }}");

			sourceCode.AppendLine("private object InterceptMethod(string methodName) { if (!methodInterceptors.ContainsKey(methodName)) throw new NotImplementedException(); return methodInterceptors[methodName](); }");

			foreach(var mockableMethod in GetMockableMethods())
			{
				WriteMethod(mockableMethod, sourceCode);
			}

			sourceCode.AppendLine("}");

			var compiler = new AssemblyCompiler()
				.UseReference<TMockable>();

			var assembly = compiler.Compile(sourceCode.ToString(), newTypeName);

			return assembly.ExportedTypes.First();
		}

		private void WriteMethod(MethodInfo mockableMethod, StringBuilder sourceCode)
		{
			var returnType = mockableMethod.ReturnType;
			var returnTypeName = returnType == typeof(void) ? "void" : returnType.Name;
			var methodName = mockableMethod.Name;
			var typeCode = Type.GetTypeCode(mockableMethod.ReturnType);

			sourceCode.AppendLine($"public {returnTypeName} {methodName} (");
			var i = 0;
			foreach (var parameter in mockableMethod.GetParameters())
			{
				sourceCode.AppendLine($"{((i++ > 0) ? "," : string.Empty)}{parameter.ParameterType.Name} {parameter.Name}");
			}

			sourceCode.AppendLine(") {");
			sourceCode.AppendLine($"var result = InterceptMethod(\"{methodName}\");");

			if (typeCode == TypeCode.Object && returnTypeName != "void")
				sourceCode.AppendLine($"return result as {returnTypeName};");
			else sourceCode.AppendLine($"return ({returnTypeName})result;");

			sourceCode.AppendLine("}");
		}

		private string GetTypeFullName(Type type)
		{
			var nameBuilder = type.Name;
			var current = type;

			while (current.DeclaringType != null)
			{
				nameBuilder = $"{current.DeclaringType.Name}.{nameBuilder}";
				current = current.DeclaringType;
			}

			return nameBuilder;
		}

		private IEnumerable<MethodInfo> GetMockableMethods()
		{
			return type.GetMethods().Where(x => x.IsAbstract || x.IsVirtual);
		}

		public MyMock<TMockable> MockMethod<TResult>(Expression<Func<TMockable, TResult>> methodCall, TResult result)
		{
			var expression = (MethodCallExpression)methodCall.Body;
			var methodName = expression.Method.Name;
			methodInterceptors[methodName] = () => result;

			return this;
		}
	}
}

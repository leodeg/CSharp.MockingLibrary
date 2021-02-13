using MockLib.Tests.Unit;
using System;
using System.Collections.Generic;
using Xunit;

public class ExampleProxy : IExample
{
	private readonly Dictionary<string, Func<object>> methodInterceptors;

	public ExampleProxy(Dictionary<string, Func<object>> methodInterceptors) { this.methodInterceptors = methodInterceptors; }

	public string ExampleMethod() { var result = InterceptMethod("ExampleMethod"); return (string)result; }

	private object InterceptMethod(string methodName) { if (!methodInterceptors.ContainsKey(methodName)) throw new NotImplementedException(); return methodInterceptors[methodName](); }
}

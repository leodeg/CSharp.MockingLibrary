using System;
using Xunit;

namespace MockLib.Tests.Unit
{
	public class MockTests
	{



		[Fact]
		public void Mock_ShouldWork()
		{
			var mockExample = new MyMock<IExample>();

			mockExample.MockMethod(x => x.ExampleMethod(), "hi from mock");
			var example = mockExample.Object;

			Assert.Equal("hi from mock", example.ExampleMethod());
		}
	}
}

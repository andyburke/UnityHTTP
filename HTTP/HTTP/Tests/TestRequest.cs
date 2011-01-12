using System;
using NUnit.Framework;
namespace HTTP
{
	[TestFixture()]
	public class TestRequest
	{
		[Test()]
		public void TestCase ()
		{
			var r = new Request("get", "http://www.google.com/");
			r.Send();
			while(!r.isDone);
			Console.WriteLine(r.bytes.Length.ToString());
		}
	}
}


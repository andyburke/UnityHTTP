using System;
using NUnit.Framework;
using System.Threading;

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
			while(!r.isDone) Thread.Sleep(100);
			Console.WriteLine(r.response.status);
			Console.WriteLine(r.response.Text);
			
			r = new Request("get", "http://www.digitalcoding.com/tools/test-gzip-deflate-compression.html");
			r.Send();
			while(!r.isDone) Thread.Sleep(100);
			Console.WriteLine(r.response.status);
			Console.WriteLine(r.response.Text);
		}
	}
}


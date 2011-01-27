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
			var r = new Request("get", "http://2ge1ffhdgsaom3padg8i2jd5pke1ajbt-a-fc-opensocial.googleusercontent.com/gadgets/ifr?url=http://gadgetsforblogger.googlecode.com/files/recent-comments-gadget.xml&container=peoplesense&parent=http://entitycrisis.blogspot.com/&mid=0&view=profile&libs=google.blog&d=0.555.7&lang=en&country=US&view-params=%7B%22summaryLength%22:%22200%22,%22numberOfPosts%22:%2210%22,%22skin%22:%7B%22FACE_SIZE%22:%2232%22,%22HEIGHT%22:%22200%22,%22TITLE%22:%22::+comments%22,%22BORDER_COLOR%22:%22transparent%22,%22ENDCAP_BG_COLOR%22:%22transparent%22,%22ENDCAP_TEXT_COLOR%22:%22%23333333%22,%22ENDCAP_LINK_COLOR%22:%22%23d52a33%22,%22ALTERNATE_BG_COLOR%22:%22transparent%22,%22CONTENT_BG_COLOR%22:%22transparent%22,%22CONTENT_LINK_COLOR%22:%22%23d52a33%22,%22CONTENT_TEXT_COLOR%22:%22%23333333%22,%22CONTENT_SECONDARY_LINK_COLOR%22:%22%23d52a33%22,%22CONTENT_SECONDARY_TEXT_COLOR%22:%22%23666666%22,%22CONTENT_HEADLINE_COLOR%22:%22%23000000%22,%22FONT_FACE%22:%22normal+normal+13px+Arial,+Tahoma,+Helvetica,+FreeSans,+sans-serif%22%7D%7D&up_summaryLength=200&up_numberOfPosts=10&communityId=04501167318190226925&caller=http://entitycrisis.blogspot.com/");
			r.Send();
			while(!r.isDone) Thread.Sleep(100);
			Console.WriteLine(r.response.status);
			Console.WriteLine(r.response.Text);
			
			r = new Request("get", "http://entitycrisis.blogspot.com/", true);
			r.Send();
			while(!r.isDone) Thread.Sleep(100);
			Console.WriteLine(r.response.status);
			Console.WriteLine(r.response.Text);
			
			r = new Request("get", "http://entitycrisis.blogspot.com/", true);
			r.Send();
			while(!r.isDone) Thread.Sleep(100);
			Console.WriteLine(r.response.status);
			Console.WriteLine(r.response.Text);

		}
	}
}


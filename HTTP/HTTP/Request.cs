using System;
using System.IO;
using System.Text;
using System.Collections.Generic;
using System.Net.Sockets;
using System.Globalization;
using System.Threading;


namespace HTTP
{
	public class HTTPException : Exception
	{
		public HTTPException (string message) : base(message)
		{
		}
	}

	public class Request
	{
		public string method = "GET";
		public string protocol = "HTTP/1.1";
		public byte[] bytes;
		public Uri uri;
		public static byte[] EOL = { (byte)'\r', (byte)'\n' };
		public Response response = null;
		public bool isDone = false;
		public int maximumRetryCount = 8;
		public bool acceptGzip = true;
		public bool useCache = false;
		public Exception exception = null;

		Dictionary<string, List<string>> headers = new Dictionary<string, List<string>> ();
		static Dictionary<string, string> etags = new Dictionary<string, string> ();

		public Request (string method, string uri)
		{
			this.method = method;
			this.uri = new Uri (uri);
		}

		public Request (string method, string uri, bool useCache)
		{
			this.method = method;
			this.uri = new Uri (uri);
			this.useCache = useCache;
		}

		public Request (string method, string uri, byte[] bytes)
		{
			this.method = method;
			this.uri = new Uri (uri);
			this.bytes = bytes;
		}

		public void AddHeader (string name, string value)
		{
			name = name.ToLower ().Trim ();
			value = value.Trim ();
			if (!headers.ContainsKey (name))
				headers[name] = new List<string> ();
			headers[name].Add (value);
		}

		public string GetHeader (string name)
		{
			name = name.ToLower ().Trim ();
			if (!headers.ContainsKey (name))
				return "";
			return headers[name][0];
		}

		public List<string> GetHeaders (string name)
		{
			name = name.ToLower ().Trim ();
			if (!headers.ContainsKey (name))
				headers[name] = new List<string> ();
			return headers[name];
		}

		public void SetHeader (string name, string value)
		{
			name = name.ToLower ().Trim ();
			value = value.Trim ();
			if (!headers.ContainsKey (name))
				headers[name] = new List<string> ();
			headers[name].Clear ();
			headers[name].Add (value);
		}

		public void Send ()
		{
			isDone = false;
			if (acceptGzip)
				SetHeader ("Accept-Encoding", "gzip");
			ThreadPool.QueueUserWorkItem (new WaitCallback (delegate(Object state) {
				try {
					var retry = 0;
					while (++retry < maximumRetryCount) {
						if (useCache) {
							string etag = "";
							if (etags.TryGetValue (uri.AbsoluteUri, out etag)) {
								SetHeader ("If-None-Match", etag);
							}
						}
						SetHeader ("Host", uri.Host);
						var client = new TcpClient ();
						client.Connect (uri.Host, uri.Port);
						using (var stream = client.GetStream ()) {
							WriteToStream (stream);
							response = new Response (stream);
						}
						client.Close ();
						switch (response.status) {
						case 307:
						case 302:
						case 301:
							uri = new Uri (response.GetHeader ("Location"));
							continue;
						default:
							retry = maximumRetryCount;
							break;
						}
					}
					if (useCache) {
						string etag = response.GetHeader ("etag");
						if (etag.Length > 0)
							etags[uri.AbsoluteUri] = etag;
					}
					
				} catch (Exception e) {
					Console.WriteLine ("Unhandled Exception, aborting request.");
					Console.WriteLine (e);
					exception = e;
					response = null;
				}
				isDone = true;
			}));
		}

		public string Text {
			set { bytes = System.Text.Encoding.UTF8.GetBytes (value); }
		}

		void WriteToStream (Stream outputStream)
		{
			var stream = new BinaryWriter (outputStream);
			stream.Write (ASCIIEncoding.ASCII.GetBytes (method.ToUpper () + " " + uri.PathAndQuery + " " + protocol));
			stream.Write (EOL);
			foreach (string name in headers.Keys) {
				foreach (string value in headers[name]) {
					stream.Write (ASCIIEncoding.ASCII.GetBytes (name));
					stream.Write (':');
					stream.Write (ASCIIEncoding.ASCII.GetBytes (value));
					stream.Write (EOL);
				}
			}
			if (bytes != null && bytes.Length > 0) {
				if (GetHeader ("Content-Length") == "") {
					stream.Write (ASCIIEncoding.ASCII.GetBytes ("content-length: " + bytes.Length.ToString ()));
					stream.Write (EOL);
					stream.Write (EOL);
				}
				stream.Write (bytes);
			} else {
				stream.Write (EOL);
			}
		}
		
	}
	
}


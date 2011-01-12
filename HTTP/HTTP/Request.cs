using System;
using System.IO;
using System.Text;
using System.Collections.Generic;
using System.Net.Sockets;
using System.Globalization;


namespace HTTP
{
	public class HTTPException : Exception {
		public HTTPException(string message) : base(message) {
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
		
		Dictionary<string, List<string>> headers = new Dictionary<string, List<string>>();
		
		public Request(string method, string uri) {
			this.method = method;
			this.uri = new Uri(uri);
		}
		
		public Request(string method, string uri, byte[] bytes) {
			this.method = method;
			this.uri = new Uri(uri);
			this.bytes = bytes;
		}
		
		public void AddHeader(string name, string value) {
			name = name.ToLower().Trim();
			value = value.Trim();
			if(!headers.ContainsKey(name)) headers[name] = new List<string>();
			headers[name].Add(value);
		}
		
		public void Send() {
			Console.WriteLine(uri.Port);
			isDone = false;
			var client = new TcpClient();
			client.BeginConnect(uri.Host, uri.Port, delegate(IAsyncResult result) {
				client.EndConnect(result);
				Console.WriteLine("Connected.");
				using(var stream = client.GetStream()) {
					WriteToStream(stream);
					response = new Response(stream);
				}
				client.Close();
				isDone = true;
				
			}, null);
		}
		
		
		void WriteToStream (Stream outputStream)
		{
			var stream = new BinaryWriter(outputStream);
			
			stream.Write (ASCIIEncoding.ASCII.GetBytes (method.ToUpper() + " " + uri.PathAndQuery + " " + protocol));
			stream.Write (EOL);
			foreach (string name in headers.Keys) {
				foreach(string value in headers[name]) {
					stream.Write (ASCIIEncoding.ASCII.GetBytes (name));
					stream.Write (':');
					stream.Write (ASCIIEncoding.ASCII.GetBytes (value));
					stream.Write (EOL);
				}
			}
			if (bytes != null && bytes.Length > 0) {
				if(!headers.ContainsKey("content-length")) {
					stream.Write(ASCIIEncoding.ASCII.GetBytes("content-length: " + bytes.Length.ToString()));
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


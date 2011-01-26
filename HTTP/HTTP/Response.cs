using System;
using System.Collections.Generic;
using System.IO;
using System.Text;
using System.Globalization;


namespace HTTP
{
	public class Response
	{
		public int status = 200;
		public string message = "OK";
		public byte[] bytes;
		
		Dictionary<string, List<string>> headers = new Dictionary<string, List<string>>();
		
		public string Text {
			get {
				if(bytes == null) return "";
				return System.Text.UTF8Encoding.UTF8.GetString(bytes);	
			}
		}
		
		void AddHeader(string name, string value) {
			name = name.ToLower().Trim();
			value = value.Trim();
			if(!headers.ContainsKey(name)) headers[name] = new List<string>();
			headers[name].Add(value);
		}
		
		public List<string> GetHeaders(string name) {
			name = name.ToLower().Trim();
			if(!headers.ContainsKey(name)) headers[name] = new List<string>();
			return headers[name];
		}
		
		public string GetHeader(string name) {
			name = name.ToLower().Trim();
			if(!headers.ContainsKey(name)) return string.Empty;
			return headers[name][headers[name].Count-1];
		}
		
		public Response (Stream stream)
		{
			ReadFromStream(stream);
		}
		
		string ReadLine (BinaryReader stream)
		{
			var line = new List<byte> ();
			while (true) {
				byte c = stream.ReadByte ();
				if (c == Request.EOL[1]) break;
				line.Add (c);
			}
			var s = ASCIIEncoding.ASCII.GetString (line.ToArray ()).Trim ();
			return s;
		}

		string[] ReadKeyValue (BinaryReader stream)
		{
			string line = ReadLine (stream);
			if (line == "")
				return null;
			else {
				var split = line.IndexOf(':');
				if(split == -1) return null;
				var parts = new string[2];
				parts[0] = line.Substring(0,split).Trim();
				parts[1] = line.Substring(split+1).Trim();
				return parts;
			}
			
		}
				
		void ReadFromStream (Stream inputStream)
		{
			
			var stream = new BinaryReader(inputStream);
			var top = ReadLine (stream).Split(new char[] {' '});
			
			if(!int.TryParse(top[1], out status))
				throw new HTTPException("Bad Status Code");
			
			message = string.Join(" ", top, 2, top.Length-2);
			headers.Clear ();
			
			while (true) {
				// Collect Headers
				string[] parts = ReadKeyValue (stream);
				if (parts == null) break;
				AddHeader(parts[0], parts[1]);
			}
			
			if (GetHeader("transfer-encoding") == "chunked") {
				var receivedBytes = new List<byte>();
				while (true) {
					// Collect Body
					string hexLength = ReadLine (stream);
					//Console.WriteLine("HexLength:" + hexLength);
					if(hexLength.Length == 0) break;
					int length = int.Parse (hexLength, NumberStyles.AllowHexSpecifier);
					if (length == 0) break;
					receivedBytes.AddRange (stream.ReadBytes (length));
				}
				bytes = receivedBytes.ToArray();
				while (true) {
					//Collect Trailers
					string[] parts = ReadKeyValue (stream);
					if (parts == null) break;
					AddHeader(parts[0], parts[1]);
				}
				
			} else {
				// Read Body
				int contentLength = 0;
				
				try {
					contentLength = int.Parse(GetHeader("content-length"));
				} catch {
					throw new HTTPException("Bad Content Length.");	
				}
				bytes = stream.ReadBytes (contentLength);
				
			}
			
		}
		
	}
}


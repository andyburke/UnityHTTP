using UnityEngine;
using System;
using System.IO;
using System.Text;
using System.Collections;
using System.Collections.Generic;
using System.Net.Sockets;
using System.Globalization;
using System.Threading;
using System.Net.Security;
using System.Security.Authentication;
using System.Security.Cryptography.X509Certificates;

namespace HTTP
{
	public class HTTPException : Exception
	{
		public HTTPException (string message) : base(message)
		{
		}
	}

	public enum RequestState {
		Waiting, Reading, Done
	}

	public class Request
	{
        public static bool LogAllRequests = false;
        public static bool VerboseLogging = false;

		public CookieJar cookieJar = CookieJar.Instance;
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
		public RequestState state = RequestState.Waiting;
        public long responseTime = 0; // in milliseconds
		public bool synchronous = false;

		public Action< HTTP.Request > completedCallback = null;

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

        public Request( string method, string uri, WWWForm form )
        {
			this.method = method;
			this.uri = new Uri (uri);
			this.bytes = form.data;
            foreach ( DictionaryEntry entry in form.headers )
            {
                this.AddHeader( (string)entry.Key, (string)entry.Value );
            }
        }

        public Request( string method, string uri, Hashtable data )
        {
            this.method = method;
            this.uri = new Uri( uri );
            this.bytes = Encoding.UTF8.GetBytes( JSON.JsonEncode( data ) );
            this.AddHeader( "Content-Type", "application/json" );
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

        public List< string > GetHeaders()
        {
            List< string > result = new List< string >();
            foreach (string name in headers.Keys) {
				foreach (string value in headers[name]) {
                    result.Add( name + ": " + value );
				}
			}

            return result;
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
			headers[name].TrimExcess();
			headers[name].Add (value);
		}

		private void GetResponse() {
            System.Diagnostics.Stopwatch curcall = new System.Diagnostics.Stopwatch();
	        curcall.Start();
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
						var ostream = stream as Stream;
						if (uri.Scheme.ToLower() == "https") {
							ostream = new SslStream (stream, false, new RemoteCertificateValidationCallback (ValidateServerCertificate));
							try {
								var ssl = ostream as SslStream;
								ssl.AuthenticateAsClient (uri.Host);
							} catch (Exception e) {
								Debug.LogError ("Exception: " + e.Message);
								return;
							}
						}
						WriteToStream (ostream);
						response = new Response ();
						response.request = this;
						state = RequestState.Reading;
						response.ReadFromStream(ostream);
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
			state = RequestState.Done;
			isDone = true;
            responseTime = curcall.ElapsedMilliseconds;

            if ( completedCallback != null )
            {
				if (synchronous) {
					completedCallback(this);
				} else {
                    // we have to use this dispatcher to avoid executing the callback inside this worker thread
	                ResponseCallbackDispatcher.Singleton.requests.Enqueue( this );
				}
            }

            if ( LogAllRequests )
            {
#if !UNITY_EDITOR
				System.Console.WriteLine("NET: " + InfoString( VerboseLogging ));
#else
                if ( response != null && response.status >= 200 && response.status < 300 )
                {
                    Debug.Log( InfoString( VerboseLogging ) );
                }
                else if ( response != null && response.status >= 400 )
                {
                    Debug.LogError( InfoString( VerboseLogging ) );
                }
                else
                {
                    Debug.LogWarning( InfoString( VerboseLogging ) );
                }
#endif
            }			
		}
		
		public virtual void Send( Action< HTTP.Request > callback = null)
		{
			
            if (!synchronous && callback != null && ResponseCallbackDispatcher.Singleton == null )
            {
                ResponseCallbackDispatcher.Init();
            }

            completedCallback = callback;

			isDone = false;
			state = RequestState.Waiting;
			if (acceptGzip)
				SetHeader ("Accept-Encoding", "gzip");

			if ( this.cookieJar != null )
			{
				List< Cookie > cookies = this.cookieJar.GetCookies( new CookieAccessInfo( uri.Host, uri.AbsolutePath ) );
								string cookieString = this.GetHeader( "cookie" );
				for ( int cookieIndex = 0; cookieIndex < cookies.Count; ++cookieIndex )
				{
					if ( cookieString.Length > 0 && cookieString[ cookieString.Length - 1 ] != ';' )
					{
						cookieString += ';';
					}
					cookieString += cookies[ cookieIndex ].name + '=' + cookies[ cookieIndex ].value + ';';
				}
		        SetHeader( "cookie", cookieString );
		    }

			if ( bytes != null && bytes.Length > 0 && GetHeader ("Content-Length") == "" ) {
                SetHeader( "Content-Length", bytes.Length.ToString() );
            }

            if ( GetHeader( "User-Agent" ) == "" ) {
                SetHeader( "User-Agent", "UnityWeb 1.0 ( Unity " + Application.unityVersion + " ) ( " + SystemInfo.operatingSystem + " )" );
            }

            if ( GetHeader( "Connection" ) == "" ) {
                SetHeader( "Connection", "close" );
            }
			
			// Basic Authorization
			if (!String.IsNullOrEmpty(uri.UserInfo)) {	
				SetHeader("Authorization", "Basic " + System.Convert.ToBase64String(System.Text.ASCIIEncoding.ASCII.GetBytes(uri.UserInfo)));
			}
			
			if (synchronous) {
				GetResponse();
			} else {
				ThreadPool.QueueUserWorkItem (new WaitCallback ( delegate(object t) {
					GetResponse();
				})); 
			}
		}

		public string Text {
			set { bytes = System.Text.Encoding.UTF8.GetBytes (value); }
		}


		public static bool ValidateServerCertificate (object sender, X509Certificate certificate, X509Chain chain, SslPolicyErrors sslPolicyErrors)
		{
#if !UNITY_EDITOR
            System.Console.WriteLine( "NET: SSL Cert: " + sslPolicyErrors.ToString() );
#else
			Debug.LogWarning("SSL Cert Error: " + sslPolicyErrors.ToString ());
#endif
			return true;
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

            stream.Write (EOL);

			if (bytes != null && bytes.Length > 0) {
				stream.Write (bytes);
			}
		}

        private static string[] sizes = { "B", "KB", "MB", "GB" };
        public string InfoString( bool verbose )
        {
            string status = isDone && response != null ? response.status.ToString() : "---";
            string message = isDone && response != null ? response.message : "Unknown";
            double size = isDone && response != null && response.bytes != null ? response.bytes.Length : 0.0f;

            int order = 0;
            while ( size >= 1024.0f && order + 1 < sizes.Length )
            {
                ++order;
                size /= 1024.0f;
            }

            string sizeString = String.Format( "{0:0.##}{1}", size, sizes[ order ] );

            string result = uri.ToString() + " [ " + method.ToUpper() + " ] [ " + status + " " + message + " ] [ " + sizeString + " ] [ " + responseTime + "ms ]";

            if ( verbose && response != null )
            {
                result += "\n\nRequest Headers:\n\n" + String.Join( "\n", GetHeaders().ToArray() );
                result += "\n\nResponse Headers:\n\n" + String.Join( "\n", response.GetHeaders().ToArray() );

                if ( response.Text != null )
                {
                    result += "\n\nResponse Body:\n" + response.Text;
                }
            }

            return result;
        }
	}
}

using UnityEngine;
using System.Collections;
using System.IO;
using System;
using HTTP;

namespace HTTP
{
	public class DiskCacheOperation
	{
		public bool isDone = false;
		public bool fromCache = false;
		public Request request = null;
	}

#if UNITY_WEBPLAYER
	public class DiskCache : MonoBehaviour
	{
		static DiskCache _instance = null;
		public static DiskCache Instance {
			get {
				if (_instance == null) {
					var g = new GameObject ("DiskCache", typeof(DiskCache));
					g.hideFlags = HideFlags.HideAndDontSave;
					_instance = g.GetComponent<DiskCache> ();
				}
				return _instance;
			}
		}

		public DiskCacheOperation Fetch (Request request)
		{
			var handle = new DiskCacheOperation ();
			handle.request = request;
			StartCoroutine (Download (request, handle));
			return handle;
		}

		IEnumerator Download(Request request, DiskCacheOperation handle)
		{
			request.Send ();
			while (!request.isDone)
				yield return new WaitForEndOfFrame ();
			handle.isDone = true;
		}
	}
#else
	public class DiskCache : MonoBehaviour
	{
		string cachePath = null;

		static DiskCache _instance = null;
		public static DiskCache Instance {
			get {
				if (_instance == null) {
					var g = new GameObject ("DiskCache", typeof(DiskCache));
					g.hideFlags = HideFlags.HideAndDontSave;
					_instance = g.GetComponent<DiskCache> ();
				}
				return _instance;
			}
		}

		void Awake ()
		{
			cachePath = System.IO.Path.Combine (Environment.GetFolderPath (Environment.SpecialFolder.LocalApplicationData), "uwcache");
			if (!Directory.Exists (cachePath))
				Directory.CreateDirectory (cachePath);
		}

		public DiskCacheOperation Fetch (Request request)
		{
			var guid = "";
			foreach (var b in System.Security.Cryptography.MD5.Create ().ComputeHash (System.Text.ASCIIEncoding.ASCII.GetBytes (request.uri.ToString ()))) {
				guid = guid + b.ToString ("X2");
			}

			var filename = System.IO.Path.Combine (cachePath, guid);
			if (File.Exists (filename) && File.Exists (filename + ".etag"))
				request.SetHeader ("If-None-Match", File.ReadAllText (filename + ".etag"));
			var handle = new DiskCacheOperation ();
			handle.request = request;
			StartCoroutine (DownloadAndSave (request, filename, handle));
			return handle;
		}

		IEnumerator DownloadAndSave (Request request, string filename, DiskCacheOperation handle)
		{
			var useCachedVersion = File.Exists(filename);
            Action< HTTP.Request > callback = request.completedCallback;
			request.Send(); // will clear the completedCallback
			while (!request.isDone)
				yield return new WaitForEndOfFrame ();
			if (request.exception == null && request.response != null) {
				if (request.response.status == 200) {
					var etag = request.response.GetHeader ("etag");
					if (etag != "") {
						File.WriteAllBytes (filename, request.response.bytes);
						File.WriteAllText (filename + ".etag", etag);
					}
					useCachedVersion = false;
				}
			}

			if(useCachedVersion) {
				if(request.exception != null) {
					Debug.LogWarning("Using cached version due to exception:" + request.exception);
					request.exception = null;
				}
				request.response.status = 304;
				request.response.bytes = File.ReadAllBytes (filename);
				request.isDone = true;
			}
			handle.isDone = true;

            if ( callback != null )
            {
                callback( request );
            }
		}

	}
#endif
}

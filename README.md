# Attribution

Based on Simon Wittber's UnityWeb code (http://code.google.com/p/unityweb/).

# About

This is a TcpClient-based HTTP library for use in Unity.  It should work in
both the standalone player and in the web player.

It also has convenience methods for working with JSON.
# Examples

IEnumerator example:

```C#
public IEnumerator SomeRoutine() {
    HTTP.Request someRequest = new HTTP.Request( "get", "http://someurl.com/somewhere" );
    someRequest.Send();

    while( !someRequest.isDone )
    {
        yield return null;
    }

    // parse some JSON, for example:
    JSONObject thing = new JSONObject( request.response.Text );
}
```

Closure-style (does not need to be in a coroutine):

```C#
HTTP.Request someRequest = new HTTP.Request( "get", "http://someurl.com/somewhere" );
someRequest.Send( ( request ) => {
    // parse some JSON, for example:
    JSONObject thing = new JSONObject( request.response.Text );
});
```

Post request using form data:

```C#
WWWForm form = new WWWForm();
form.AddField( "something", "yo" );
form.AddField( "otherthing", "hey" );

HTTP.Request someRequest = new HTTP.Request( "post", "http://someurl.com/some/post/handler", form );
someRequest.Send( ( request ) => {
    // parse some JSON, for example:
    bool result = false;
    Hashtable thing = (Hashtable)JSON.JsonDecode( request.response.Text, ref result );
    if ( !result )
    {
        Debug.LogWarning( "Could not parse JSON response!" );
        return;
    }
});
```

Post request using JSON:

```C#
Hashtable data = new Hashtable();
data.Add( "something", "hey!" );
data.Add( "otherthing", "YO!!!!" );

// When you pass a Hashtable as the third argument, we assume you want it send as JSON-encoded
// data.  We'll encode it to JSON for you and set the Content-Type header to application/json
HTTP.Request theRequest = new HTTP.Request( "post", "http://someurl.com/a/json/post/handler", data );
theRequest.Send( ( request ) => {

    // we provide Object and Array convenience methods that attempt to parse the response as JSON
    // if the response cannot be parsed, we will return null
    // note that if you want to send json that isn't either an object ({...}) or an array ([...])
    // that you should use JSON.JsonDecode directly on the response.Text, Object and Array are
    // only provided for convenience
    Hashtable result = request.response.Object;
    if ( result == null )
    {
        Debug.LogWarning( "Could not parse JSON response!" );
        return;
    }
  
});
```

If you want to make a request while not in Play Mode (e. g. from a custom Editor menu command or wizard), you must use the Request synchronously, since Unity's main update loop is not running. The call will block until the response is available.

```C#
Hashtable data = new Hashtable();
data.Add( "something", "hey!" );
data.Add( "otherthing", "YO!!!!" );

HTTP.Request theRequest = new HTTP.Request("post", "http://someurl.com/a/json/post/handler", data );
theRequest.synchronous = true;
theRequest.Send((request) => {
	EditorUtility.DisplayDialog("Request was posted.", request.response.Text, "Ok");
});
```


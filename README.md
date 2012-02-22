# Attribution

Based on Simon Wittber's UnityWeb code (http://code.google.com/p/unityweb/).

# About

This is a TcpClient-based HTTP library for use in Unity.  It should work in
both the standalone player and in the web player.

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

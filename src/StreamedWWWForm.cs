using System;
using System.IO;
using System.Text;
using System.Collections;

namespace HTTP
{
    public class StreamedWWWForm {
        string boundary;
        public FormDataStream stream;

        public Hashtable headers {
            get {
                return new Hashtable {
                    { "Content-Type", "multipart/form-data; boundary=\"" + boundary + "\""}
                };
            }
        }

        public StreamedWWWForm(){
            byte[] bytes = new byte[40];
            var random = new Random();
            for (int i=0; i<40; i++){
                bytes[i] = (byte)(48 + random.Next(62));
                if (bytes[i] > 57){
                    bytes[i] += 7;
                }
                if (bytes[i] > 90){
                    bytes[i] += 6;
                }
            }
            boundary = Encoding.ASCII.GetString(bytes);
            stream = new FormDataStream(boundary);
        }
        
        public void AddField(string fieldName, string fieldValue){
            var contentStream = new MemoryStream(Encoding.UTF8.GetBytes(fieldValue));
            stream.AddPart(fieldName, "text/plain; charset=\"utf-8\"", contentStream);

        }
        public void AddBinaryData(string fieldName, byte[] contents=null, string mimeType = null){
            var contentStream = new MemoryStream(contents);
            if (mimeType == null){
                mimeType = "application/octet-stream";
            }
            stream.AddPart(fieldName, mimeType, contentStream, fieldName + ".dat");
        }
        public void AddBinaryData(string fieldName, Stream contents=null, string mimeType = null){
            if (mimeType == null){
                mimeType = "application/octet-stream";
            }
            stream.AddPart(fieldName, mimeType, contents, fieldName + ".dat");
        }
        public void AddFile(string fieldName, string path, string mimeType=null){
            if (mimeType == null){
                mimeType = "application/octet-stream";
            }
            var contents = new FileInfo(path).Open(FileMode.Open);
            stream.AddPart(fieldName, mimeType, contents, fieldName + ".dat");
        }
    }
}

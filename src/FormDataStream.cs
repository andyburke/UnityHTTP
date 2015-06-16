using System;
using System.IO;
using System.Text;
using System.Collections.Generic;
namespace HTTP {

    public class FormPart {
        byte[] header;
        Stream contents;
        int position = 0;

        public FormPart(string fieldName, string mimeType, string boundary, Stream contents, string fileName=null){
            string filenameheader = "";
            if (fileName != null){
                filenameheader = "; filename=\"" + fileName +"\"";
            }
            header =  Encoding.ASCII.GetBytes(
                "\r\n--" + boundary + "\r\n" +
                "Content-Type: " + mimeType + "\r\n" +
                "Content-disposition: form-data; name=\"" + fieldName + "\"" + filenameheader + "\r\n\r\n"
            );
            this.contents = contents;
        }
        public long Length {
            get {
                return header.Length + contents.Length;
            }
        }
        public int Read(byte[] buffer, int offset, int size){
            int writed = 0;
            int bytesToWrite;
            if (position < header.Length){
                bytesToWrite =  (int)(header.Length - position) > size ? size : (int)(header.Length - position); 
                Array.Copy (
                    header,     // from header
                    position,   // started from position
                    buffer,     // to buffer
                    offset,     // started with offset
                    bytesToWrite
               );
               writed += bytesToWrite;
               position += bytesToWrite;
            }
            if (writed >= size){
                return writed;
            }
            bytesToWrite = contents.Read(buffer, writed + offset, size - writed);
            writed += bytesToWrite;
            position += bytesToWrite;
            return writed;
        }

        public void Dispose(){
            header = null;
            contents.Close();
        }
    }
    
    public class FormDataStream: Stream {
        long position = 0;
        List<FormPart> parts = new List<FormPart>();
        bool dirty = false;
        byte[] footer;
        string boundary;

        public FormDataStream(string boundary){
            this.boundary = boundary;
            footer = Encoding.ASCII.GetBytes("\r\n--" + boundary + "--\r\n");
        }

        public override bool CanRead { get { return true; } }
        public override bool CanSeek { get { return false; } }
        public override bool CanTimeout { get { return false; } }
        public override bool CanWrite { get { return false; } }
        public override int ReadTimeout { get { return 0; } set { } }
        public override int WriteTimeout { get { return 0; } set { } }
        public override long Position {
            get {
                return position;
            }
            set {
                throw new NotImplementedException("FormDataStream is non-seekable stream");
            }
        }
        public override long Length {
            get {
                if (parts.Count == 0){
                    return 0;
                }
                dirty = true;
                long len = 0;
                foreach (var part in parts){
                    len += part.Length;
                }
                return len + footer.Length;
            }
        }

        public override void Flush(){
            throw new NotImplementedException("FormDataStream is readonly stream");
        }

        public override int Read(byte[] buffer, int offset, int count){
            dirty = true;
            int writed = 0;
            int bytesToWrite = 0;

            // write parts
            long partsSize = 0;
            foreach (var part in parts){
                partsSize += part.Length;
                if (position > partsSize){
                    continue;
                }
                bytesToWrite = part.Read(buffer, writed + offset, count - writed);
                writed += bytesToWrite;
                position += bytesToWrite;
                if (writed >= count){
                    return count;
                }
            }


            // write footer
            bytesToWrite =  count - writed > footer.Length?  footer.Length : count - writed;
            Array.Copy (footer, 0, buffer, writed + offset, bytesToWrite);
            position += bytesToWrite;
            writed += bytesToWrite;
            return writed;
        }

        public override long Seek(long amount, SeekOrigin origin){
            throw new NotImplementedException("FormDataStream is non-seekable stream");
        }
        
        public override void SetLength (long len){
            throw new NotImplementedException("FormDataStream is readonly stream");
        }
        
        public override void Write(byte[] source, int offset, int count){
            throw new NotImplementedException("FormDataStream is readonly stream");
        }

        public void AddPart(string fieldName, string mimeType, Stream contents, string fileName=null){
            if (dirty){
                throw new InvalidOperationException("You can't change form data, form already readed");
            }
            parts.Add(new FormPart(fieldName, mimeType, boundary, contents, fileName));
        }

        public void AddPart(FormPart part){
            if (dirty){
                throw new InvalidOperationException("You can't change form data, form already readed");
            }
            parts.Add(part);
        }
        
        public override void Close(){
            foreach (var part in parts){
                part.Dispose();
            }
            base.Close();
        }
    }

}



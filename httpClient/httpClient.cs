using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace mySpace.httpClient
{
    public class httpCookie
    {
        public httpCookie(string name, string value, string path = "")
        {
            this.name = name;
            this.value = value;
            this.path = path;
        }
        public string name { get; set; }
        public string value { get; set; }
        public string domain { get; set; }
        public string path { get; set; }
    }
    public class httpFile
    {
        public httpFile(string name, string sourcePath = null, string fileName = null, string contentType = null)
        {
            this.name = name;
            if (!string.IsNullOrEmpty(sourcePath))
                this.read(sourcePath);
            this.contentType = (string.IsNullOrEmpty(contentType)) ? "application/octet-stream" : contentType;
        }
        public httpFile read(string sourcePath)
        {
            try
            {
                this.sourcePath = sourcePath;
                this.contentBytes = System.IO.File.ReadAllBytes(sourcePath);
                // this.fileName = System.IO.Path.GetFileNameWithoutExtension(sourcePath);
                this.fileName = System.IO.Path.GetFileName(sourcePath);
            }
            catch { }
            return this;
        }
        public string sourcePath { get; set; }
        public string fileName { get; set; }
        public string name { get; set; }
        public string contentType { get; set; }
        public byte[] contentBytes { get; set; }
    }
    public class httpResponse
    {
        public httpResponse()
        {
        }
        public int statusCode { get; set; }
        public byte[] bytes { get; set; }
        public string content { get { return System.Text.Encoding.Default.GetString(this.bytes); } }
    }
    public class httpRequest
    {
        public httpRequest()
        {
            System.Net.ServicePointManager.ServerCertificateValidationCallback = delegate { return true; }; // Will allow self-signed certificates.
            System.Net.ServicePointManager.Expect100Continue = false;
            uploadFiles = new List<httpFile> { };
            cookies = new List<httpCookie> { };
            formFields = new System.Collections.Specialized.NameValueCollection() { };
            headers = new System.Collections.Specialized.NameValueCollection() { };
            encoding = "UTF8";
        }
        public string method { get; set; }
        public string contentType { get; set; }
        /// <summary>Encoding i.e. "ISO-8859-1" (default UTF8)</summary>
        public string encoding { get { return _encoding; } set { _encoding = (value.ToUpper() == "UTF8" || value.ToUpper() == "UTF-8") ? "UTF8" : value; } }
        private string _encoding;
        public string URI { get; set; }
        public string username { get; set; }
        public string password { get; set; }
        public List<httpFile> uploadFiles { get; set; }
        public List<httpCookie> cookies { get; set; }
        public System.Collections.Specialized.NameValueCollection formFields { get; set; }
        public System.Collections.Specialized.NameValueCollection headers { get; set; }
        public byte[] requestBytes { get { return _requestBytes; } set { _requestBytes = value; _content = (_encoding == "UTF8") ? Encoding.UTF8.GetString(value) : Encoding.GetEncoding(_encoding).GetString(value); } }
        private byte[] _requestBytes;
        public string content
        {
            get { return _content; }
            set
            {
                _content = value;
                if (value == null)
                {
                    _requestBytes = null;
                }
                else
                {
                    try { _requestBytes = (string.IsNullOrEmpty(encoding) || encoding.ToUpper() == "UTF8") ? System.Text.Encoding.UTF8.GetBytes(value) : Encoding.Convert(Encoding.UTF8, Encoding.GetEncoding(this.encoding), Encoding.UTF8.GetBytes(value)); }
                    catch { _requestBytes = System.Text.Encoding.UTF8.GetBytes(value); }
                }
            }
        }
        private string _content;
        public int contentLength { get; set; }
        // Methods
        public httpResponse post()
        {
            // this.reqObj = (System.Net.HttpWebRequest)System.Net.WebRequest.Create(this.URI);
            this.reqObj = System.Net.WebRequest.Create(this.URI);

            if (!string.IsNullOrEmpty(this.username) && !string.IsNullOrEmpty(this.password))
            {
                this.reqObj.Credentials = httpCredential(this.URI, this.username, this.password);
                this.reqObj.PreAuthenticate = true;
            }
            this.reqObj.Proxy = null;
            // Add cookies
            if (this.cookies != null)
            {
                System.Net.HttpWebRequest httpRequest = this.reqObj as System.Net.HttpWebRequest;
                if (httpRequest.CookieContainer == null) { httpRequest.CookieContainer = new System.Net.CookieContainer(); }
                for (int i = 0; i < this.cookies.Count; i++)
                {
                    httpRequest.CookieContainer.Add(new System.Net.Cookie(this.cookies[i].name, this.cookies[i].value, this.cookies[i].path, new Uri(this.URI).Host));
                }
            }
            // Add headers            
            if (this.headers != null)
            {
                System.Net.HttpWebRequest httpRequest = this.reqObj as System.Net.HttpWebRequest;
                // foreach (string key in this.headers.Keys) { this.reqObj.Headers.Add(key, this.headers[key]); }
                foreach (string key in this.headers.Keys)
                {
                    if (key == "Accept") { httpRequest.Accept = this.headers[key]; }
                    else { httpRequest.Headers.Add(key, this.headers[key]); }
                }
            }
            if ((uploadFiles != null && uploadFiles.Count > 0) || this.contentType == "multipart/form-data")
            {
                string boundary = "----------------------------" + DateTime.Now.Ticks.ToString("x");
                this.reqObj.ContentType = "multipart/form-data; boundary=" + boundary;
                this.reqObj.Method = "POST";
                this.requestBytes = new byte[0];
                var boundarybytes = System.Text.Encoding.ASCII.GetBytes("\r\n--" + boundary + "\r\n");
                var endBoundaryBytes = System.Text.Encoding.ASCII.GetBytes("\r\n--" + boundary + "--");
                string formdataTemplate = "\r\n--" + boundary + "\r\nContent-Disposition: form-data; name=\"{0}\";\r\n\r\n{1}";
                if (this.formFields != null)
                {
                    foreach (string key in this.formFields.Keys)
                    {
                        string formitem = string.Format(formdataTemplate, key, this.formFields[key]);
                        byte[] formitembytes = System.Text.Encoding.UTF8.GetBytes(formitem);
                        try { if (!string.IsNullOrEmpty(this.encoding) && this.encoding.ToUpper() != "UTF8") { formitembytes = Encoding.Convert(Encoding.UTF8, Encoding.GetEncoding(this.encoding), formitembytes); } } catch { }
                        this.requestBytes = ByteArrayCombine(this.requestBytes, formitembytes);
                    }
                }
                for (int i = 0; i < this.uploadFiles.Count; i++)
                {
                    string headerTemplate = "Content-Disposition: form-data; name=\"{0}\"; filename=\"{1}\"\r\n" + "Content-Type: " + this.uploadFiles[i].contentType + "\r\n\r\n";
                    this.requestBytes = ByteArrayCombine(this.requestBytes, boundarybytes);
                    var header = string.Format(headerTemplate, this.uploadFiles[i].name, this.uploadFiles[i].fileName);
                    byte[] headerbytes = System.Text.Encoding.UTF8.GetBytes(header);
                    try { if (!string.IsNullOrEmpty(this.encoding) && this.encoding.ToUpper() != "UTF8") { headerbytes = Encoding.Convert(Encoding.UTF8, Encoding.GetEncoding(this.encoding), headerbytes); } } catch { }
                    this.requestBytes = ByteArrayCombine(this.requestBytes, headerbytes);
                    byte[] thisFileBytes = this.uploadFiles[i].contentBytes;
                    this.requestBytes = ByteArrayCombine(this.requestBytes, thisFileBytes);
                }
                this.requestBytes = ByteArrayCombine(this.requestBytes, endBoundaryBytes);
                this.reqObj.ContentLength = this.requestBytes.Length;
                this.stream = this.reqObj.GetRequestStream();
                this.stream.Write(this.requestBytes, 0, this.requestBytes.Length);
                this.stream.Close();
            }
            else if (this.formFields != null && this.formFields.Count > 0)
            {
                this.contentType = "application/x-www-form-urlencoded";
                this.reqObj.ContentType = this.contentType;
                this.reqObj.Method = "POST";
                foreach (string key in this.formFields.Keys)
                {
                    string formitem = ((this.requestBytes != null && this.requestBytes.Length > 0) ? "&" : "") +
                        System.Net.WebUtility.UrlEncode(key) + "=" + System.Net.WebUtility.UrlEncode(this.formFields[key]);
                    byte[] formitembytes = System.Text.Encoding.UTF8.GetBytes(formitem);
                    try { if (!string.IsNullOrEmpty(this.encoding) && this.encoding.ToUpper() != "UTF8") { formitembytes = Encoding.Convert(Encoding.UTF8, Encoding.GetEncoding(this.encoding), formitembytes); } } catch { }
                    this.requestBytes = ByteArrayCombine(this.requestBytes, formitembytes);
                }
                this.stream = this.reqObj.GetRequestStream();
                this.stream.Write(this.requestBytes, 0, this.requestBytes.Length);
                this.stream.Close();
            }
            else
            {
                this.reqObj.ContentType = this.contentType;
                this.reqObj.Method = "POST";
                System.Net.HttpWebRequest httpRequest = this.reqObj as System.Net.HttpWebRequest;
                // httpRequest.Accept = "application/json";
                this.stream = this.reqObj.GetRequestStream();
                if (this.requestBytes != null && this.requestBytes.Length > 0)
                {
                    this.stream.Write(this.requestBytes, 0, this.requestBytes.Length);
                }
                this.stream.Close();

            }
            httpResponse responseObj = new httpResponse();
            try
            {
                System.Net.WebResponse resp = this.reqObj.GetResponse();
                System.Net.HttpWebResponse httpResponse = (System.Net.HttpWebResponse)resp;
                responseObj.statusCode = (int)httpResponse.StatusCode;
                if (resp == null) return null;
                using (var memoryStream = new System.IO.MemoryStream())
                {
                    resp.GetResponseStream().CopyTo(memoryStream);
                    responseObj.bytes = memoryStream.ToArray();
                }
            }
            catch (System.Net.WebException e)
            {
                System.Net.WebResponse resp = e.Response;
                System.Net.HttpWebResponse httpResponse = (System.Net.HttpWebResponse)resp;
                if (httpResponse == null) return null;
                responseObj.statusCode = (int)httpResponse.StatusCode;
                using (var memoryStream = new System.IO.MemoryStream())
                {
                    resp.GetResponseStream().CopyTo(memoryStream);
                    responseObj.bytes = memoryStream.ToArray();
                }
            }
            return responseObj;
        }
        public httpResponse get()
        {
            if (this.formFields != null)
            {
                string queryString = "";
                foreach (string key in this.formFields.Keys)
                {
                    string formitem = ((queryString.Length > 0) ? "&" : "") +
                        System.Net.WebUtility.UrlEncode(key) + "=" + System.Net.WebUtility.UrlEncode(this.formFields[key]);
                    try
                    {
                        if (!string.IsNullOrEmpty(this.encoding) && this.encoding.ToUpper() != "UTF8")
                        {
                            byte[] nameBytes = Encoding.Convert(Encoding.UTF8, Encoding.GetEncoding(this.encoding), Encoding.UTF8.GetBytes(key));
                            byte[] valueBytes = Encoding.Convert(Encoding.UTF8, Encoding.GetEncoding(this.encoding), Encoding.UTF8.GetBytes(this.formFields[key]));
                            formitem = ((queryString.Length > 0) ? "&" : "") +
                                System.Text.Encoding.Default.GetString(System.Net.WebUtility.UrlEncodeToBytes(nameBytes, 0, nameBytes.Length)) + "=" + System.Text.Encoding.Default.GetString(System.Net.WebUtility.UrlEncodeToBytes(valueBytes, 0, valueBytes.Length));
                        }
                    }
                    catch { }
                    queryString += formitem;
                }
                this.reqObj = System.Net.WebRequest.Create(this.URI + (this.URI.Contains("?") ? "&" : "?") + queryString);
            }
            else
            {
                this.reqObj = System.Net.WebRequest.Create(this.URI);
            }
            if (!string.IsNullOrEmpty(this.username) && !string.IsNullOrEmpty(this.password))
            {
                this.reqObj.Credentials = httpCredential(this.URI, this.username, this.password);
                this.reqObj.PreAuthenticate = true;
            }
            this.reqObj.Proxy = null;
            // Add cookies
            if (this.cookies != null)
            {
                System.Net.HttpWebRequest httpRequest = this.reqObj as System.Net.HttpWebRequest;
                if (httpRequest.CookieContainer == null) { httpRequest.CookieContainer = new System.Net.CookieContainer(); }
                for (int i = 0; i < this.cookies.Count; i++)
                {
                    httpRequest.CookieContainer.Add(new System.Net.Cookie(this.cookies[i].name, this.cookies[i].value, this.cookies[i].path, new Uri(this.URI).Host));
                }
            }
            // Add headers            
            if (this.headers != null)
            {
                System.Net.HttpWebRequest httpRequest = this.reqObj as System.Net.HttpWebRequest;
                // foreach (string key in this.headers.Keys) { this.reqObj.Headers.Add(key, this.headers[key]); }
                foreach (string key in this.headers.Keys)
                {
                    if (key == "Accept") { httpRequest.Accept = this.headers[key]; }
                    else { httpRequest.Headers.Add(key, this.headers[key]); }
                }
            }
            this.reqObj.ContentType = null;
            this.reqObj.Method = "GET";
            httpResponse responseObj = new httpResponse();
            try
            {
                System.Net.WebResponse resp = this.reqObj.GetResponse();
                System.Net.HttpWebResponse httpResponse = (System.Net.HttpWebResponse)resp;
                responseObj.statusCode = (int)httpResponse.StatusCode;
                if (resp == null) return null;
                using (var memoryStream = new System.IO.MemoryStream())
                {
                    resp.GetResponseStream().CopyTo(memoryStream);
                    responseObj.bytes = memoryStream.ToArray();
                }
            }
            catch (System.Net.WebException e)
            {
                System.Net.WebResponse resp = e.Response;
                System.Net.HttpWebResponse httpResponse = (System.Net.HttpWebResponse)resp;
                if (httpResponse == null) return null;
                responseObj.statusCode = (int)httpResponse.StatusCode;
                using (var memoryStream = new System.IO.MemoryStream())
                {
                    resp.GetResponseStream().CopyTo(memoryStream);
                    responseObj.bytes = memoryStream.ToArray();
                }
            }
            return responseObj;
        }
        // Internal objects
        internal System.Net.CredentialCache httpCredential(string URI, string username, string password)
        {
            // System.Net.ServicePointManager.SecurityProtocol = System.Net.SecurityProtocolType.Ssl3;
            System.Net.CredentialCache credentialCache = new System.Net.CredentialCache();
            credentialCache.Add(new System.Uri(URI), "Basic", new System.Net.NetworkCredential(username, password));
            return credentialCache;
        }
        internal System.Net.WebRequest reqObj { get; set; }
        internal System.IO.Stream stream { get; set; }
        // Helper functions
        private byte[] ByteArrayCombine(params byte[][] arrays)
        {
            int len = 0;
            foreach (byte[] array in arrays) { if (array != null) len += array.Length; }
            // byte[] rv = new byte[arrays.Sum(a => a.Length)];
            byte[] rv = new byte[len];
            int offset = 0;
            foreach (byte[] array in arrays)
            {
                if (array != null)
                {
                    System.Buffer.BlockCopy(array, 0, rv, offset, array.Length);
                    offset += array.Length;
                }
            }
            return rv;
        }
    }

}

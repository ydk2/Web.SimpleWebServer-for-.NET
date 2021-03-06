/*
 * Created on Tue Jan 30 2018
 *
 * The MIT License (MIT)
 * Copyright (c) 2018 ydk2
 *
 * Permission is hereby granted, free of charge, to any person obtaining a copy of this software
 * and associated documentation files (the "Software"), to deal in the Software without restriction,
 * including without limitation the rights to use, copy, modify, merge, publish, distribute, sublicense,
 * and/or sell copies of the Software, and to permit persons to whom the Software is furnished to do so,
 * subject to the following conditions:
 *
 * The above copyright notice and this permission notice shall be included in all copies or substantial
 * portions of the Software.
 *
 * THE SOFTWARE IS PROVIDED "AS IS", WITHOUT WARRANTY OF ANY KIND, EXPRESS OR IMPLIED, INCLUDING BUT NOT LIMITED
 * TO THE WARRANTIES OF MERCHANTABILITY, FITNESS FOR A PARTICULAR PURPOSE AND NONINFRINGEMENT. IN NO EVENT SHALL
 * THE AUTHORS OR COPYRIGHT HOLDERS BE LIABLE FOR ANY CLAIM, DAMAGES OR OTHER LIABILITY, WHETHER IN AN ACTION OF CONTRACT,
 * TORT OR OTHERWISE, ARISING FROM, OUT OF OR IN CONNECTION WITH THE SOFTWARE OR THE USE OR OTHER DEALINGS IN THE SOFTWARE.
 */

using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Net;
using System.Net.Sockets;
using System.Text;
using System.Threading;

namespace Web
{
    public class CustomException : Exception
    {
        public int HResult;
        public CustomException(string message, int hresult)
            : base(message)
        {
            this.HResult = hresult;
        }
    }
    public class SimpleHTTPServer
    {
        private readonly string[] _indexFiles = {
            "index.html",
            "index.htm",
            "index.xml",
            "default.html",
            "default.htm",
            "default.xml"
        };

        private static IDictionary<string, string> _mimeTypeMappings = new Dictionary<string, string>(StringComparer.InvariantCultureIgnoreCase) {
            #region extension to MIME type list
            { ".asf", "video/x-ms-asf" }, { ".asx", "video/x-ms-asf" }, { ".avi", "video/x-msvideo" }, { ".bin", "application/octet-stream" }, { ".cco", "application/x-cocoa" }, { ".crt", "application/x-x509-ca-cert" }, { ".css", "text/css" }, { ".deb", "application/octet-stream" }, { ".der", "application/x-x509-ca-cert" }, { ".dll", "application/octet-stream" }, { ".dmg", "application/octet-stream" }, { ".ear", "application/java-archive" }, { ".eot", "application/octet-stream" }, { ".exe", "application/octet-stream" }, { ".flv", "video/x-flv" }, { ".gif", "image/gif" }, { ".hqx", "application/mac-binhex40" }, { ".htc", "text/x-component" }, { ".htm", "text/html" }, { ".html", "text/html" }, { ".ico", "image/x-icon" }, { ".img", "application/octet-stream" }, { ".iso", "application/octet-stream" }, { ".jar", "application/java-archive" }, { ".jardiff", "application/x-java-archive-diff" }, { ".jng", "image/x-jng" }, { ".jnlp", "application/x-java-jnlp-file" }, { ".jpeg", "image/jpeg" }, { ".jpg", "image/jpeg" }, { ".js", "application/x-javascript" }, { ".mml", "text/mathml" }, { ".mng", "video/x-mng" }, { ".mov", "video/quicktime" }, { ".mp3", "audio/mpeg" }, { ".mpeg", "video/mpeg" }, { ".mpg", "video/mpeg" }, { ".msi", "application/octet-stream" }, { ".msm", "application/octet-stream" }, { ".msp", "application/octet-stream" }, { ".pdb", "application/x-pilot" }, { ".pdf", "application/pdf" }, { ".pem", "application/x-x509-ca-cert" }, { ".pl", "application/x-perl" }, { ".pm", "application/x-perl" }, { ".png", "image/png" }, { ".prc", "application/x-pilot" }, { ".ra", "audio/x-realaudio" }, { ".rar", "application/x-rar-compressed" }, { ".rpm", "application/x-redhat-package-manager" }, { ".rss", "text/xml" }, { ".run", "application/x-makeself" }, { ".sea", "application/x-sea" }, { ".shtml", "text/html" }, { ".sit", "application/x-stuffit" }, { ".swf", "application/x-shockwave-flash" }, { ".tcl", "application/x-tcl" }, { ".tk", "application/x-tcl" }, { ".txt", "text/plain" }, { ".war", "application/java-archive" }, { ".wbmp", "image/vnd.wap.wbmp" }, { ".wmv", "video/x-ms-wmv" }, { ".xml", "text/xml" }, { ".xsl", "text/xsl" }, { ".xpi", "application/x-xpinstall" }, { ".zip", "application/zip" }, { ".ogg", "audio/ogg" }, { ".opus", "audio/ogg" }, { ".oga", "audio/ogg" }, { ".ogv", "video/ogg" }
            #endregion
        };
        private Thread _serverThread;
        private string _rootDirectory;
        private HttpListener _listener;
        private string _host;
        private int _port;

        public int Port
        {
            get { return _port; }
            private set { }
        }

        /// <summary>
        /// Construct server with given port.
        /// </summary>
        /// <param name="path">Directory path to serve.</param>
        /// <param name="port">Port of the server.</param>
        public SimpleHTTPServer(string path, string host, int port)
        {
            this.Start(path, host, port);
        }

        /// <summary>
        /// Construct server with suitable port.
        /// </summary>
        /// <param name="path">Directory path to serve.</param>
        public SimpleHTTPServer(string path, string host)
        {
            //get an empty port
            TcpListener l = new TcpListener(IPAddress.Loopback, 0);
            l.Start();
            int port = ((IPEndPoint)l.LocalEndpoint).Port;
            l.Stop();
            this.Start(path, host, port);
        }

        /// <summary>
        /// Stop server and dispose all functions.
        /// </summary>
        public void Stop()
        {
            _serverThread.Abort();
            _listener.Stop();

            Console.WriteLine("SimpleHTTPServer stoped now");
        }

        private void Listen()
        {
            _listener = new HttpListener();
            _listener.Prefixes.Add(_host + ":" + _port.ToString() + "/");
            _listener.Start();
            while (true)
            {
                try
                {
                    HttpListenerContext context = _listener.GetContext();
                    Process(context);
                }
                catch (CustomException ex)
                {
                    int code = ex.HResult;
                    Console.WriteLine("Throw error : " + code);
                }
            }
        }

        private void Process(HttpListenerContext context)
        {
            string filename = context.Request.Url.AbsolutePath;

            filename = filename.Substring(1);

            if (string.IsNullOrEmpty(filename))
            {
                foreach (string indexFile in _indexFiles)
                {
                    if (File.Exists(Path.Combine(_rootDirectory, indexFile)))
                    {
                        filename = indexFile;
                        break;
                    }
                }
            }

            string filepath = Path.Combine(_rootDirectory, filename);

            this.ReadFile(filepath, ref context);

            context.Response.OutputStream.Close();
        }

        protected void ReadFile(string filepath, ref HttpListenerContext context)
        {
            if (File.Exists(filepath))
            {
                try
                {
                    Stream input = new FileStream(filepath, FileMode.Open);

                    //Adding permanent http response headers
                    string mime;
                    context.Response.ContentType = _mimeTypeMappings.TryGetValue(Path.GetExtension(filepath), out mime) ? mime : "application/octet-stream";
                    context.Response.ContentLength64 = input.Length;

                    context.Response.StatusCode = (int)HttpStatusCode.OK;
                    context.Response.AddHeader("Date", DateTime.Now.ToString("r"));
                    context.Response.AddHeader("Last-Modified", System.IO.File.GetLastWriteTime(filepath).ToString("r"));

                    if (context.Request.HttpMethod != "HEAD")
                    {
                        byte[] buffer = new byte[1024 * 16];
                        int nbytes;
                        while ((nbytes = input.Read(buffer, 0, buffer.Length)) > 0)
                        {
                            context.Response.OutputStream.Write(buffer, 0, nbytes);
                        }
                    }

                    Console.WriteLine(context.Response.StatusCode + " : " + context.Request.Url);

                    input.Close();
                    context.Response.OutputStream.Flush();

                }
                catch (Exception ex)
                {
                    //int errorCode = (int) HttpStatusCode.InternalServerError;
                    this.throwError(ref context, 418);
                }

            }
            else
            {
                int errorCode = (int)HttpStatusCode.NotFound;
                this.throwError(ref context, errorCode);
            }
        }
        private void throwError(ref HttpListenerContext context, int errorCode)
        {
            context.Response.StatusCode = errorCode;
            if (context.Request.HttpMethod != "HEAD")
            {

                string responseString = "";

                string path = Path.Combine(_rootDirectory, context.Response.StatusCode + ".html");
                if (File.Exists(path))
                {
                    responseString = File.ReadAllText(path);
                }
                else
                {
                    string epath = Path.Combine(_rootDirectory, "..");
                    epath = Path.Combine(epath, "error.html");

                    Console.WriteLine(epath);
                    if (File.Exists(epath))
                    {
                        responseString = File.ReadAllText(epath);
                    }
                }
                byte[] buffer = System.Text.Encoding.UTF8.GetBytes(responseString);
                // Get a response stream and write the response to it.
                context.Response.ContentLength64 = buffer.Length;
                System.IO.Stream output = context.Response.OutputStream;
                output.Write(buffer, 0, buffer.Length);
                output.Close();
            }
            Console.WriteLine(context.Response.StatusCode + " : " + context.Request.Url);
        }
        public void Start(string path, string host, int port)
        {
            this._rootDirectory = path;
            this._host = host;
            this._port = port;
            _serverThread = new Thread(this.Listen);
            _serverThread.IsBackground = true;
            _serverThread.Start();

            Console.WriteLine("SimpleWebserver running Ctrl+c to stop it.");
        }

    }
}
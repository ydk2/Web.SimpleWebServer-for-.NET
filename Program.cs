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
using System.IO;
using System.Linq;
using System.Net;
using System.Net.Sockets;
using System.Runtime.CompilerServices;
using System.Text;
using System.Threading;
using Newtonsoft.Json.Linq;
//using Web.SimpleHTTPServer;

namespace Web {
     public class Program {
        static void Main (string[] args) {

            string root = AppDomain.CurrentDomain.BaseDirectory;
            string text = System.IO.File.ReadAllText (root + "config.json");
            JObject stuff = JObject.Parse (text);
            int port = Int32.Parse ((string) stuff["port"]);
            string www = (string) stuff["www"];
            string host = (string) stuff["host"];

            SimpleHTTPServer myServer = new SimpleHTTPServer (root + www, host, port);

            var line = Console.ReadLine ();
            if (string.IsNullOrEmpty (line)) {
                myServer.Stop ();
            }
        }

    }
}
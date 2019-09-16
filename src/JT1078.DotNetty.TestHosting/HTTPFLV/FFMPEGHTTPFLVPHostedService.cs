﻿using DotNetty.Buffers;
using DotNetty.Codecs.Http.WebSockets;
using JT1078.DotNetty.Core.Session;
using Microsoft.Extensions.Hosting;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using JT1078.Protocol;
using System.Collections.Concurrent;
using JT1078.Protocol.Enums;
using System.Diagnostics;
using System.IO.Pipes;
using Newtonsoft.Json;

namespace JT1078.DotNetty.TestHosting
{
    class FFMPEGHTTPFLVPHostedService : BackgroundService
    {
        private readonly Process process;
        private readonly NamedPipeServerStream pipeServerOut;
        private const string PipeNameOut = "demo1serverout";
        public FFMPEGHTTPFLVPHostedService()
        {
            pipeServerOut = new NamedPipeServerStream(PipeNameOut, PipeDirection.In, 1, PipeTransmissionMode.Byte, PipeOptions.Asynchronous,10240,10240);
            process = new Process
            {
                StartInfo =
                {
                    FileName = @"C:\ffmpeg\bin\ffmpeg.exe",
                    Arguments = $@"-f dshow -i video={HardwareCamera.CameraName} -c copy -f flv -vcodec h264 -y \\.\pipe\{PipeNameOut}",
                    UseShellExecute = false,
                    CreateNoWindow = true,
                    RedirectStandardError = true
                }
            };
        }


        public override void Dispose()
        {
            try
            {
                process.Close();
                pipeServerOut.Flush();
            }
            catch
            {

            }
            process.Dispose();
            pipeServerOut.Dispose();
            base.Dispose();
        }

        protected override Task ExecuteAsync(CancellationToken stoppingToken)
        {
            process.Start();
            Task.Run(() =>
            {
                while (true)
                {
                    try
                    {
                        Console.WriteLine("IsConnected>>>" + pipeServerOut.IsConnected);
                        if (pipeServerOut.IsConnected)
                        {
                            if (pipeServerOut.CanRead)
                            {
                                Span<byte> v1 = new byte[2048];
                                var length = pipeServerOut.Read(v1);
                                var realValue = v1.Slice(0, length).ToArray();
                                if (realValue.Length <= 0) continue;
                                Console.WriteLine(JsonConvert.SerializeObject(realValue)+"-"+ length.ToString());
                            }
                        }
                        else
                        {
                            if (!pipeServerOut.IsConnected)
                            {
                                Console.WriteLine("WaitForConnection Star...");
                                pipeServerOut.WaitForConnectionAsync();
                                Console.WriteLine("WaitForConnection End...");
                            }
                        }
                    }
                    catch (Exception ex)
                    {
                        Console.WriteLine(ex);
                    }
                }
            });
            return Task.CompletedTask;
        }
    }
}

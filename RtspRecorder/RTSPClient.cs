using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Net.Sockets;
using System.Net;
using System.Text.RegularExpressions;

namespace RtspRecorder
{
    internal class RTSPClient
    {
        private string url;
        private string hostname;
        private int port;
        private int seq = 2;
        private bool stopFlag = false;
        private int recCount = 0;
        private long streamSize = 0L;
        public bool Detail { get; set; } = false;
        public bool StdOut { get; set; } = false;
        public string Program { get; set; }
        public string OutName { get; set; }
        public int RecDurLimit { get; set; } = 0;
        private TcpClient client;

        private RTSPClient()
        {

        }

        public RTSPClient(string url)
        {
            this.url = url;
        }

        public static string GetIP(string domain)
        {
            IPHostEntry hostEntry = Dns.GetHostEntry(domain);
            IPEndPoint ipEndPoint = new IPEndPoint(hostEntry.AddressList[0], 0);
            return ipEndPoint.Address.ToString();

        }

        private void LogReq(object text)
        {
            if (!Detail) return;
            Console.ForegroundColor = ConsoleColor.DarkGray;
            Console.Write(text);
            Console.ResetColor();
            Console.WriteLine();
        }

        private void LogResp(object text)
        {
            if (!Detail) return;
            Console.ForegroundColor = ConsoleColor.DarkCyan;
            Console.Write(text);
            Console.ResetColor();
            Console.WriteLine();
        }

        public void Connect()
        {
            if (client != null && client.Connected) 
                client.Close();
            Console.Write("Connecting...");
            seq = 2;
            var uri = new Uri(this.url);
            hostname = uri.Host;
            port = uri.Port == -1 ? 554 : uri.Port;
            client = new TcpClient(hostname, port);
            Console.WriteLine("Connected.");
        }

        private void StartTimer()
        {
            var timer = new System.Timers.Timer()
            {
                Interval = 1000,
                AutoReset = true
            };
            timer.Elapsed += delegate
            {
                if (streamSize == 0)
                    return;

                recCount++;
                if (recCount > RecDurLimit && RecDurLimit > 0) 
                {
                    timer.Stop();
                    stopFlag = true;
                }
                if (!StdOut)
                    Console.Write("\rReceiving... [" + Util.FormatTime(recCount) + $"] [{Util.FormatFileSize(streamSize)}]" + "");
                //Console.SetCursorPosition(0, Console.GetCursorPosition().Item2);
            };
            timer.Start();
        }

        /**
         * 中国联通家庭宽带多媒体应用平台技术规范-与机顶盒终端接口分册
         * #Section 8.3.1
         */
        public void DoWork()
        {
        REWORK:
            //1.OPTIONS 忽略

            //2.DESCRIBE
            var msg2 = RTSPMessage.GetDescribeMessage(url, seq);
            LogReq(msg2);
            client.SendString(msg2);
            this.seq++;
            var resp2str = client.ReceieveHead();
            LogResp(resp2str);
            if (Regex.IsMatch(resp2str, "Location: (.*)"))
            {
                this.url = Regex.Match(resp2str, "Location: (.*)").Groups[1].Value.Trim();
                Connect();
                goto REWORK;
            }
            if (Regex.IsMatch(resp2str, "Content-Base: (.*)"))
            {
                this.url = Regex.Match(resp2str, "Content-Base: (.*)").Groups[1].Value.Trim();
            }


            //3.SETUP
            var msg3 = RTSPMessage.GetSetupMessage(url, seq);
            LogReq(msg3);
            client.SendString(msg3);
            this.seq++;
            LogResp(client.ReceieveHead());


            //4.PLAY
            var msg4 = RTSPMessage.GetPlayMessage(url, seq);
            LogReq(msg4);
            client.SendString(msg4);
            this.seq++;
            LogResp(client.ReceieveHead());

            //5.接收数据并写入文件
            if (client.Available == 0)
            {
                Console.WriteLine("无数据!!");
                return;
            }
            if (OutName == "auto")
            {
                OutName = $"{Util.GetValidFileName(Program)}_{DateTime.Now.ToString("yyyy-MM-dd_HH-mm-ss-fff")}.ts";
            }
            else if (!StdOut) 
            {
                var fullPath = Path.GetFullPath(OutName);
                var dir = Path.GetDirectoryName(fullPath);
                var name = Util.GetValidFileName(Path.GetFileName(fullPath));
                if (!Directory.Exists(dir)) Directory.CreateDirectory(dir);
                OutName = Path.Combine(dir, name);
            }
            Console.WriteLine($"Output... {OutName}");

            StartTimer();
            
            var stream = client.GetStream();
            var reader = new BinaryReader(stream);
            using var output = StdOut ? Console.OpenStandardOutput() : new FileStream(OutName, FileMode.Create);
            using var bufferWriter = new BufferedStream(output, 2048);
            /**           ___________________
             * TCP Header | 4 Bits          | TS Data (7*188)
             *            | $ | id | Length |
             *            -------------------
             */
            while (true && !stopFlag) 
            {
                if (reader.ReadByte() == 0x24) //遇到4字节头
                {
                    int interleaveId = reader.ReadByte();
                    if (interleaveId == 0) 
                    {
                        var arr = reader.ReadBytes(2);
                        if (BitConverter.IsLittleEndian)
                            Array.Reverse(arr);
                        int length = BitConverter.ToInt16(arr, 0);
                        var data = reader.ReadBytes(length); //TS data
                        bufferWriter.Write(data);
                        streamSize += data.Length;
                    }
                }
            }
        }

        public void Close()
        {
            if (client.Connected)
            {
                //5.TEARDOWN
                var msg5 = RTSPMessage.GetTearDownMessage(url, seq);
                LogReq(msg5);
                client.SendString(msg5);
                client.Close();
            }
        }
    }
}

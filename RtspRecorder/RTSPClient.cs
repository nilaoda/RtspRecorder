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
        private long streamSizeLast = 0L;
        public bool Detail { get; set; } = false;
        public bool StdOut { get; set; } = false;
        public string Program { get; set; }
        public string OutName { get; set; }
        public int RecDurLimit { get; set; } = 0;
        private TcpClient client;
        private string title = "";
        private string programId = "";
        private string serviceProvider = "";
        private string serviceName = "";

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

        private void LogTsInfo(object text)
        {
            Console.ForegroundColor = ConsoleColor.Cyan;
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
                if (RecDurLimit > 0 && recCount > RecDurLimit) 
                {
                    timer.Stop();
                    stopFlag = true;
                }
                var size = Util.FormatFileSize(streamSize);
                if (streamSize != streamSizeLast)
                {
                    if (!StdOut) Console.Write("\rReceiving... " + (string.IsNullOrEmpty(this.title) ? "" : $"[{this.title}] ") + "[" + Util.FormatTime(recCount) + $"] [{size}]" + "".PadRight(6));
                    streamSizeLast = streamSize;
                }
                else
                {
                    //无数据，断开连接
                    Console.WriteLine();
                    Close();
                }
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
            if (Regex.IsMatch(resp2str, "s=(.*)"))
            {
                this.title = Regex.Match(resp2str, "s=(.*)").Groups[1].Value.Trim();
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
            var infoBuffer = new List<byte>(188 * 5000); //5000个分包中解析信息，没有就算了
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
                        int length = BitConverter.ToUInt16(arr, 0);
                        var data = reader.ReadBytes(length); //TS data
                        if (infoBuffer.Count < 188 * 5000)
                        {
                            infoBuffer.AddRange(data);
                        }
                        else if (this.programId == "") 
                        {
                            this.ParseTsInfo(infoBuffer.ToArray()); //Parse Info
                            var info = $"Program {this.programId}"
                                + ((!string.IsNullOrEmpty(this.serviceName) || !string.IsNullOrEmpty(this.serviceProvider)) ? "\r\n  Metadata:" : "")
                                + (!string.IsNullOrEmpty(this.serviceName) ? $"\r\n    service_name    :  {this.serviceName}" : "")
                                + (!string.IsNullOrEmpty(this.serviceProvider) ? $"\r\n    service_provider:  {this.serviceProvider}" : "");
                            if (this.programId != "") LogTsInfo(info);
                            else infoBuffer.Clear();
                        }
                        bufferWriter.Write(data);
                        streamSize += data.Length;
                    }
                }
            }
            bufferWriter.Flush();
        }

        /// <summary>
        /// Parsing SDT and PAT
        //  Reference: https://en.wikipedia.org/wiki/MPEG_transport_stream
        //             https://en.wikipedia.org/wiki/Service_Description_Table
        //             https://www.etsi.org/deliver/etsi_en/300400_300499/300468/01.03.01_60/en_300468v010301p.pdf
        /// </summary>
        private void ParseTsInfo(byte[] data)
        {
            UInt16 ConvertToUint16(IEnumerable<byte> bytes)
            {
                if (BitConverter.IsLittleEndian)
                    bytes = bytes.Reverse();
                return BitConverter.ToUInt16(bytes.ToArray());
            }

            for (int i = 0; i < data.Length; i++)
            {
                if (data[i] == 0x47 && (i + 188) < data.Length && data[i + 188] == 0x47)
                {
                    var tsData = data.Skip(i).Take(188);
                    var tsHeaderInt = BitConverter.ToUInt32(BitConverter.IsLittleEndian ? tsData.Take(4).Reverse().ToArray() : tsData.Take(4).ToArray(), 0);
                    var pid = (tsHeaderInt & 0x1fff00) >> 8;
                    var tsPayload = tsData.Skip(4);
                    //PAT
                    if (pid == 0x0000)
                    {
                        this.programId = ConvertToUint16(tsPayload.Skip(9).Take(2)).ToString();
                    }
                    //SDT, BAT, ST
                    else if (pid == 0x0011)
                    {
                        var tableId = (int)tsPayload.Skip(1).First();
                        //Current TS Info
                        if (tableId == 0x42)
                        {
                            var sectionLength = ConvertToUint16(tsPayload.Skip(2).Take(2)) & 0xfff;
                            var sectionData = tsPayload.Skip(4).Take(sectionLength);
                            var dscripData = sectionData.Skip(8);
                            var descriptorsLoopLength = (ConvertToUint16(dscripData.Skip(3).Take(2))) & 0xfff;
                            var descriptorsData = dscripData.Skip(5).Take(descriptorsLoopLength);
                            var serviceProviderLength = (int)descriptorsData.Skip(3).First();
                            this.serviceProvider = Encoding.UTF8.GetString(descriptorsData.Skip(4).Take(serviceProviderLength).ToArray());
                            var serviceNameLength = (int)descriptorsData.Skip(4 + serviceProviderLength).First();
                            this.serviceName = Encoding.UTF8.GetString(descriptorsData.Skip(5 + serviceProviderLength).Take(serviceNameLength).ToArray());
                        }
                    }
                    if (this.programId != "" && (this.serviceName != "" || this.serviceProvider != ""))
                        break;
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

using System;
using System.Collections.Generic;
using System.Linq;
using System.Net.Sockets;
using System.Text;
using System.Text.RegularExpressions;
using System.Threading.Tasks;

namespace RtspRecorder
{
    internal static class TcpClientEx
    {
        public static void SendString(this TcpClient client, string msg)
        {
            if(!client.Connected)
                throw new Exception("尚未建立连接!!");
            var data = Encoding.UTF8.GetBytes(msg);
            client.GetStream().Write(data, 0, data.Length);
        }

        public static string ReceieveHead(this TcpClient client)
        {
            StringBuilder stringBuilder = new StringBuilder();

            while (client.Available == 0) Thread.Sleep(200);

            var stream = client.GetStream();
            var reader = new StreamReader(stream);
            var line = "";
            while ((line = reader.ReadLine()) != "")
            {
                stringBuilder.AppendLine(line);
            }
            if (Regex.IsMatch(stringBuilder.ToString(), "Content-Length: (.*)"))
            {
                var size = Convert.ToInt32(Regex.Match(stringBuilder.ToString(), "Content-Length: (.*)").Groups[1].Value);
                var t = new char[size];
                reader.ReadBlock(t, 0, size);
                stringBuilder.AppendLine();
                stringBuilder.Append(t);
            }
            return stringBuilder.ToString();
        }
    }
}

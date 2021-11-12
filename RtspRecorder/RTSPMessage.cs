using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace RtspRecorder
{
    internal class RTSPMessage
    {
        private static string UA = "Lavf58.20.100";

        public static string GetDescribeMessage(string url, int seq)
        {
            return $"DESCRIBE {url} RTSP/1.0\r\n" +
                $"CSeq: {seq}\r\n" +
                $"User-Agent: {UA}\r\n" +
                $"Accept: application/sdp\r\n"+
                $"\r\n";
        }

        public static string GetSetupMessage(string url, int seq)
        {
            //使用TCP直接承载MPEG2-TS 不使用RTP封装
            return $"SETUP {url} RTSP/1.0\r\n" +
                $"Transport: MP2T/TCP;unicast;interleaved=0-1\r\n" +
                $"CSeq: {seq}\r\n" +
                $"User-Agent: {UA}\r\n" +
                $"\r\n";
        }

        public static string GetPlayMessage(string url, int seq)
        {
            return $"PLAY {url} RTSP/1.0\r\n" +
                $"Range: npt=0.000-\r\n" +
                $"CSeq: {seq}\r\n" +
                $"User-Agent: {UA}\r\n" +
                $"\r\n";
        }

        public static string GetTearDownMessage(string url, int seq)
        {
            return $"TEARDOWN {url} RTSP/1.0\r\n" +
                $"CSeq: {seq}\r\n" +
                $"User-Agent: {UA}\r\n" +
                $"\r\n";
        }
    }
}

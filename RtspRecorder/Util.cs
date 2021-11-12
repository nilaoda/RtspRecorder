using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace RtspRecorder
{
    internal class Util
    {
        /// <summary>
        /// 从 hh:mm:ss 解析TimeSpan
        /// </summary>
        /// <param name="timeStr"></param>
        /// <returns></returns>
        public static TimeSpan ParseDur(string timeStr)
        {
            var arr = timeStr.Replace("：", ":").Split(':');
            var days = -1;
            var hours = -1;
            var mins = -1;
            var secs = -1;
            arr.Reverse().Select(i => Convert.ToInt32(i)).ToList().ForEach(item =>
            {
                if (secs == -1) secs = item;
                else if (mins == -1) mins = item;
                else if (hours == -1) hours = item;
                else if (days == -1) days = item;
            });

            if (days == -1) days = 0;
            if (hours == -1) hours = 0;
            if (mins == -1) mins = 0;
            if (secs == -1) secs = 0;

            return new TimeSpan(days, hours, mins, secs);
        }

        /// <summary>
        /// 将秒数转换为 hh.mm.ss
        /// </summary>
        /// <param name="secs"></param>
        /// <returns></returns>
        public static string ConvertSeconds(int secs)
        {
            var ts = new TimeSpan(0, 0, 0, secs);
            return ts.ToString(@"hh\:mm\:ss").Replace(":", ".");
        }

        public static string GetValidFileName(string input, string re = ".")
        {
            string title = input;
            foreach (char invalidChar in Path.GetInvalidFileNameChars())
            {
                title = title.Replace(invalidChar.ToString(), re);
            }
            return title;
        }

        public static String FormatTime(Int32 time)
        {
            TimeSpan ts = new TimeSpan(0, 0, time);
            string str = "";
            str = (ts.Hours.ToString("00") == "00" ? "" : ts.Hours.ToString("00") + "h") + ts.Minutes.ToString("00") + "m" + ts.Seconds.ToString("00") + "s";
            return str;
        }

        public static String FormatFileSize(Double fileSize)
        {
            if (fileSize < 0)
            {
                throw new ArgumentOutOfRangeException("fileSize");
            }
            else if (fileSize >= 1024 * 1024 * 1024)
            {
                return string.Format("{0:########0.00}GB", ((Double)fileSize) / (1024 * 1024 * 1024));
            }
            else if (fileSize >= 1024 * 1024)
            {
                return string.Format("{0:####0.00}MB", ((Double)fileSize) / (1024 * 1024));
            }
            else if (fileSize >= 1024)
            {
                return string.Format("{0:####0.00}KB", ((Double)fileSize) / 1024);
            }
            else
            {
                return string.Format("{0}bytes", fileSize);
            }
        }

        public static bool CheckTime(string[] input)
        {
            if (input.Length == 1)
            {
                try
                {
                    var start = DateTime.ParseExact(input[0], "yyyyMMddHHmmss", System.Globalization.CultureInfo.CurrentCulture);
                    if (start > DateTime.Now)
                        return false;
                }
                catch (Exception)
                {
                    return false;
                }
            }
            else if(input.Length == 2)
            {
                try
                {
                    var start = DateTime.ParseExact(input[0], "yyyyMMddHHmmss", System.Globalization.CultureInfo.CurrentCulture);
                    var end = DateTime.ParseExact(input[1], "yyyyMMddHHmmss", System.Globalization.CultureInfo.CurrentCulture);
                    if (start > end || start > DateTime.Now)
                        return false;
                }
                catch (Exception)
                {
                    return false;
                }
            }
            else
            {
                return false;
            }
            return true;
        }

        public static string GetPlaybackUrl(string url, string[] arr)
        {
            if (url.Contains("/PLTV/"))
            {
                url = url.Replace("/PLTV/", "/TVOD/");
                url += "&playseek=" + string.Join("-", arr);
                url += arr.Length == 1 ? "-20991231235959" : "";
            }
            else
            {
                throw new ArgumentException("链接不支持回看!");
            }
            return url;
        }
    }
}

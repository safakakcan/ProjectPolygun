// MIRROR CHANGE: drop in Codice.Utils HttpUtility subset to not depend on Unity's plastic scm package
// SOURCE: Unity Plastic SCM package

using System;
using System.Collections;
using System.Collections.Generic;
using System.Collections.Specialized;
using System.IO;
using System.Text;

namespace Edgegap.Codice.Utils // MIRROR CHANGE: namespace Edgegap.* to not collide if anyone has Plastic SCM installed already
{
    public sealed class HttpUtility
    {
        private static void WriteCharBytes(IList buf, char ch, Encoding e)
        {
            if (ch > 'ÿ')
            {
                var encoding = e;
                var chars = new char[1] { ch };
                foreach (var num in encoding.GetBytes(chars))
                    buf.Add(num);
            }
            else
            {
                buf.Add((byte)ch);
            }
        }

        public static string UrlDecode(string s, Encoding e)
        {
            if (null == s)
                return null;
            if (s.IndexOf('%') == -1 && s.IndexOf('+') == -1)
                return s;
            if (e == null)
                e = Encoding.UTF8;
            var length = (long)s.Length;
            var buf = new List<byte>();
            for (var index = 0; index < length; ++index)
            {
                var ch = s[index];
                if (ch == '%' && index + 2 < length && s[index + 1] != '%')
                {
                    if (s[index + 1] == 'u' && index + 5 < length)
                    {
                        var num = GetChar(s, index + 2, 4);
                        if (num != -1)
                        {
                            WriteCharBytes(buf, (char)num, e);
                            index += 5;
                        }
                        else
                        {
                            WriteCharBytes(buf, '%', e);
                        }
                    }
                    else
                    {
                        int num;
                        if ((num = GetChar(s, index + 1, 2)) != -1)
                        {
                            WriteCharBytes(buf, (char)num, e);
                            index += 2;
                        }
                        else
                        {
                            WriteCharBytes(buf, '%', e);
                        }
                    }
                }
                else if (ch == '+')
                {
                    WriteCharBytes(buf, ' ', e);
                }
                else
                {
                    WriteCharBytes(buf, ch, e);
                }
            }

            var array = buf.ToArray();
            return e.GetString(array);
        }

        private static int GetInt(byte b)
        {
            var ch = (char)b;
            if (ch >= '0' && ch <= '9')
                return ch - 48;
            if (ch >= 'a' && ch <= 'f')
                return ch - 97 + 10;
            return ch >= 'A' && ch <= 'F' ? ch - 65 + 10 : -1;
        }

        private static int GetChar(string str, int offset, int length)
        {
            var num1 = 0;
            var num2 = length + offset;
            for (var index = offset; index < num2; ++index)
            {
                var b = str[index];
                if (b > '\u007F')
                    return -1;
                var num3 = GetInt((byte)b);
                if (num3 == -1)
                    return -1;
                num1 = (num1 << 4) + num3;
            }

            return num1;
        }

        public static string UrlEncode(string str)
        {
            return UrlEncode(str, Encoding.UTF8);
        }

        public static string UrlEncode(string s, Encoding Enc)
        {
            if (s == null)
                return null;
            if (s == string.Empty)
                return string.Empty;
            var flag = false;
            var length = s.Length;
            for (var index = 0; index < length; ++index)
            {
                var c = s[index];
                if ((c < '0' || (c < 'A' && c > '9') || (c > 'Z' && c < 'a') || c > 'z') && !HttpEncoder.NotEncoded(c))
                {
                    flag = true;
                    break;
                }
            }

            if (!flag)
                return s;
            var bytes1 = new byte[Enc.GetMaxByteCount(s.Length)];
            var bytes2 = Enc.GetBytes(s, 0, s.Length, bytes1, 0);
            return Encoding.ASCII.GetString(UrlEncodeToBytes(bytes1, 0, bytes2));
        }

        public static byte[] UrlEncodeToBytes(byte[] bytes, int offset, int count)
        {
            return bytes == null ? null : HttpEncoder.Current.UrlEncode(bytes, offset, count);
        }

        public static string HtmlDecode(string s)
        {
            if (s == null)
                return null;
            using (var output = new StringWriter())
            {
                HttpEncoder.Current.HtmlDecode(s, output);
                return output.ToString();
            }
        }

        public static NameValueCollection ParseQueryString(string query)
        {
            return ParseQueryString(query, Encoding.UTF8);
        }

        public static NameValueCollection ParseQueryString(
            string query,
            Encoding encoding)
        {
            if (query == null)
                throw new ArgumentNullException(nameof(query));
            if (encoding == null)
                throw new ArgumentNullException(nameof(encoding));
            if (query.Length == 0 || (query.Length == 1 && query[0] == '?'))
                return new HttpQSCollection();
            if (query[0] == '?')
                query = query.Substring(1);
            var result = (NameValueCollection)new HttpQSCollection();
            ParseQueryString(query, encoding, result);
            return result;
        }

        internal static void ParseQueryString(
            string query,
            Encoding encoding,
            NameValueCollection result)
        {
            if (query.Length == 0)
                return;
            var str1 = HtmlDecode(query);
            var length = str1.Length;
            var num1 = 0;
            var flag = true;
            while (num1 <= length)
            {
                var startIndex = -1;
                var num2 = -1;
                for (var index = num1; index < length; ++index)
                    if (startIndex == -1 && str1[index] == '=')
                    {
                        startIndex = index + 1;
                    }
                    else if (str1[index] == '&')
                    {
                        num2 = index;
                        break;
                    }

                if (flag)
                {
                    flag = false;
                    if (str1[num1] == '?')
                        ++num1;
                }

                string name;
                if (startIndex == -1)
                {
                    name = null;
                    startIndex = num1;
                }
                else
                {
                    name = UrlDecode(str1.Substring(num1, startIndex - num1 - 1), encoding);
                }

                if (num2 < 0)
                {
                    num1 = -1;
                    num2 = str1.Length;
                }
                else
                {
                    num1 = num2 + 1;
                }

                var str2 = UrlDecode(str1.Substring(startIndex, num2 - startIndex), encoding);
                result.Add(name, str2);
                if (num1 == -1)
                    break;
            }
        }

        private sealed class HttpQSCollection : NameValueCollection
        {
            public override string ToString()
            {
                var count = Count;
                if (count == 0)
                    return "";
                var stringBuilder = new StringBuilder();
                var allKeys = AllKeys;
                for (var index = 0; index < count; ++index)
                    stringBuilder.AppendFormat("{0}={1}&", allKeys[index], UrlEncode(this[allKeys[index]]));
                if (stringBuilder.Length > 0)
                    --stringBuilder.Length;
                return stringBuilder.ToString();
            }
        }
    }
}
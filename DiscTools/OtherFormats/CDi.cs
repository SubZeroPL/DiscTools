using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;

namespace DiscTools.OtherFormats
{
    public class CDi
    {
        public static DiscInspector CDiParser(DiscInspector di)
        {
            if (!File.Exists(di.CuePath))
                return null;

            using (var stream = File.Open(di.CuePath, FileMode.Open))
            {
                // try and detect dreamcast
                var headerPos = GetHeaderOffset(stream);
                stream.Seek(headerPos, SeekOrigin.Begin);
                var buffer = new byte[0x100];
                var detection = new byte[0x10];
                stream.Read(buffer, 0, buffer.Length);
                Array.Copy(buffer, 0x0, detection, 0, detection.Length);
                var detStr = (Encoding.UTF8.GetString(detection));

                // internal name
                stream.Seek(headerPos, SeekOrigin.Begin);
                buffer = new byte[0x100];
                var internalName = new byte[0x80];
                stream.Read(buffer, 0, buffer.Length);
                Array.Copy(buffer, 0x80, internalName, 0, internalName.Length);
                var intName = (Encoding.UTF8.GetString(internalName));

                var buffStr = (Encoding.UTF8.GetString(buffer));

                var data = new byte[0x9];
                stream.Read(buffer, 0, buffer.Length);
                Array.Copy(buffer, 0x40, data, 0, data.Length);
                /*
                header = 0x0 SEGA SEGAKATANA for 0x10 bytes (0xF bytes without 0x20 space)
                internal name = 0x80 for 0x80 bytes
                serial number (id) = 0x40 for 9 bytes
                */

                


                var res = (Encoding.UTF8.GetString(data));

                return di;
            }
        }

        private static long GetHeaderOffset(Stream stream)
        {
            var header = new byte[] { 0x53, 0x45, 0x47, 0x41, 0x20, 0x53, 0x45, 0x47, 0x41, 0x4B, 0x41, 0x54, 0x41, 0x4E, 0x41 };
            var buffer = new byte[1024 * 1024]; //read a MiB at a time


            for (var i = 1; i < stream.Length / 1024; i++)
            {
                var streamPos = (stream.Length - (i * buffer.Length));
                if (streamPos < 0) break;
                stream.Position = streamPos;
                stream.Read(buffer, 0, buffer.Length);
                var index = IndexOfSequence(buffer, header, 0);
                if (index.Count > 0)
                {
                    var bufferIndex = index[0];
                    var streamIndex = streamPos + bufferIndex;
                    return streamIndex;
                }
            }
            return 0;
        }
        //adapted from http://stackoverflow.com/posts/332667
        public static List<int> IndexOfSequence(byte[] buffer, byte[] pattern, int startIndex)
        {
            var positions = new List<int>();
            var i = Array.IndexOf<byte>(buffer, pattern[0], startIndex);
            while (i >= 0 && i <= buffer.Length - pattern.Length)
            {
                var segment = new byte[pattern.Length];
                Buffer.BlockCopy(buffer, i, segment, 0, pattern.Length);
                if (segment.SequenceEqual<byte>(pattern))
                    positions.Add(i);
                i = Array.IndexOf<byte>(buffer, pattern[0], i + pattern.Length);
            }
            return positions;
        }
    }
}

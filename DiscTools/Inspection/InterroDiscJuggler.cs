using System;
using System.Collections.Generic;
using System.Text;
using System.Linq;
using System.Text.RegularExpressions;
using System.IO;

namespace DiscTools.Inspection
{
    public partial class Interrogator
    {
        public DetectedDiscType ScanDiscJuggler()
        {
            if (DiscJugIsDreamcast())
                return DetectedDiscType.DreamCast;

            return DetectedDiscType.UnknownFormat;
        }

        public bool DiscJugIsDreamcast()
        {
            // load CDI stream
            using (var stream = File.Open(discI.CuePath, FileMode.Open))
            {
                // try and detect dreamcast
                var headerPos = GetDCHeaderOffset(stream);
                stream.Seek(headerPos, SeekOrigin.Begin);
                var buffer = new byte[0x100];
                var detection = new byte[0x10];
                stream.Read(buffer, 0, buffer.Length);
                Array.Copy(buffer, 0x0, detection, 0, detection.Length);

                // buffer should now contain the header info
                var header = new List<string>();

                for (var i = 0; i < 20; i++)
                {
                    var lookup = System.Text.Encoding.Default.GetString(buffer.Skip(i * 16).Take(16).ToArray());
                    header.Add(lookup);
                }

                if (header[0].Trim() != "SEGA SEGAKATANA")
                    return false;

                discI.Data.SerialNumber = header[4].Split(' ').First().Trim();
                discI.Data.Version = header[4].Split(' ').Last().Trim();
                discI.Data.GameTitle = (header[8] + header[9]).Trim();
                discI.Data.InternalDate = header[5].Trim();
                discI.Data.Publisher = header[1].Trim();
                discI.Data.AreaCodes = header[3].Split(' ').First().Trim();
                discI.Data.PeripheralCodes = header[3].Split(' ').Last().Trim();

                discI.Data.MediaID = header[2].Split(' ').First().Trim();
                discI.Data.MediaInfo = header[2].Trim().Split(' ').Last().Trim();

                discI.Data.DeviceInformation = header[0].Trim();
                discI.Data.ManufacturerID = header[7].Trim();

                return true;
            }
        }
        

        private static long GetDCHeaderOffset(Stream stream)
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

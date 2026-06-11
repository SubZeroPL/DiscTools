using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Text;
using System.Text.RegularExpressions;
using System.IO;

namespace DiscTools.ISO
{
    public class FFMpeg
    {
        public static string FFMpegPath;

        public class AudioQueryResult
        {
            public bool IsAudio;
        }

        private static string[] Escape(IEnumerable<string> args)
        {
            //return args.Select(s => s.Contains(" ") ? string.Format("\"{0}\"", s) : s).ToArray();

            var working = new List<string>();
            foreach (var s in args)
            {
                if (s.Contains(" "))
                {
                    var w = "\"" + s + "\"";
                    working.Add(w);
                }
                else
                {
                    working.Add(s);
                }
            }

            return working.ToArray();
        }

        //note: accepts . or : in the stream stream/substream separator in the stream ID format, since that changed at some point in FFMPEG history
        //if someone has a better idea how to make the determination of whether an audio stream is available, I'm all ears
        static readonly Regex rxHasAudio = new Regex(@"Stream \#(\d*(\.|\:)\d*)\: Audio", RegexOptions.Compiled);
        public AudioQueryResult QueryAudio(string path)
        {
            var ret = new AudioQueryResult();
            var stdout = Run("-i", path).Text;
            ret.IsAudio = rxHasAudio.Matches(stdout).Count > 0;
            return ret;
        }

        /// <summary>
        /// queries whether this service is available. if ffmpeg is broken or missing, then you can handle it gracefully
        /// </summary>
        public bool QueryServiceAvailable()
        {
            try
            {
                var stdout = Run("-version").Text;
                if (stdout.Contains("ffmpeg version")) return true;
            }
            catch
            {
            }
            return false;
        }

        public struct RunResults
        {
            public string Text;
            public int ExitCode;
        }

        public RunResults Run(params string[] args)
        {
            args = Escape(args);
            var sbCmdline = new StringBuilder();
            for (var i = 0; i < args.Length; i++)
            {
                sbCmdline.Append(args[i]);
                if (i != args.Length - 1) sbCmdline.Append(' ');
            }

            var oInfo = new ProcessStartInfo(FFMpegPath, sbCmdline.ToString())
            {
                UseShellExecute = false,
                CreateNoWindow = true,
                RedirectStandardOutput = true,
                RedirectStandardError = true
            };

            var proc = Process.Start(oInfo);
            var result = proc.StandardOutput.ReadToEnd();
            result += proc.StandardError.ReadToEnd();
            proc.WaitForExit();

            return new RunResults
            {
                ExitCode = proc.ExitCode,
                Text = result
            };
        }

        public byte[] DecodeAudio(string path)
        {
            var tempfile = Path.GetTempFileName();
            try
            {
                var runResults = Run("-i", path, "-xerror", "-f", "wav", "-ar", "44100", "-ac", "2", "-acodec", "pcm_s16le", "-y", tempfile);
                if (runResults.ExitCode != 0)
                    throw new InvalidOperationException("Failure running ffmpeg for audio decode. here was its output:\r\n" + runResults.Text);
                var ret = File.ReadAllBytes(tempfile);
                if (ret.Length == 0)
                    throw new InvalidOperationException("Failure running ffmpeg for audio decode. here was its output:\r\n" + runResults.Text);
                return ret;
            }
            finally
            {
                File.Delete(tempfile);
            }
        }
    }

    class AudioDecoder
    {
        [Serializable]
        public class AudioDecoder_Exception : Exception
        {
            public AudioDecoder_Exception(string message)
                : base(message)
            {
            }
        }

        public AudioDecoder()
        {
        }

        bool CheckForAudio(string path)
        {
            var ffmpeg = new FFMpeg();
            var qa = ffmpeg.QueryAudio(path);
            return qa.IsAudio;
        }

        /// <summary>
        /// finds audio at a path similar to the provided path (i.e. finds Track01.mp3 for Track01.wav)
        /// TODO - isnt this redundant with CueFileResolver?
        /// </summary>
        string FindAudio(string audioPath)
        {
            var basePath = Path.GetFileNameWithoutExtension(audioPath);
            //look for potential candidates
            var di = new DirectoryInfo(Path.GetDirectoryName(audioPath));
            var fis = di.GetFiles();
            //first, look for the file type we actually asked for
            foreach (var fi in fis)
            {
                if (fi.FullName.ToUpper() == audioPath.ToUpper())
                    if (CheckForAudio(fi.FullName))
                        return fi.FullName;
            }
            //then look for any other type
            foreach (var fi in fis)
            {
                if (Path.GetFileNameWithoutExtension(fi.FullName).ToUpper() == basePath.ToUpper())
                {
                    if (CheckForAudio(fi.FullName))
                    {
                        return fi.FullName;
                    }
                }
            }
            return null;
        }

        public byte[] AcquireWaveData(string audioPath)
        {
            var path = FindAudio(audioPath);
            if (path == null)
            {
                throw new AudioDecoder_Exception("Could not find source audio for: " + Path.GetFileName(audioPath));
            }
            return new FFMpeg().DecodeAudio(path);
        }

    }
}

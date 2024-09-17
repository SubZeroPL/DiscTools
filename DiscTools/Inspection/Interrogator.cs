using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using DiscTools.ISO;
using DiscTools.Objects;

namespace DiscTools.Inspection
{
    /// <summary>
    /// handles all disc indentification and data retrieval stuff
    /// </summary>
    public partial class Interrogator
    {
        private DiscInspector discI = new DiscInspector();
        private bool IntenseScan { get; set; }

        private Disc disc;
        private EDiscStreamView discView;
        private ISOFile iso;
        private DiscIdentifier di;
        private int CurrentLBA;
        private bool isIso;
        private ISONode? ifn;
        private byte[] currSector = new byte[2048];

        private DetectedDiscType DiscSubType { get; set; }

        /* Constructors */
        //public Interrogator() { }

        /// <summary>
        /// Scan for all systems
        /// </summary>
        /// <param name="cuePath"></param>
        /// <param name="intenseScan"></param>
        public Interrogator(string cuePath, bool intenseScan)
        {
            discI.CuePath = cuePath;
            IntenseScan = intenseScan;
            iso = new ISOFile();

            discI.DetectedDiscType = DetectedDiscType.UnknownFormat;
            discI.Data = new DiscData();
        }

        /* Methods */

        public DiscInspector Start(DetectedDiscType detectedDiscType = DetectedDiscType.UnknownFormat)
        {
            // cue existance check
            if (!File.Exists(discI.CuePath))
                return discI;

            //////////////////
            /* OtherFormats */
            //////////////////

            // discjuggler - currently only implemented for dreamcast CDI files
            if (IntenseScan)
            {
                if (Path.GetExtension(discI.CuePath).Equals(".cdi", StringComparison.CurrentCultureIgnoreCase))
                {
                    discI.DetectedDiscType = ScanDiscJuggler();

                    return discI;
                }
            }


            // attempt to mount the disc
            try
            {
                disc = Disc.LoadAutomagic(discI.CuePath);
            }
            catch
            {
                return discI;
            }

            // detect disc mode
            discView = EDiscStreamView.DiscStreamView_Mode1_2048;
            if (disc.TOC.Session1Format == SessionFormat.Type20_CDXA)
                discView = EDiscStreamView.DiscStreamView_Mode2_Form1_2048;

            // biztools discident init
            di = new DiscIdentifier(disc);

            // try and mount it as an ISO
            isIso = iso.Parse(new DiscStream(disc, discView, 0));

            // if iso is mounted, populate data from volume descriptor(s) (at the moment just from the first one)
            if (isIso)
            {
                var vs = iso.VolumeDescriptors.Where(a => a != null).ToArray().First();

                // translate the vd
                discI.Data._ISOData = PopulateISOData(vs);
                discI.Data._ISOData.ISOFiles = iso.Root.Children;
                ifn = null;
            }

            // populate basic disc data
            var dataTracks = 0;
            var audioTracks = 0;

            foreach (var t in disc.Structure.Sessions.Where(a => a != null))
            {
                for (int i = 0; i < t.Tracks.Count(); i++)
                {
                    if (t.Tracks[i].IsData)
                    {
                        dataTracks++;
                        continue;
                    }

                    if (t.Tracks[i].IsAudio)
                    {
                        audioTracks++;
                    }
                }
            }

            discI.Data.TotalAudioTracks = audioTracks;
            discI.Data.TotalDataTracks = dataTracks;
            discI.Data.TotalTracks = audioTracks + dataTracks;

            discI.DiscStructure = disc.Structure;

            // do actual interrogation
            discI.DetectedDiscType = detectedDiscType switch
            {
                DetectedDiscType.UnknownFormat or DetectedDiscType.UnknownCDFS => InterrogateALL(),
                _ => InterrogateSpecific(detectedDiscType)
            };

            discI.DiscTypeString = discI.DetectedDiscType.ToString();
            discI.DiscViewString = discView.ToString();

            return discI;
        }

        private readonly Dictionary<int, byte[]> _sectorCache = new Dictionary<int, byte[]>();

        private byte[]? ReadSectorCached(int lba)
        {
            //read it if we dont have it cached
            //we wont be caching very much here, it's no big deal
            //identification is not something we want to take a long time
            if (!_sectorCache.TryGetValue(lba, out var data))
            {
                data = new byte[2048];
                var read = di.dsr.ReadLBA_2048(lba, data, 0);
                if (read != 2048)
                    return null;
                _sectorCache[lba] = data;
            }

            return data;
        }

        private bool StringAt(string s, int n, int lba = 0)
        {
            var data = ReadSectorCached(lba);
            if (data == null) return false;
            byte[] cmp = Encoding.ASCII.GetBytes(s);
            byte[] cmp2 = new byte[cmp.Length];
            Buffer.BlockCopy(data, n, cmp2, 0, cmp.Length);
            return cmp.SequenceEqual(cmp2);
        }


        /* Static Methods */
        private static ISOData PopulateISOData(ISOVolumeDescriptor vd)
        {
            var i = new ISOData();

            // strings
            i.AbstractFileIdentifier =
                Encoding.Default.GetString(vd.AbstractFileIdentifier).TrimEnd('\0', ' ');
            i.ApplicationIdentifier =
                Encoding.Default.GetString(vd.ApplicationIdentifier).TrimEnd('\0', ' ');
            i.BibliographicalFileIdentifier = Encoding.Default.GetString(vd.BibliographicalFileIdentifier)
                .TrimEnd('\0', ' ');
            i.CopyrightFileIdentifier =
                Encoding.Default.GetString(vd.CopyrightFileIdentifier).TrimEnd('\0', ' ');
            i.DataPreparerIdentifier =
                Encoding.Default.GetString(vd.DataPreparerIdentifier).TrimEnd('\0', ' ');
            i.PublisherIdentifier = Encoding.Default.GetString(vd.PublisherIdentifier).TrimEnd('\0', ' ');
            i.Reserved = Encoding.Default.GetString(vd.Reserved).Trim('\0');
            i.SystemIdentifier = Encoding.Default.GetString(vd.SystemIdentifier).TrimEnd('\0', ' ');
            i.VolumeIdentifier = Encoding.Default.GetString(vd.VolumeIdentifier).TrimEnd('\0', ' ');
            i.VolumeSetIdentifier = Encoding.Default.GetString(vd.VolumeSetIdentifier).TrimEnd('\0', ' ');

            // ints
            i.NumberOfSectors = vd.NumberOfSectors;
            i.PathTableSize = vd.PathTableSize;
            i.SectorSize = vd.SectorSize;
            i.Type = vd.Type;
            i.VolumeSequenceNumber = vd.VolumeSequenceNumber;

            // datetimes
            i.EffectiveDateTime = TextConverters.ParseDiscDateTime(
                TextConverters.TruncateLongString(
                    Encoding.Default.GetString(vd.EffectiveDateTime.ToArray()).Trim(), 12));
            i.ExpirationDateTime = TextConverters.ParseDiscDateTime(
                TextConverters.TruncateLongString(
                    Encoding.Default.GetString(vd.ExpirationDateTime.ToArray()).Trim(), 12));
            i.LastModifiedDateTime = TextConverters.ParseDiscDateTime(
                TextConverters.TruncateLongString(
                    Encoding.Default.GetString(vd.LastModifiedDateTime.ToArray()).Trim(), 12));
            i.VolumeCreationDate = TextConverters.ParseDiscDateTime(
                TextConverters.TruncateLongString(
                    Encoding.Default.GetString(vd.VolumeCreationDateTime.ToArray()).Trim(), 12));

            // other
            i.RootDirectoryRecord = vd.RootDirectoryRecord;

            return i;
        }

        private static string getHexStringFromByteArray(byte[] byteArray)
        {
            var hexString = "";
            foreach (var b in byteArray)
            {
                hexString += b.ToString("X2");
            }

            return hexString;
        }
    }
}
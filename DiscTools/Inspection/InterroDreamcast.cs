using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace DiscTools.Inspection
{
    public partial class Interrogator
    {
        public bool ScanISODreamcast()
        {
            if (!discI.Data._ISOData.SystemIdentifier.Contains("SEGAKATANA")) return false;
            // store lba for IP.BIN
            var cnf = discI.Data._ISOData.ISOFiles.FirstOrDefault(a => a.Key.Contains("IP.BIN"));
            if (cnf.Key == null) return GetDreamcastData();
            ifn = cnf.Value;
            CurrentLBA = Convert.ToInt32(ifn.Offset);

            return GetDreamcastData();
        }

        public bool GetDreamcastData()
        {
            byte[] data = di.ReadData(CurrentLBA, 2048);
            currSector = data;
            string res = Encoding.Default.GetString(data);

            return GetDreamcastData(res);
        }

        public bool GetDreamcastData(string lbaString)
        {
            if (!lbaString.Contains("segakatana", StringComparison.CurrentCultureIgnoreCase))
                return false;

            var ind = lbaString.ToLower().IndexOf("segakatana", StringComparison.Ordinal);
            var d = lbaString[ind..];

            var header = new List<string>();

            var dat = Encoding.Default.GetBytes(d);

            for (var i = 0; i < 20; i++)
            {
                var lookup = Encoding.Default.GetString(dat.Skip((i * 16) - 5).Take(16).ToArray());
                header.Add(lookup);
            }

            discI.Data.SerialNumber = header[4].Split('V').First().Trim();
            discI.Data.Version = "V" + header[4].Split('V').Last().Trim();
            discI.Data.GameTitle = (header[8] + header[9]).Trim();
            discI.Data.InternalDate = header[5].Trim();
            discI.Data.Publisher = header[1].Trim();
            discI.Data.AreaCodes = header[3].Trim().Split(' ').First().Trim();
            discI.Data.PeripheralCodes = header[3].Trim().Split(' ').Last().Trim();

            discI.Data.MediaID = header[2].Split(' ').First().Trim();
            discI.Data.MediaInfo = header[2].Trim().Split(' ').Last().Trim();

            discI.Data.DeviceInformation = header[0].Trim();
            discI.Data.ManufacturerID = header[7].Trim();

            return true;
        }
    }
}
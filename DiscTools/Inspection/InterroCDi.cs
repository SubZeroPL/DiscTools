using System;
using System.Collections.Generic;
using System.Text;
using System.Linq;
using System.Text.RegularExpressions;
using DiscTools.Objects;

namespace DiscTools.Inspection
{
    public partial class Interrogator
    {
        public bool ScanISOCDi()
        {
            // currently no implementation
            return false;
        }
        
        public bool GetCDiData()
        {
            currSector = di.ReadData(CurrentLBA, 2048);

            var sS = System.Text.Encoding.Default.GetString(currSector);

            return GetCDiData(sS);
        }

        public bool GetCDiData(string lbaString)
        {
            if (lbaString.ToLower().Contains("cd-rtos"))
            {
                var cdv = new CDiVolumeDescriptor(currSector);

                discI.Data.ManufacturerID = System.Text.Encoding.Default.GetString(currSector.ToList().Skip(1).Take(4).ToArray());
                discI.Data.OtherData = System.Text.Encoding.Default.GetString(currSector.ToList().Skip(8).Take(16).ToArray()).Trim();
                var start = 190;
                var block = 128;

                var header = new List<string>();
                for (var a = 0; a < 10; a++)
                {
                    var test = System.Text.Encoding.Default.GetString(currSector.ToList().Skip(start + (a * block)).Take(block).ToArray());
                    header.Add(test);
                }

                discI.Data.DeviceInformation = header[3].Trim();
                discI.Data.GameTitle = header[0].Trim();
                discI.Data.Publisher = header[1].Trim();
                discI.Data.Developer = header[2].Trim();

                discI.Data.InternalDate = TextConverters.TruncateLongString(header[5], 12);

                return true;
            }

            return false;
        }
    }
}

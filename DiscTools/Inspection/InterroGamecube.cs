using System;
using System.Collections.Generic;
using System.Text;
using System.Linq;
using System.Text.RegularExpressions;

namespace DiscTools.Inspection
{
    public partial class Interrogator
    {
        public bool ScanISOGamecube()
        {
            // no implementation

            return false;
        }
        
        public bool GetGamecubeData()
        {
            var data = di.ReadData(CurrentLBA, 2048);
            currSector = data;
            var res = System.Text.Encoding.Default.GetString(data);

            return GetGamecubeData(res);
        }

        public bool GetGamecubeData(string lbaString)
        {
            // most of this stuff gleaned from:  https://github.com/sleepy9090/GameCubeIsoAnalyzer

            // console identification
            // dvdMagic
            //string dvdMag = Encoding.Default.GetString(Encoding.Default.GetBytes(lbaString).Skip(28).Take(4).ToArray());
            var dvdMag = getHexStringFromByteArray(Encoding.Default.GetBytes(lbaString).Skip(28).Take(4).ToArray());

            if (dvdMag != "C2339F3D")
                return false;

            discI.Data.OtherData = dvdMag;
            discI.DetectedDiscType = DetectedDiscType.Gamecube;

            var consoleId = Encoding.Default.GetString(Encoding.Default.GetBytes(lbaString).Skip(0).Take(1).ToArray());
            discI.Data.DeviceInformation = Statics.Nintendo.GetDiscId(consoleId);

            // game name
            var gName = Encoding.Default.GetString(Encoding.Default.GetBytes(lbaString).Skip(32).Take(992).ToArray()).Trim().TrimEnd('\0');
            discI.Data.GameTitle = gName;

            // game code
            var gc = Encoding.Default.GetString(Encoding.Default.GetBytes(lbaString).Skip(1).Take(2).ToArray());
            discI.Data.MediaID = gc;

            // country code
            var cc = Encoding.Default.GetString(Encoding.Default.GetBytes(lbaString).Skip(3).Take(1).ToArray());
            discI.Data.AreaCodes = Statics.Nintendo.GetRegion(cc);

            // maker code
            var makerHex = Encoding.Default.GetString(Encoding.Default.GetBytes(lbaString).Skip(4).Take(2).ToArray());
            
            discI.Data.Publisher = Statics.Nintendo.GetMaker(makerHex);

            // disc id
            var discId = Encoding.Default.GetString(Encoding.Default.GetBytes(lbaString).Skip(6).Take(1).ToArray());
            discI.Data.SerialNumber = discId;

            // version
            var ver = Encoding.Default.GetString(Encoding.Default.GetBytes(lbaString).Skip(7).Take(1).ToArray());
            discI.Data.Version = ver;

            
            
            return true;
        }
    }
}

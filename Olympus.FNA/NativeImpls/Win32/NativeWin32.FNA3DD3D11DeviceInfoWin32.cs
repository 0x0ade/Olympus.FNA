#if WINDOWS

using Microsoft.Xna.Framework;
using OlympUI;
using SDL2;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Management;
using System.Runtime.InteropServices;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Forms;

namespace Olympus.NativeImpls {
	public unsafe partial class NativeWin32 : NativeImpl {
        public class FNA3DD3D11Win32DeviceInfo : FNA3DD3D11DeviceInfo {

            public readonly string? DriverVersion;

            public FNA3DD3D11Win32DeviceInfo(FNA3DD3D11DeviceInfo orig) : base(orig.Device) {
                using ManagementObjectSearcher searcher = new ManagementObjectSearcher("select * from Win32_VideoController");
                foreach (ManagementObject obj in searcher.Get()) {
                    if (obj["Description"] as string == orig.Device) {
                        DriverVersion = obj["DriverVersion"] as string;
                        break;
                    }
                }
            }

            public override string ToString() => 
                DriverVersion is not null ?
                    $"{Device} (using D3D11, driver version: {DriverVersion})" :
                    $"{Device} (using D3D11)";

        }
    }
}

#endif

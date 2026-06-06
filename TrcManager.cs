using System;
using System.Collections.Generic;
using System.Diagnostics.SymbolStore;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using TrcSdkLib;

namespace DcsBiosListener
{
    public class TrcManager
    {
        private devCore Core = new devCore(0x0000, 0xffff);
        private devGeneral gen = new devGeneral();
        private List<dev0200> devices = new List<dev0200>();
        public TrcManager()
        {
            Core.FindDevices();

            for (int i = 0; i < 64; i++)
            {
                dev0200 gauge = Core.gauge_trc02[i];
                if (gauge != null)
                {
                    devices.Add(gauge);
                }
            }
        }

        public void SetLight(int intensity)
        {
            if (devices.Count == 1)
            {
                dev0200 device = devices[0];
                device.setLight(intensity);
            }
        }

        public List<dev0200> GetGauges()
        {
            return this.devices;
        }
    }
}

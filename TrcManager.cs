using System;
using System.Collections.Generic;
using System.Diagnostics.SymbolStore;
using System.Linq;
using System.Management;
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
        private ManagementEventWatcher deviceArrivalWatcher;
        private ManagementEventWatcher deviceRemovalWatcher;

        public event EventHandler<List<dev0200>> OnGaugesChanged;

        public TrcManager()
        {
            RebuildGaugeList();

            deviceArrivalWatcher = new ManagementEventWatcher(
                new WqlEventQuery("SELECT * FROM __InstanceCreationEvent WITHIN 2 WHERE TargetInstance ISA 'Win32_PnPEntity'"));
            deviceArrivalWatcher.EventArrived += (s, e) => RebuildGaugeList();
            deviceArrivalWatcher.Start();

            deviceRemovalWatcher = new ManagementEventWatcher(
                new WqlEventQuery("SELECT * FROM __InstanceDeletionEvent WITHIN 2 WHERE TargetInstance ISA 'Win32_PnPEntity'"));
            deviceRemovalWatcher.EventArrived += (s, e) => RebuildGaugeList();
            deviceRemovalWatcher.Start();
        }

        public void RebuildGaugeList()
        {
            Core.FindDevices();

            var found = new List<dev0200>();
            for (int i = 0; i < 64; i++)
            {
                dev0200 gauge = Core.gauge_trc02[i];
                if (gauge != null)
                {
                    found.Add(gauge);
                }
            }

            devices = found;
            Console.WriteLine($"Found TRC General guages: {devices.Count}");
            OnGaugesChanged?.Invoke(this, devices);
        }

        public void SetLight(int intensity)
        {
            List<dev0200> currentDevices = devices;
            if (currentDevices.Count == 1)
            {
                dev0200 device = currentDevices[0];
                device.setLight(intensity);
            }
        }

        public List<dev0200> GetGauges()
        {
            return this.devices;
        }
    }
}

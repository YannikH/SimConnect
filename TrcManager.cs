using System;
using System.Collections.Generic;
using System.Diagnostics.SymbolStore;
using System.Linq;
using System.Management;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using Newtonsoft.Json.Linq;
using TrcSdkLib;

namespace DcsBiosListener
{
    public class TrcManager
    {
        private devCore Core = new devCore(0x0000, 0xffff);
        private devGeneral gen = new devGeneral();
        private List<devGeneral> devices = new List<devGeneral>();
        private ManagementEventWatcher deviceArrivalWatcher;
        private ManagementEventWatcher deviceRemovalWatcher;

        public event EventHandler<List<devGeneral>> OnGaugesChanged;

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

            var found = new List<devGeneral> ();
            for (int i = 0; i < 64; i++)
            {
                dev0200 gauge = Core.gauge_trc02[i];
                if (gauge != null)
                {
                    found.Add(gauge);
                }
            }
            if (Core.trc0286 != null)
            {
                Core.trc0286.loadCalibration();
                found.Add(Core.trc0286);
            }
            if (Core.trc0287 != null)
            {
                Core.trc0287.loadCalibration();
                found.Add(Core.trc0287);
            }

            devices = found;
            Console.WriteLine($"Found TRC General guages: {devices.Count}");
            OnGaugesChanged?.Invoke(this, devices);
        }

        public void SetLight(int intensity)
        {
            List<devGeneral> currentDevices = devices;
            if (currentDevices.Count == 1)
            {
                if (currentDevices[0] is dev0200)
                {
                    dev0200 device = (dev0200)currentDevices[0];
                    device.setLight(intensity);
                }
            }
        }

        public async void SetGauge(string gaugeType, int gaugeIndex, JObject data)
        {
            if (gaugeIndex < 0 || gaugeIndex >= devices.Count) return;

            switch (gaugeType)
            {
                case "General":
                    if (devices[gaugeIndex] is dev0200 gauge)
                    {
                        int light = data["light"].Value<int>();
                        int s1 = data["Servo1"].Value<int>();
                        int s2 = data["Servo2"].Value<int>();
                        Console.WriteLine($"Setting gauge {gaugeIndex} light to {light}");
                        gauge.setLight(light);
                        gauge.setServo(1, Math.Min(Math.Max(500, s1), 2500));
                        gauge.setServo(2, Math.Min(Math.Max(500, s2), 2500));
                    }
                    break;
                case "Altimeter":
                    if (devices[gaugeIndex] is dev0286 altimeter)
                    {
                        int result;
                        double pot100ft = 0;
                        double pot1k = 0;
                        double pot10k = 0;
                        if ((result = await altimeter.requestPotmeter(0)) >= 0)
                        {
                            pot100ft = result / 1200.0 * 1000.0;
                        }
                        if ((result = await altimeter.requestPotmeter(1)) >= 0)
                        {
                            pot1k = result / 1200.0 * 1000.0;
                        }
                        if ((result = await altimeter.requestPotmeter(2)) >= 0)
                        {
                            pot10k = result / 1200.0 * 1000.0;
                        }
                        double altFt = (pot10k * 10.0) + pot1k + (pot100ft / 10.0);
                        Console.WriteLine($"Updating {pot10k} {pot1k} {pot100ft} {altFt}");
                        int light = data["light"].Value<int>();
                        altimeter.setLight(light);
                        altimeter.setServo(1, 1480 - 5);
                        //altimeter.setServo(2, 1000);
                        //const int desiredAlt = 4500;
                    }
                    break;
            }
        }

        public List<devGeneral> GetGauges()
        {
            return this.devices;
        }

        public static string GetGaugeType(devGeneral device)
        {
            switch (device)
            {
                case dev0200 _:
                    return "General";
                case dev0286 _:
                    return "Altimeter";
                default:
                    return "Unknown";
            }
        }
    }
}

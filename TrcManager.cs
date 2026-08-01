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

        public async void RebuildGaugeList()
        {
            Core.FindDevices();

            var found = new List<devGeneral> ();
            for (int i = 0; i < 64; i++)
            {
                dev0200 gauge = Core.gauge_trc02[i];
                if (gauge != null)
                {
                    await gauge.loadCalibration();
                    found.Add(gauge);
                }
            }
            if (Core.trc0286 != null)
            {
                await Core.trc0286.loadCalibration();
                found.Add(Core.trc0286);
            }
            if (Core.trc0287 != null)
            {
                await Core.trc0287.loadCalibration();
                found.Add(Core.trc0287);
            }
            if (Core.trc0288 != null)
            {
                await Core.trc0288.loadCalibration();
                found.Add(Core.trc0288);
            }
            if (Core.trc0289 != null)
            {
                await Core.trc0289.loadCalibration();
                found.Add(Core.trc0289);
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

        public async Task SetGauge(string gaugeType, int gaugeIndex, JObject data)
        {
            if (gaugeIndex < 0 || gaugeIndex >= devices.Count) return;
            if (devices[gaugeIndex] == null) return;
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
                        //int result;
                        //double pot1 = 0;
                        //double pot2 = 0;
                        //if ((result = await altimeter.requestPotmeter(0)) >= 0)
                        //{
                        //    pot1 = result;
                        //}
                        //if ((result = await altimeter.requestPotmeter(1)) >= 0)
                        //{
                        //    pot2 = result;
                        //}
                        //double pot10k = pot1 == 0 ? pot2 : pot1;
                        ////double altFt = (pot10k * 10.0) + pot1k + (pot100ft / 10.0);
                        //Console.WriteLine($"Updating {pot10k}");
                        //int altLow = await altimeter.getAltiHigh();
                        //Console.WriteLine($"AL {altLow}");
                        //int light = data["light"].Value<int>();
                        //altimeter.setLight(light);
                        //altimeter.setServo(1, 1480 - 100);
                        //altimeter.setServo(2, 1000);
                        //const int desiredAlt = 4500;
                    }
                    break;
                case "HeadingIndicator":
                    if (devices[gaugeIndex] is dev0288 headingIndicator)
                    {
                        float direction = data["direction"].Value<float>();
                        await headingIndicator.setCompass(direction);
                        int light = data["light"].Value<int>();
                        headingIndicator.setLight(light);
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
                case dev0288 _:
                    return "HeadingIndicator";
                default:
                    return "Unknown";
            }
        }
    }
}

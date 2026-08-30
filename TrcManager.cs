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
                //Core.trc0286.initHeight();
                await Core.trc0286.initParameters();
                await Core.trc0286.setHeight(75000);
                found.Add(Core.trc0286);
                Console.WriteLine("Altimeter Found");
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

        private static readonly TimeSpan GaugeUpdateInterval = TimeSpan.FromMilliseconds(50);
        private readonly Dictionary<int, GaugeUpdateState> pendingGaugeUpdates = new Dictionary<int, GaugeUpdateState>();

        private class GaugeUpdateState
        {
            public string GaugeType;
            public JObject Data;
            public bool HasPending;
            public bool IsRunning;
        }

        public Task SetGauge(string gaugeType, int gaugeId, JObject data)
        {
            GaugeUpdateState state;
            lock (pendingGaugeUpdates)
            {
                if (!pendingGaugeUpdates.TryGetValue(gaugeId, out state))
                {
                    state = new GaugeUpdateState();
                    pendingGaugeUpdates[gaugeId] = state;
                }
                state.GaugeType = gaugeType;
                state.Data = data;
                state.HasPending = true;

                if (state.IsRunning) return Task.CompletedTask;
                state.IsRunning = true;
            }

            return PumpGaugeUpdates(gaugeId, state);
        }

        private async Task PumpGaugeUpdates(int gaugeId, GaugeUpdateState state)
        {
            try
            {
                while (true)
                {
                    string gaugeType;
                    JObject data;
                    lock (pendingGaugeUpdates)
                    {
                        gaugeType = state.GaugeType;
                        data = state.Data;
                        state.HasPending = false;
                    }

                    await ApplyGaugeUpdate(gaugeType, gaugeId, data);
                    await Task.Delay(GaugeUpdateInterval);

                    lock (pendingGaugeUpdates)
                    {
                        if (!state.HasPending) return;
                    }
                }
            }
            finally
            {
                lock (pendingGaugeUpdates)
                {
                    state.IsRunning = false;
                }
            }
        }

        private devGeneral FindGauge(int gaugeId)
        {
            return devices.FirstOrDefault(d => d.ProductID == gaugeId);
        }

        public async Task ApplyGaugeUpdate(string gaugeType, int gaugeId, JObject data)
        {
            //Console.WriteLine($"Updating gauge {gaugeType}");
            devGeneral device = FindGauge(gaugeId);
            if (device == null) return;
            switch (gaugeType)
            {
                case "General":
                    if (device is dev0200 gauge)
                    {
                        int light = data["light"].Value<int>();
                        int s1 = data["Servo1"].Value<int>();
                        int s2 = data["Servo2"].Value<int>();
                        Console.WriteLine($"Setting gauge {gaugeId} light to {light}");
                        gauge.setLight(light);
                        gauge.setServo(1, Math.Min(Math.Max(500, s1), 2500));
                        gauge.setServo(2, Math.Min(Math.Max(500, s2), 2500));
                    }
                    break;
                case "Altimeter":
                    if (device is dev0286 altimeter)
                    {
                        int light = data["light"].Value<int>();
                        altimeter.setLight(light);
                        int alt = data["AltFt"].Value<int>();
                        await altimeter.setHeight(alt);
                    }
                    break;
                case "AltimeterAdjust":
                    if (device is dev0286 altimeterAdjust)
                    {
                        int adjustment = data["adjust"].Value<int>();
                        Console.WriteLine($"Adjust {adjustment}");
                        if (adjustment == -1)
                        {
                            Console.WriteLine("ADJ Down");
                            await altimeterAdjust.AdjustInternalHeightDown();
                        }
                        else if (adjustment == 1)
                        {
                            Console.WriteLine("ADJ Up");
                            await altimeterAdjust.AdjustInternalHeightUp();
                        }
                    }
                    break;
                case "HeadingIndicator":
                    if (device is dev0288 headingIndicator)
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

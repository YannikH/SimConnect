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

        // Fired once per gauge write that is actually applied to hardware - i.e. after
        // SetGauge's coalescing pump (GaugeUpdateInterval) has done its rate limiting, not
        // once per incoming request. Lets the UI show a debug log of what really happened.
        public event EventHandler<GaugeUpdateAppliedEventArgs> OnGaugeUpdateApplied;

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

        private static readonly TimeSpan GaugeUpdateInterval = TimeSpan.FromMilliseconds(100);

        // TrcSdkLib opens each device's FileStream with isAsync:false, so the read/write
        // timeouts it manages internally never actually abort a stuck HID transaction - they
        // just mark the Task canceled while the real blocking call keeps running on a thread
        // pool thread forever, holding the device's internal busy-lock. If a single update
        // takes anywhere near this long, the device is wedged, not just slow.
        private static readonly TimeSpan GaugeStallTimeout = TimeSpan.FromSeconds(2);

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

                    Task applyTask = ApplyGaugeUpdate(gaugeType, gaugeId, data);
                    Task finished = await Task.WhenAny(applyTask, Task.Delay(GaugeStallTimeout));
                    if (finished != applyTask)
                    {
                        // The underlying HID transaction never returned. Don't keep pumping
                        // into a device object that's permanently wedged (its own busy-lock
                        // will never release) - drop it and let re-enumeration pick up a
                        // fresh, unlocked instance next time this gauge is addressed, the same
                        // way a physical unplug/replug recovers it today.
                        Console.WriteLine($"Gauge {gaugeId} ({gaugeType}) stopped responding - dropping it for re-detection.");
                        DropStalledGauge(gaugeId);
                        return;
                    }
                    await applyTask;

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

        // Removes the wedged device so FindGauge can no longer reach it, then re-triggers
        // discovery. The old object (and whatever thread is still stuck inside it) is simply
        // abandoned - it holds no OS resources we can safely reclaim from here, but nothing
        // will address it again once it's out of `devices`.
        private void DropStalledGauge(int gaugeId)
        {
            devices = devices.Where(d => d.ProductID != gaugeId).ToList();
            RebuildGaugeList();
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
            bool applied = false;
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
                        applied = true;
                    }
                    break;
                case "Altimeter":
                    if (device is dev0286 altimeter)
                    {
                        int light = data["light"].Value<int>();
                        altimeter.setLight(light);
                        int alt = data["AltFt"].Value<int>();
                        await altimeter.setHeight(alt);
                        applied = true;
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
                        applied = true;
                    }
                    break;
                case "HeadingIndicator":
                    if (device is dev0288 headingIndicator)
                    {
                        float direction = data["direction"].Value<float>();
                        await headingIndicator.setCompass(direction);
                        int light = data["light"].Value<int>();
                        headingIndicator.setLight(light);
                        applied = true;
                    }
                    break;
            }

            if (applied)
            {
                OnGaugeUpdateApplied?.Invoke(this, new GaugeUpdateAppliedEventArgs
                {
                    GaugeType = gaugeType,
                    GaugeId = gaugeId,
                    Data = data,
                    Timestamp = DateTime.Now,
                });
            }
        }

        public List<devGeneral> GetGauges()
        {
            return this.devices;
        }

        public class GaugeUpdateAppliedEventArgs : EventArgs
        {
            public string GaugeType;
            public int GaugeId;
            public JObject Data;
            public DateTime Timestamp;
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

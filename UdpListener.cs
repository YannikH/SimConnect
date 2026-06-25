using System;
using System.Collections.Generic;
using System.Linq;
using System.Net.Sockets;
using System.Net;
using System.Text;
using System.Threading.Tasks;
using System.Threading;
using System.IO;

namespace DcsBiosListener
{
    public class UdpListener
    {
        // ── DCS-BIOS defaults ─────────────────────────────────────────────────
        public const string DefaultMulticastGroup = "239.255.50.10";
        public const int DefaultPort = 5010;

        private const uint SyncAddress = 0x5555;

        // ── State ─────────────────────────────────────────────────────────────
        private readonly string _multicastGroup;
        private readonly int _port;

        private UdpClient _udpClient;
        private CancellationTokenSource _cts;
        private Task _listenTask;
        private bool _disposed;

        // 64 KB address space mirroring DCS-BIOS export memory
        private readonly byte[] _exportData = new byte[0x10000];

        // ── Events ────────────────────────────────────────────────────────────

        /// <summary>Raised for every address/data pair received (before frame sync).</summary>
        public event EventHandler<DcsBiosDataEventArgs> DataReceived;

        /// <summary>Raised once per complete frame (after the sync sequence).</summary>
        public event EventHandler FrameCompleted;

        // ── Construction ──────────────────────────────────────────────────────

        public UdpListener(
            string multicastGroup = DefaultMulticastGroup,
            int port = DefaultPort)
        {
            _multicastGroup = multicastGroup;
            _port = port;
        }

        // ── Public API ────────────────────────────────────────────────────────

        /// <summary>Start listening in the background.</summary>
        public void Start()
        {
            if (_listenTask != null && !_listenTask.IsCompleted)
                throw new InvalidOperationException("Already listening.");

            _cts = new CancellationTokenSource();
            _udpClient = CreateUdpClient();
            _listenTask = Task.Run(() => ListenLoop(_cts.Token));

            Console.WriteLine($"[DCS-BIOS] Listening on {_multicastGroup}:{_port}");
        }

        /// <summary>Stop listening and release the socket.</summary>
        public void Stop()
        {
            _cts?.Cancel();
            _udpClient?.Close();   // unblocks the blocking Receive call
            try { _listenTask?.Wait(2000); } catch { /* ignore */ }
            Console.WriteLine("[DCS-BIOS] Stopped.");
        }

        /// <summary>
        /// Returns the current 16-bit value at a given DCS-BIOS address.
        /// </summary>
        public ushort GetUInt16(ushort address)
        {
            if (address > 0xFFFE)
                throw new ArgumentOutOfRangeException(nameof(address));
            return (ushort)(_exportData[address] | (_exportData[address + 1] << 8));
        }

        /// <summary>
        /// Extract a bit-field value using a mask and optional right-shift.
        /// Typical usage:  int value = GetValue(address, mask: 0x00FF, shift: 0);
        /// </summary>
        public int GetValue(ushort address, ushort mask, int shift = 0)
            => (GetUInt16(address) & mask) >> shift;

        // ── Private ───────────────────────────────────────────────────────────

        private UdpClient CreateUdpClient()
        {
            var client = new UdpClient();
            client.Client.SetSocketOption(SocketOptionLevel.Socket,
                                          SocketOptionName.ReuseAddress, true);
            client.Client.Bind(new IPEndPoint(IPAddress.Any, _port));
            var mcastAddr = IPAddress.Parse(_multicastGroup);
            client.JoinMulticastGroup(mcastAddr);

            return client;
        }

        private void ListenLoop(CancellationToken token)
        {
            //using (StreamWriter sw = new StreamWriter("C:/SentPackets.txt"))
            //{
                var remoteEp = new IPEndPoint(IPAddress.Any, 0);

                while (!token.IsCancellationRequested)
                {
                    byte[] packet;
                    try
                    {
                        packet = _udpClient.Receive(ref remoteEp);
                    }
                    catch (SocketException)
                    {
                        break;
                    }
                    catch (Exception ex)
                    {
                        Console.Error.WriteLine($"[DCS-BIOS] Receive error: {ex.Message}");
                        break;
                    }

                    try
                    {
                        ProcessPacket(packet);
                    }
                    catch (Exception ex)
                    {
                        Console.Error.WriteLine($"[DCS-BIOS] ProcessPacket error: {ex.Message}");
                    }
                }
            //}
        }

        private void ProcessPacket(byte[] packet/*, StreamWriter sw*/)
        {
            int i = 0;
            while (i + 4 <= packet.Length)
            {
                uint address = (uint)(packet[i] | (packet[i + 1] << 8));
                uint count   = (uint)(packet[i + 2] | (packet[i + 3] << 8));
                i += 4;

                if (address == SyncAddress)
                {
                    FrameCompleted?.Invoke(this, EventArgs.Empty);
                    continue;
                }

                if (i + count > packet.Length) break;

                for (uint j = 0; j < count; j++)
                {
                    if (address + j < _exportData.Length)
                        _exportData[address + j] = packet[i + j];
                }

                for (uint j = 0; j + 1 < count; j += 2)
                {
                    ushort addr = (ushort)(address + j);
                    ushort data = (ushort)(packet[i + j] | (packet[i + j + 1] << 8));
                    DataReceived?.Invoke(this, new DcsBiosDataEventArgs(addr, data));
                    long timestamp = DateTimeOffset.UtcNow.ToUnixTimeSeconds();
                    //sw.WriteLine($"${timestamp}:{addr}:{data}");
                }

                i += (int)count;
            }
        }

        // ── IDisposable ───────────────────────────────────────────────────────

        public void Dispose()
        {
            if (_disposed) return;
            _disposed = true;
            Stop();
            _cts?.Dispose();
        }
    }

    // ── Supporting types ──────────────────────────────────────────────────────

    public class DcsBiosDataEventArgs : EventArgs
    {
        /// <summary>DCS-BIOS export address (0x0000–0xFFFE).</summary>
        public ushort Address { get; }

        /// <summary>16-bit data value written at that address.</summary>
        public ushort Data { get; }

        public DcsBiosDataEventArgs(ushort address, ushort data)
        {
            Address = address;
            Data = data;
        }
    }
}

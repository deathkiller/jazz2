#if !__ANDROID__ && !__CONSTRAINED__ && !WINDOWS_RUNTIME && !UNITY_STANDALONE_LINUX
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Net;
using System.Net.NetworkInformation;
using System.Net.Sockets;
using System.Security.Cryptography;

namespace Lidgren.Network
{
    public static partial class NetUtility
    {
        private static readonly long s_timeInitialized = Stopwatch.GetTimestamp();
        private static readonly double s_dInvFreq = 1.0 / Stopwatch.Frequency;

        public static ulong GetPlatformSeed(int seedInc)
        {
            ulong seed = (ulong)Stopwatch.GetTimestamp();
            return seed ^ ((ulong)Environment.WorkingSet + (ulong)seedInc);
        }

        public static double Now { get { return (Stopwatch.GetTimestamp() - s_timeInitialized) * s_dInvFreq; } }

        private static IList<NetworkInterface> GetNetworkInterfaces()
        {
            IPGlobalProperties computerProperties = IPGlobalProperties.GetIPGlobalProperties();
            if (computerProperties == null)
                return null;

            NetworkInterface[] interfaces = NetworkInterface.GetAllNetworkInterfaces();
            if (interfaces == null || interfaces.Length < 1)
                return null;

            List<NetworkInterface> result = new List<NetworkInterface>();
            foreach (NetworkInterface adapter in interfaces) {
                if (adapter.NetworkInterfaceType == NetworkInterfaceType.Loopback || adapter.NetworkInterfaceType == NetworkInterfaceType.Unknown)
                    continue;
                if (!adapter.Supports(NetworkInterfaceComponent.IPv4) && !adapter.Supports(NetworkInterfaceComponent.IPv6))
                    continue;
                if (adapter.OperationalStatus != OperationalStatus.Up)
                    continue;

                // Make sure this adapter has any IPv4 or IPv6 addresses
                IPInterfaceProperties properties = adapter.GetIPProperties();
                foreach (UnicastIPAddressInformation unicastAddress in properties.UnicastAddresses) {
                    if (unicastAddress != null && unicastAddress.Address != null &&
                        (unicastAddress.Address.AddressFamily == AddressFamily.InterNetwork ||
                         unicastAddress.Address.AddressFamily == AddressFamily.InterNetworkV6)) {
                        // Yes it does, return this network interface.
                        result.Add(adapter);
                        break;
                    }
                }
            }
            return result;
        }

        /// <summary>
        /// If available, returns the bytes of the physical (MAC) address for the first usable network interface
        /// </summary>
        public static byte[] GetMacAddressBytes()
        {
            var interfaces = GetNetworkInterfaces();
            if (interfaces == null || interfaces.Count == 0) {
                return null;
            }
            return interfaces[0].GetPhysicalAddress().GetAddressBytes();
        }

        public static IList<IPAddress> GetBroadcastAddresses()
        {
            IList<NetworkInterface> interfaces = GetNetworkInterfaces();
            if (interfaces == null)
                return null;

            List<IPAddress> addresses = new List<IPAddress>();
            foreach (var ni in interfaces) {
                var properties = ni.GetIPProperties();
                foreach (UnicastIPAddressInformation unicastAddress in properties.UnicastAddresses) {
                    if (unicastAddress != null && unicastAddress.Address != null && unicastAddress.Address.AddressFamily == AddressFamily.InterNetwork) {
                        var mask = unicastAddress.IPv4Mask;
                        byte[] ipAdressBytes = unicastAddress.Address.GetAddressBytes();
                        byte[] subnetMaskBytes = mask.GetAddressBytes();

                        if (ipAdressBytes.Length != subnetMaskBytes.Length)
                            throw new ArgumentException("Lengths of IP address and subnet mask do not match.");

                        byte[] broadcastAddress = new byte[ipAdressBytes.Length];
                        for (int i = 0; i < broadcastAddress.Length; i++) {
                            broadcastAddress[i] = (byte)(ipAdressBytes[i] | (subnetMaskBytes[i] ^ 255));
                        }

                        addresses.Add(new IPAddress(broadcastAddress));
                    }
                }
            }

            if (addresses.Count == 0) {
                addresses.Add(IPAddress.Broadcast);
            }

            return addresses;
        }

        /// <summary>
        /// Gets my local IPv4 or IPv6 address (not necessarily external)
        /// </summary>
        public static IList<IPAddress> GetSelfAddresses()
        {
            var interfaces = GetNetworkInterfaces();
            if (interfaces == null) {
                return null;
            }

            List<IPAddress> addresses = new List<IPAddress>();
            foreach (var ni in interfaces) {
                IPInterfaceProperties properties = ni.GetIPProperties();
                foreach (UnicastIPAddressInformation unicastAddress in properties.UnicastAddresses) {
                    if (unicastAddress != null && unicastAddress.Address != null &&
                        (unicastAddress.Address.AddressFamily == AddressFamily.InterNetwork ||
                         unicastAddress.Address.AddressFamily == AddressFamily.InterNetworkV6)) {

                        addresses.Add(unicastAddress.Address);
                    }
                }
            }

            return addresses;
        }

        public static void Sleep(int milliseconds)
        {
            System.Threading.Thread.Sleep(milliseconds);
        }

        public static IPAddress CreateAddressFromBytes(byte[] bytes)
        {
            return new IPAddress(bytes);
        }

        private static readonly SHA256 s_sha = SHA256.Create();
        public static byte[] ComputeSHAHash(byte[] bytes, int offset, int count)
        {
            return s_sha.ComputeHash(bytes, offset, count);
        }
    }

    public static partial class NetTime
    {
        private static readonly long s_timeInitialized = Stopwatch.GetTimestamp();
        private static readonly double s_dInvFreq = 1.0 / Stopwatch.Frequency;

        /// <summary>
        /// Get number of seconds since the application started
        /// </summary>
        public static double Now { get { return (Stopwatch.GetTimestamp() - s_timeInitialized) * s_dInvFreq; } }
    }
}
#endif
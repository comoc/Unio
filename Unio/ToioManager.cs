﻿using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Windows.Devices.Bluetooth;
using Windows.Devices.Bluetooth.Advertisement;
using Windows.Devices.Bluetooth.GenericAttributeProfile;

// https://toio.github.io/toio-spec/docs/ble_communication_overview.html
// https://docs.microsoft.com/ja-jp/windows/uwp/devices-sensors/gatt-client
namespace Unio
{
    class ToioManager
    {
        private BluetoothLEAdvertisementWatcher watcher;
        private List<Toio> toioList = new List<Toio>();

        private static ToioManager instance = null;
        private static readonly object lockObj = new object();

        public delegate void NewlyFound(Toio toio);
        private NewlyFound newlyFound = null;

        private ToioManager()
        {
        }

        public static ToioManager Instance {
            get {
                lock (lockObj)
                {
                    if (instance == null)
                    {
                        instance = new ToioManager();
                    }
                    return instance;
                }
            }
        }

        public void Search(int period, NewlyFound f = null)
        {
            watcher = new BluetoothLEAdvertisementWatcher();
            watcher.Received += Watcher_Received;
            watcher.ScanningMode = BluetoothLEScanningMode.Passive;
            if (f != null)
                newlyFound += f;
            watcher.Start();
            Thread.Sleep(period);
            watcher.Stop();
        }


        private void Watcher_Received(BluetoothLEAdvertisementWatcher sender, BluetoothLEAdvertisementReceivedEventArgs args)
        {
            lock (lockObj)
            {
                if (toioList.Count(t => t.Address == args.BluetoothAddress) > 0)
                {
                    return;
                }
            }

            var bleServiceUUIDs = args.Advertisement.ServiceUuids;
            foreach (var uuid in bleServiceUUIDs)
            {
                if (uuid == Toio.ServiceUUID)
                {
                    Task task = Task.Run(async () =>
                    {
                        BluetoothLEDevice bluetoothLeDevice = await BluetoothLEDevice.FromBluetoothAddressAsync(args.BluetoothAddress);

                        GattDeviceServicesResult result = await bluetoothLeDevice.GetGattServicesForUuidAsync(Toio.ServiceUUID);

                        if (result.Status == GattCommunicationStatus.Success)
                        {
                            var service = result.Services[0];

                            Toio toio = new Toio(args.BluetoothAddress, service);

                            // Test
                            byte battery = toio.ReadBatteryLife();

                            lock (lockObj)
                            {
                                toioList.Add(toio);
                            }

                            if (newlyFound != null)
                                newlyFound(toio);
                        }
                    });
                    task.Wait();
                }
            }
        }

        public int GetToioCount()
        {
            int n = 0;
            lock (lockObj)
            {
                n = toioList.Count;
            }
            return n;
        }

        public Toio GetToio(int n)
        {
            Toio t = null;
            lock (lockObj)
            {
                t = toioList.ElementAt(n);
            }
            return t;
        }

    }
}

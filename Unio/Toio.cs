using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Runtime.InteropServices.WindowsRuntime;
using System.Threading.Tasks;
using Windows.Devices.Bluetooth;
using Windows.Devices.Bluetooth.GenericAttributeProfile;
using Windows.Storage.Streams;

namespace Unio
{
    public class Toio
    {
        public static readonly Guid ServiceUUID = new Guid("10B20100-5B3B-4571-9508-CF3EFCD7BBAE");

        public static readonly Guid CharacteristicUUID_IDInformation = new Guid("10B20101-5B3B-4571-9508-CF3EFCD7BBAE");
        public static readonly Guid CharacteristicUUID_MotionOrMagneticSensorInformation = new Guid("10B20106-5B3B-4571-9508-CF3EFCD7BBAE");
        public static readonly Guid CharacteristicUUID_ButtonInformation = new Guid("10B20107-5B3B-4571-9508-CF3EFCD7BBAE");
        public static readonly Guid CharacteristicUUID_BatteryInformation = new Guid("10B20108-5B3B-4571-9508-CF3EFCD7BBAE");
        public static readonly Guid CharacteristicUUID_MotorControl = new Guid("10B20102-5B3B-4571-9508-CF3EFCD7BBAE");
        public static readonly Guid CharacteristicUUID_LightControl = new Guid("10B20103-5B3B-4571-9508-CF3EFCD7BBAE");
        public static readonly Guid CharacteristicUUID_SoundControl = new Guid("10B20104-5B3B-4571-9508-CF3EFCD7BBAE");
        public static readonly Guid CharacteristicUUID_Configuration = new Guid("10B201FF-5B3B-4571-9508-CF3EFCD7BBAE");

        private static int serialNumberCounter = 1;

        public int SerialNumber { private set; get; }
        public ulong Address { private set; get; }

        // Service
        private GattDeviceService Service = null;
        // Information
        private GattCharacteristic CharacteristicIDInformation = null;
        private GattCharacteristic CharacteristicMotionOrMagneticSensorInformation = null;
        private GattCharacteristic CharacteristicButtonInformation = null;
        private GattCharacteristic CharacteristicBatteryInformation = null;
        // Control
        private GattCharacteristic CharacteristicMotorControl = null;
        private GattCharacteristic CharacteristicLightControl = null; // TODO
        private GattCharacteristic CharacteristicSoundControl = null; // TODO
        private GattCharacteristic CharacteristicConfiguration = null; // TODO

        private List<GattCharacteristic> characteristics = new List<GattCharacteristic>();

        public static readonly byte MotionSensorInformationType = 0x01;
        public static readonly byte MagneticSensorInformationType = 0x02;
        public static readonly byte PostureAngleSensorInformationType = 0x03;

        public byte BatteryLife { private set; get; }

        public byte LightControlType;
        public byte LightControlDuration;
        public byte LightControlCount;
        public byte LightControlID;
        public byte LightValueRed;
        public byte LightValueGreen;
        public byte LightValueBlue;

        public byte SoundControlType;
        public byte SoundEffectID;
        public byte SoundVolume;

        public Toio(ulong address, GattDeviceService service)
        {
            SerialNumber = serialNumberCounter;
            serialNumberCounter++;

            Address = address;
            Service = service;

            // Initialize required characteristics
            Task task = Task.Run(async () =>
            {
                var characteristics = await Service.GetCharacteristicsForUuidAsync(Toio.CharacteristicUUID_IDInformation, BluetoothCacheMode.Uncached);
                if (characteristics.Status == GattCommunicationStatus.Success)
                {
                    CharacteristicIDInformation = characteristics.Characteristics.FirstOrDefault();
                    CharacteristicIDInformation.ValueChanged += CharacteristicIDInformation_ValueChanged;
                    await CharacteristicIDInformation.WriteClientCharacteristicConfigurationDescriptorAsync(GattClientCharacteristicConfigurationDescriptorValue.Notify);
                    this.characteristics.Add(CharacteristicIDInformation);
                }

                characteristics = await Service.GetCharacteristicsForUuidAsync(Toio.CharacteristicUUID_MotionOrMagneticSensorInformation);
                if (characteristics.Status == GattCommunicationStatus.Success)
                {
                    CharacteristicMotionOrMagneticSensorInformation = characteristics.Characteristics.FirstOrDefault();
                    CharacteristicMotionOrMagneticSensorInformation.ValueChanged += CharacteristicMotionOrMagneticSensorInformation_ValueChanged;
                    await CharacteristicMotionOrMagneticSensorInformation.WriteClientCharacteristicConfigurationDescriptorAsync(GattClientCharacteristicConfigurationDescriptorValue.Notify);
                    this.characteristics.Add(CharacteristicMotionOrMagneticSensorInformation);
                }

                characteristics = await Service.GetCharacteristicsForUuidAsync(Toio.CharacteristicUUID_ButtonInformation);
                if (characteristics.Status == GattCommunicationStatus.Success)
                {
                    CharacteristicButtonInformation = characteristics.Characteristics.FirstOrDefault();
                    CharacteristicButtonInformation.ValueChanged += CharacteristicButtonInformation_ValueChanged;
                    await CharacteristicButtonInformation.WriteClientCharacteristicConfigurationDescriptorAsync(GattClientCharacteristicConfigurationDescriptorValue.Notify);
                    this.characteristics.Add(CharacteristicButtonInformation);
                }

                characteristics = await Service.GetCharacteristicsForUuidAsync(Toio.CharacteristicUUID_BatteryInformation);
                if (characteristics.Status == GattCommunicationStatus.Success)
                {
                    CharacteristicBatteryInformation = characteristics.Characteristics.FirstOrDefault();
                    CharacteristicBatteryInformation.ValueChanged += CharacteristicBatteryInformation_ValueChanged;
                    await CharacteristicBatteryInformation.WriteClientCharacteristicConfigurationDescriptorAsync(GattClientCharacteristicConfigurationDescriptorValue.Notify);
                    this.characteristics.Add(CharacteristicBatteryInformation);
                }

                characteristics = await Service.GetCharacteristicsForUuidAsync(Toio.CharacteristicUUID_MotorControl);
                if (characteristics.Status == GattCommunicationStatus.Success)
                {
                    CharacteristicMotorControl = characteristics.Characteristics.FirstOrDefault();
                    this.characteristics.Add(CharacteristicMotorControl);
                }

                characteristics = await Service.GetCharacteristicsForUuidAsync(Toio.CharacteristicUUID_LightControl);
                if (characteristics.Status == GattCommunicationStatus.Success)
                {
                    CharacteristicLightControl = characteristics.Characteristics.FirstOrDefault();
                    //await CharacteristicLightControl.WriteClientCharacteristicConfigurationDescriptorAsync(GattClientCharacteristicConfigurationDescriptorValue.)
                    this.characteristics.Add(CharacteristicLightControl);
                }

                characteristics = await Service.GetCharacteristicsForUuidAsync(Toio.CharacteristicUUID_SoundControl);
                if (characteristics.Status == GattCommunicationStatus.Success)
                {
                    CharacteristicSoundControl = characteristics.Characteristics.FirstOrDefault();
                    this.characteristics.Add(CharacteristicSoundControl);
                }

                characteristics = await Service.GetCharacteristicsForUuidAsync(Toio.CharacteristicUUID_Configuration);
                if (characteristics.Status == GattCommunicationStatus.Success)
                {
                    CharacteristicConfiguration = characteristics.Characteristics.FirstOrDefault();
                    this.characteristics.Add(CharacteristicConfiguration);
                }

            });
            task.Wait();
        }

        public delegate void OnValueChanged(int serial, string uuid, byte[] data);
        public event OnValueChanged onValueChanged;

        private void CharacteristicButtonInformation_ValueChanged(GattCharacteristic sender, GattValueChangedEventArgs args)
        {
            byte[] data = ReadDataOnValueChanged(args);
            onValueChanged?.Invoke(SerialNumber, sender.Uuid.ToString(), data);

            byte ButtonID = data[0];
            byte ButtonStatus = data[1];

            Debug.WriteLine($"{System.Reflection.MethodBase.GetCurrentMethod().Name} ButtonID:{ButtonID}, ButtonStatus:{ButtonStatus}");
        }

        private void CharacteristicMotionOrMagneticSensorInformation_ValueChanged(GattCharacteristic sender, GattValueChangedEventArgs args)
        {
            byte[] data = ReadDataOnValueChanged(args);
            onValueChanged?.Invoke(SerialNumber, sender.Uuid.ToString(), data);

            if (data[0] == MotionSensorInformationType)
            {
                byte MotionSensorLevelDetection = data[1];
                byte MotionSensorCollisionDetection = data[2];
                byte MotionSensorDoubleClickDetection = data[3];
                byte MotionSensorPostureDetection = data[4];

                Debug.WriteLine($"{System.Reflection.MethodBase.GetCurrentMethod().Name} MotionSensorInformationType:{MotionSensorInformationType}, MotionSensorLevelDetection:{MotionSensorLevelDetection}, MotionSensorCollisionDetection:{MotionSensorCollisionDetection}, MotionSensorDoubleClickDetection:{MotionSensorDoubleClickDetection}, MotionSensorPostureDetection:{MotionSensorPostureDetection}");
            }
            else if (data[0] == MagneticSensorInformationType)
            {
                byte MagneticSensorStatus = data[1];

                Debug.WriteLine($"{System.Reflection.MethodBase.GetCurrentMethod().Name} MagneticSensorInformationType:{MagneticSensorInformationType}, MagneticSensorStatus:{MagneticSensorStatus}");
            }

            else if (data[0] == PostureAngleSensorInformationType)
            {
                byte sMagneticSensorStatus = data[1];

                Debug.WriteLine($"{System.Reflection.MethodBase.GetCurrentMethod().Name} PostureAngleSensorInformationType:{PostureAngleSensorInformationType}, data:{data}");
            }
        }

        private void CharacteristicIDInformation_ValueChanged(GattCharacteristic sender, GattValueChangedEventArgs args)
        {
            byte[] data = ReadDataOnValueChanged(args);
            onValueChanged?.Invoke(SerialNumber, sender.Uuid.ToString(), data);

            byte IDPositionID = data[0];
            ushort IDCubeCenterX = BitConverter.ToUInt16(data, 1);
            ushort IDCubeCenterY = BitConverter.ToUInt16(data, 3);
            ushort IDCubeAngle = BitConverter.ToUInt16(data, 5);
            ushort IDSensorX = BitConverter.ToUInt16(data, 7);
            ushort IDSensorY = BitConverter.ToUInt16(data, 9);
            ushort IDSensorAngle = BitConverter.ToUInt16(data, 11);

            Debug.WriteLine($"{System.Reflection.MethodBase.GetCurrentMethod().Name} IDPositionID:{IDPositionID}, IDCubeCenterX:{IDCubeCenterX}, IDCubeCenterY:{IDCubeCenterY}, IDCubeCenterX:{IDCubeCenterX}, IDCubeAngle:{IDCubeAngle}, IDSensorX:{IDSensorX}, IDSensorY:{IDSensorY}, IDSensorAngle:{IDSensorAngle}");
        }

        private void CharacteristicBatteryInformation_ValueChanged(GattCharacteristic sender, GattValueChangedEventArgs args)
        {
            byte[] data = ReadDataOnValueChanged(args);
            onValueChanged?.Invoke(SerialNumber, sender.Uuid.ToString(), data);

            BatteryLife = data[0];
        }

        private byte[] ReadDataOnValueChanged(GattValueChangedEventArgs args)
        {
            byte[] data = new byte[args.CharacteristicValue.Length];
            DataReader.FromBuffer(args.CharacteristicValue).ReadBytes(data);
            return data;
        }

        private async Task<byte[]> ReadDataFromGattCharacteristicAsync(GattCharacteristic characteristic)
        {
            byte[] input = null;
            GattReadResult result = await characteristic.ReadValueAsync();
            if (result.Status == GattCommunicationStatus.Success)
            {
                var reader = DataReader.FromBuffer(result.Value);
                input = new byte[reader.UnconsumedBufferLength];
                reader.ReadBytes(input);
            }
            return input;
        }

        public byte ReadBatteryLife()
        {
            if (CharacteristicBatteryInformation == null)
                return 0;

            Task task = Task.Run(async () =>
            {
                byte[] values = await ReadDataFromGattCharacteristicAsync(CharacteristicBatteryInformation);
                BatteryLife = values[0];
                Debug.WriteLine($"{System.Reflection.MethodBase.GetCurrentMethod().Name} BatteryLife {BatteryLife}");
            });
            task.Wait();

            return BatteryLife;
        }

        public byte[] Read(string uuid)
        {
            return null;
        }

        public void Write(string uuid, byte[] data)
        {
            //foreach (GattCharacteristic gc in characteristics)
            //{
            //    string u = gc.Uuid.ToString();
            //    Debug.WriteLine(u);
            //}
            GattCharacteristic c = characteristics.Find(x => x.Uuid.ToString() == uuid);
            if (c != null)
                Write(c, data);
        }

        private void Write(GattCharacteristic c, byte[] data)
        {
            if (c == null)
                return;
            if (data == null || data.Length == 0)
                return;

            Task task = Task.Run(async () =>
            {
                GattCommunicationStatus result = await c.WriteValueAsync(data.AsBuffer());
                if (result == GattCommunicationStatus.Success)
                {
                    // Successfully wrote to device
                    Debug.WriteLine("Write Success");
                }
                else
                {
                    Debug.WriteLine("Write Failure");
                }
            });
            task.Wait();
        }

        public void ReadRequest(string uuid, byte data)
        {
            Write(uuid, new byte[] { data });
        }
    }
}

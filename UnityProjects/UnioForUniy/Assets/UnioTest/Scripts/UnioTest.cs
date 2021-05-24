using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using WebSocketSharp;

namespace Unio
{
    public class NetData
    {
        public int serial;
        public string uuid;
        public int[] data;
        public NetData ()
        {
            serial = 0;
            uuid = "";
            data = new int[0];
        }
    }

    public class Data
    {
        public int serial; //{ private set; get; }
        public string uuid; //{ private set; get; }
        public byte[] data; //{ private set; get; }
        public Data ()
        {
            serial = 0;
            uuid = "";
            data = new byte[0];
        }

        public Data (Data src)
        {
            this.serial = src.serial;
            this.uuid = src.uuid;
            this.data = src.data;
        }

        public Data (int serial, string uuid, byte[] data)
        {
            this.serial = serial;
            this.uuid = uuid;
            this.data = data;
        }
    }

    public class DataConverter
    {
        public static Data TryConvert (NetData rd)
        {
            if (rd == null || rd.uuid == null || rd.uuid == "" || rd.data == null)
                return null;

            byte[] d = new byte[rd.data.Length];
            for (int i = 0; i < rd.data.Length; i++)
            {
                if (rd.data[i] < 0)
                    rd.data[i] = 0;
                else if (rd.data[i] > 255)
                    rd.data[i] = 255;
                d[i] = (byte) rd.data[i];
            }
            Data data = new Data (rd.serial, rd.uuid.ToUpper (), d);

            return data;
        }

        public static NetData TryConvert (Data rd)
        {
            if (rd == null || rd.uuid == null || rd.uuid == "" || rd.data == null)
                return null;

            NetData data = new NetData ();
            data.serial = rd.serial;
            data.uuid = rd.uuid;
            data.data = new int[rd.data.Length];
            for (int i = 0; i < rd.data.Length; i++)
            {
                data.data[i] = rd.data[i];
            }

            return data;
        }
    }

    public class ConnectionRequestData : Data
    {
        public ConnectionRequestData () : base (0, "", null) { }
    }

    public class BatteryData : Data
    {
        public static readonly string Uuid = "10B20108-5B3B-4571-9508-CF3EFCD7BBAE";
        public byte batteryLevel;
        public BatteryData (Data d) : base (d)
        {
            batteryLevel = d.data[0];
        }
    }

    public class ButtonData : Data
    {
        public static readonly string Uuid = "10B20107-5B3B-4571-9508-CF3EFCD7BBAE";
        public static readonly byte ButtonStatePressed = 0x80;
        public static readonly byte ButtonStateReleased = 0x00;

        public byte buttonId;
        public byte buttonState;

        public ButtonData(Data src) : base(src)
        {
            buttonId = data[0];
            buttonState = data[1];
        }
    }

    public class LampControlData : Data
    {
        public static readonly string Uuid = "10B20103-5B3B-4571-9508-CF3EFCD7BBAE";
        public static readonly byte ControlTypeAllOff = 0x01;
        public static readonly byte ControlTypeLimitedTime = 0x03;
        public static readonly byte ControlTypeContinuous = 0x04;
        public static readonly byte LampCount = 0x01;
        public static readonly byte LampId = 0x01;

        // All off
        public LampControlData (int serialNumber) : base (serialNumber, Uuid, new byte[1] { ControlTypeAllOff }) { }

        // One shot
        public LampControlData (int serialNumber, byte duration, byte red, byte green, byte blue)
        : base (serialNumber, Uuid, new byte[7] { ControlTypeLimitedTime, duration, LampCount, LampId, red, green, blue }) { }

        // Sequence
        // operationCount must be the same as (duration_red_green_blue.Count / 4)
        public LampControlData (int serialNumber, byte repeatCount, byte operationCount, byte[] duration_red_green_blue)
        {
            this.serial = serialNumber;
            this.uuid = Uuid;
            this.data = new byte[3 + 6 * (int)operationCount];
            this.data[0] = ControlTypeContinuous;
            this.data[1] = repeatCount;
            this.data[2] = operationCount;
            for (int n = 0; n < (int)operationCount; n++)
            {
                this.data[3 + 6 * n] = duration_red_green_blue[4 * n];
                this.data[3 + 6 * n + 1] = LampCount;
                this.data[3 + 6 * n + 2] = LampId;
                this.data[3 + 6 * n + 3] = duration_red_green_blue[4 * n + 1];
                this.data[3 + 6 * n + 4] = duration_red_green_blue[4 * n + 2];
                this.data[3 + 6 * n + 5] = duration_red_green_blue[4 * n + 3];

            }
        }
    }

    public class MagneticSensorRequestData : Data
    {
        public static readonly string Uuid = "10B20106-5B3B-4571-9508-CF3EFCD7BBAE";
        public static readonly byte InformationType = 0x82;

        public MagneticSensorRequestData (int serialNumber) : base (serialNumber, Uuid, new byte[1] { InformationType }) { }
    }

    public class MagneticSensorData : Data
    {
        public static readonly string Uuid = "10B20106-5B3B-4571-9508-CF3EFCD7BBAE";
        public static readonly byte InformationType = 0x02;

        public byte magnetStatus;
        public byte magneticForceStrength;
        public sbyte magneticForceDirectionX;
        public sbyte magneticForceDirectionY;
        public sbyte magneticForceDirectionZ;

        public MagneticSensorData (Data d) : base (d)
        {
            magnetStatus = d.data[1];
            magneticForceStrength = d.data[1];
            magneticForceDirectionX = Convert.ToSByte (d.data[2]);
            magneticForceDirectionY = Convert.ToSByte (d.data[3]);
            magneticForceDirectionZ = Convert.ToSByte (d.data[4]);
        }
    }

    public class MotionSensorRequestData : Data
    {
        public static readonly string Uuid = "10B20106-5B3B-4571-9508-CF3EFCD7BBAE";
        public static readonly byte InformationType = 0x81;

        public MotionSensorRequestData (int serialNumber) : base (serialNumber, Uuid, new byte[1] { InformationType }) { }
    }

    public class MotionSensorData : Data
    {
        public static readonly string Uuid = "10B20106-5B3B-4571-9508-CF3EFCD7BBAE";
        public static readonly byte InformationType = 0x01;
        public static readonly byte LevelNotLevel = 0x00;
        public static readonly byte CollisionNoCollision = 0x00;
        public static readonly byte DoubleTapNoDoubleTap = 0x00;
        public static readonly byte PositionPositivePosition = 0x01;
        public static readonly byte PostureTopSideUp = 1;
        public static readonly byte PostureBottomSideUp = 2;
        public static readonly byte PostureBackSideUp = 3;
        public static readonly byte PostureFrontSideUp = 4;
        public static readonly byte PostureRightSideUp = 5;
        public static readonly byte PostureLeftSideUp = 6;
        public static readonly byte ShakeNoShake = 0x00;

        public byte levelDetection;
        public byte collisionDetection;
        public byte doubleTapDetection;
        public byte postureDetection;
        public byte shakeDetection;

        public MotionSensorData (Data d) : base (d)
        {
            levelDetection = data[1];
            collisionDetection = data[2];
            doubleTapDetection = data[3];
            postureDetection = data[4];
            shakeDetection = data[5];
        }

    }

    // TODO: Insufficient. A little more implementation is needed to support all the features
    public class MotorControlData : Data
    {
        public static readonly string Uuid = "10B20102-5B3B-4571-9508-CF3EFCD7BBAE";
        public static readonly byte ControlTypeBasic = 0x01;
        public static readonly byte ControlTypeTarget = 0x03;
        public static readonly byte LeftId = 0x01;
        public static readonly byte RightId = 0x02;
        public static readonly byte MoveTypeRotateAndMove = 1;
        public static readonly byte MoveTypeRotateAndMoveWithoutBackword = 2;
        public static readonly byte MoveTypeRotateThenMove = 3;

        public MotorControlData (int serialNumber, bool isLeftForward, byte leftSpeed, bool isRightForward, byte rightSpeed) : base (serialNumber, Uuid,
        new byte[7] { ControlTypeBasic, LeftId, (byte) (isLeftForward ? 0x01 : 0x02), leftSpeed, RightId, (byte) (isRightForward ? 0x01 : 0x02), rightSpeed}) { }

        // With target
        public MotorControlData (int serialNumber, byte controlIdValue, byte timeout, byte moveType, byte maxIndicationSpeed, byte speedChangeType, ushort targetX, ushort targetY, ushort targetAngle)
        {
            this.serial = serialNumber;
            this.uuid = Uuid;
            byte[] targetXB = BitConverter.GetBytes(targetX);
            byte[] targetYB = BitConverter.GetBytes(targetY);
            byte[] targetAngleB = BitConverter.GetBytes(targetAngle);
            this.data = new byte[13] {ControlTypeTarget, controlIdValue, timeout, moveType, maxIndicationSpeed, speedChangeType, 0x00, targetXB[0], targetXB[1], targetYB[0], targetYB[1], targetAngleB[0], targetAngleB[1]};
        }
    }

    public class PostureAngleRequestData : Data
    {
        public static readonly string Uuid = "10B20106-5B3B-4571-9508-CF3EFCD7BBAE";
        public static readonly byte InformationType = 0x83;
        public static readonly byte NotificationTypeEuler = 0x01;
        public static readonly byte NotificationTypeQuaternion = 0x02;

        public PostureAngleRequestData (int serialNumber, byte notificationType) : base (serialNumber, Uuid, new byte[2] { InformationType, notificationType }) { }
    }

    public class PostureAngleData : Data
    {
        public static readonly string Uuid = "10B20106-5B3B-4571-9508-CF3EFCD7BBAE";
        public static readonly byte InformationType = 0x03;
        public static readonly byte Euler = 0x01;
        public static readonly byte Quaternion = 0x02;
        public Quaternion posture;

        public PostureAngleData (Data d) : base (d)
        {
            if (d.data[1] == Euler)
            {
                int x = BitConverter.ToInt16 (data, 2);
                int y = BitConverter.ToInt16 (data, 4);
                int z = BitConverter.ToInt16 (data, 6);
                posture = new Quaternion ();
                posture.eulerAngles = new Vector3 (y, z + 180, x);

            }
            else if (d.data[1] == Quaternion)
            {
                int w = BitConverter.ToInt16 (data, 2);
                int x = BitConverter.ToInt16 (data, 4);
                int y = BitConverter.ToInt16 (data, 6);
                int z = BitConverter.ToInt16 (data, 8);
                float fw = w / 1000f;
                float fx = x / 1000f;
                float fy = y / 1000f;
                float fz = z / 1000f;
                // Convert right-handed to left-handed
                posture = new Quaternion (-fy, fz, fx, -fw);
                // Flip around z-axis
                posture.eulerAngles = new Vector3 (-posture.eulerAngles.x, posture.eulerAngles.y, -posture.eulerAngles.z + 180);
            }
        }
    }

    public class ReadSensorPositionIdData : Data
    {
        public static readonly string Uuid = "10B20101-5B3B-4571-9508-CF3EFCD7BBAE";
        public static byte InformationType = 0x01;
        public ushort cubeCenterX { private set; get; }
        public ushort cubeCenterY { private set; get; }
        public ushort cubeAngle { private set; get; }
        public ushort readSensorX { private set; get; }
        public ushort readSensorY { private set; get; }

        public ReadSensorPositionIdData (Data src) : base (src)
        {
            cubeCenterX = BitConverter.ToUInt16 (data, 1);
            cubeCenterY = BitConverter.ToUInt16 (data, 3);
            cubeAngle = BitConverter.ToUInt16 (data, 5);
            readSensorX = BitConverter.ToUInt16 (data, 7);
            readSensorY = BitConverter.ToUInt16 (data, 9);
        }
    }

    public class ReadSensorPositionIdMissedData : Data
    {
        public static readonly string Uuid = "10B20101-5B3B-4571-9508-CF3EFCD7BBAE";
        public static byte InformationType = 0x03;
        public ReadSensorPositionIdMissedData (Data src) : base (src) { }
    }

    public class ReadSensorStandardIdData : Data
    {
        public static readonly string Uuid = "10B20101-5B3B-4571-9508-CF3EFCD7BBAE";
        public static byte InformationType = 0x02;
        public uint standardId { private set; get; }
        public ushort cubeAngle { private set; get; }

        public ReadSensorStandardIdData (Data d) : base (d)
        {
            standardId = BitConverter.ToUInt32 (data, 1);
            cubeAngle = BitConverter.ToUInt16 (data, 5);
        }
    }

    public class ReadSensorStandardIdMissedData : Data
    {
        public static readonly string Uuid = "10B20101-5B3B-4571-9508-CF3EFCD7BBAE";
        public static byte InformationType = 0x04;

        public ReadSensorStandardIdMissedData (Data src) : base (src) { }
    }

    // TODO: Insufficient. A little more implementation is needed to support all the features
    public class SoundControlData : Data
    {
        public static readonly string Uuid = "10B20104-5B3B-4571-9508-CF3EFCD7BBAE";
        public static readonly byte ControlTypeSoundEffect = 0x02;
        public static readonly byte ControlTypeMidiNote = 0x03;

        // Stop
        public SoundControlData (int serialNumber) : base (serialNumber, Uuid, new byte[1] { 0x01 }) { } // Stop

        // Sound effect
        public SoundControlData (int serialNumber, byte controlType, byte soundEffectId, byte volume) : base (serialNumber, Uuid, new byte[3] { controlType, soundEffectId, volume }) { }

        // MIDI note
        public SoundControlData (int serialNumber, byte controlType, byte repeatCount, byte duration, byte midiNoteNumber, byte volume) : base (serialNumber, Uuid,
            new byte[6] { controlType, repeatCount, 0x01, duration, midiNoteNumber, volume }) { }
    }
}

public class UnioTest : MonoBehaviour
{
    [SerializeField] string webSocketServerAddress = "127.0.0.1";
    [SerializeField] int webSocketServerPort = 12345;
    [SerializeField] UnityEngine.UI.Text serialNumbersText = null;
    WebSocket client;
    HashSet<int> serialNumbers = new HashSet<int> ();
    ConcurrentQueue<Unio.Data> dataQueue = new ConcurrentQueue<Unio.Data> ();

    void Start ()
    {
        string serverUrl = "ws://" + webSocketServerAddress + ":" + webSocketServerPort + "/";
        client = new WebSocket (serverUrl);
        client.OnMessage += (sender, e) =>
        {
            Debug.Log ("OnMessage: " + e.Data);

            Unio.NetData nd = JsonUtility.FromJson<Unio.NetData> (e.Data);

            if (nd.serial == 0)
                return;
            serialNumbers.Add (nd.serial);

            Unio.Data d = Unio.DataConverter.TryConvert (nd);
            dataQueue.Enqueue (d);
        };

        client.OnOpen += (sender, e) =>
        {

        };

        client.OnClose += (sender, e) =>
        {
            client = null;
        };

        client.Connect ();
    }

    void Update ()
    {
        if (serialNumbers.Count == 0)
            serialNumbersText.text = "Connected count: 0";
        else
        {
            string ss = "";
            int i = 0;
            foreach (int n in serialNumbers)
            {
                ss += n;
                i ++;
                if (i != serialNumbers.Count)
                    ss += ", ";
            }
            string s = "Serial numbers: [" + ss + "]";
            serialNumbersText.text = s;
        }

        while (dataQueue.Count != 0)
        {
            Unio.Data d = null;
            if (dataQueue.TryDequeue (out d) && d != null)
            {
                if (d.data == null || d.data.Length == 0)
                {
                    // Serial number
                    Debug.Log ("Serial: " + d.serial);
                }
                else if (d.uuid == Unio.BatteryData.Uuid)
                {
                    Unio.BatteryData batteryData = new Unio.BatteryData (d);
                    Debug.Log ("Serial: " + d.serial);
                    Debug.Log ("Battery level (%): " + batteryData.batteryLevel);
                }
                else if (d.uuid == Unio.ButtonData.Uuid)
                {
                    Unio.ButtonData buttonData = new Unio.ButtonData (d);
                    Debug.Log ("Serial: " + d.serial);
                    Debug.Log ("Button Id: " + buttonData.buttonId);
                    Debug.Log ("Button state: " + buttonData.buttonState);
                }
                else if (d.uuid == Unio.MotionSensorData.Uuid && d.data[0] == Unio.MotionSensorData.InformationType)
                {
                    Unio.MotionSensorData motionSensorData = new Unio.MotionSensorData (d);
                    Debug.Log ("Serial: " + d.serial);
                    Debug.Log ("Collision detection: " + motionSensorData.collisionDetection);
                    Debug.Log ("Double-tap detection: " + motionSensorData.doubleTapDetection);
                    Debug.Log ("Level detection: " + motionSensorData.levelDetection);
                    Debug.Log ("Posture detection: " + motionSensorData.postureDetection);
                    Debug.Log ("Shake detection: " + motionSensorData.shakeDetection);
                }
                else if (d.uuid == Unio.MagneticSensorData.Uuid && d.data[0] == Unio.MagneticSensorData.InformationType)
                {
                    Unio.MagneticSensorData magneticSensorData = new Unio.MagneticSensorData (d);
                    Debug.Log ("Serial: " + d.serial);
                    Debug.Log ("Magnet status: " + magneticSensorData.magnetStatus);
                    Debug.Log ("Magnetic force strength: " + magneticSensorData.magneticForceStrength);
                    Debug.Log ("Magnetic force direction X: " + magneticSensorData.magneticForceDirectionX);
                    Debug.Log ("Magnetic force direction Y: " + magneticSensorData.magneticForceDirectionY);
                    Debug.Log ("Magnetic force direction Z: " + magneticSensorData.magneticForceDirectionZ);
                }
                else if (d.uuid == Unio.PostureAngleData.Uuid && d.data[0] == Unio.PostureAngleData.InformationType)
                {
                    Unio.PostureAngleData postureAngleData = new Unio.PostureAngleData (d);
                    transform.localRotation = postureAngleData.posture;
                    Debug.Log ("Serial: " + d.serial);
                    Debug.Log ("Posture quaternion: " + postureAngleData.posture);
                }
                else if (d.uuid == Unio.ReadSensorPositionIdData.Uuid && d.data[0] == Unio.ReadSensorPositionIdData.InformationType)
                {
                    Unio.ReadSensorPositionIdData readSensorPositionIdData = new Unio.ReadSensorPositionIdData (d);
                    Debug.Log ("Serial: " + d.serial);
                    Debug.Log ("Cube center X: " + readSensorPositionIdData.cubeCenterX);
                    Debug.Log ("Cube center Y: " + readSensorPositionIdData.cubeCenterY);
                    Debug.Log ("Cube angle: " + readSensorPositionIdData.cubeAngle);
                    Debug.Log ("Read sensor X: " + readSensorPositionIdData.readSensorX);
                    Debug.Log ("Read sensor Y: " + readSensorPositionIdData.readSensorY);
                }
                else if (d.uuid == Unio.ReadSensorStandardIdData.Uuid && d.data[0] == Unio.ReadSensorStandardIdData.InformationType)
                {
                    Unio.ReadSensorStandardIdData readSensorStandardIdData = new Unio.ReadSensorStandardIdData (d);
                    Debug.Log ("Serial: " + d.serial);
                    Debug.Log ("Standard Id: " + readSensorStandardIdData.standardId);
                    Debug.Log ("Cube angle: " + readSensorStandardIdData.cubeAngle);
                }
                else if (d.uuid == Unio.ReadSensorPositionIdMissedData.Uuid && d.data[0] == Unio.ReadSensorPositionIdMissedData.InformationType)
                {
                    Unio.ReadSensorPositionIdMissedData readSensorPositionIdMissedData = new Unio.ReadSensorPositionIdMissedData (d);
                    Debug.Log ("Serial: " + d.serial);
                    Debug.Log ("Position Id missed");
                }
                else if (d.uuid == Unio.ReadSensorStandardIdMissedData.Uuid && d.data[0] == Unio.ReadSensorStandardIdMissedData.InformationType)
                {
                    Unio.ReadSensorStandardIdMissedData readSensorStandardIdMissedData = new Unio.ReadSensorStandardIdMissedData (d);
                    Debug.Log ("Serial: " + d.serial);
                    Debug.Log ("Standard Id missed");
                }
            }
        }

        if (client == null || !client.IsAlive)
            return;
    }

    void OnDestroy ()
    {
        if (client != null)
        {
            client.Close ();
            client = null;
        }
    }

    public void OnButtonClick (UnityEngine.UI.Button sender)
    {
        if (sender == null)
            return;

        if (client == null || !client.IsAlive)
            return;

        if (sender.name == "Connect")
        {
            Unio.ConnectionRequestData d = new Unio.ConnectionRequestData ();
            string s = JsonUtility.ToJson (d);
            Debug.Log (s);
            client.Send (s);
        }

        if (serialNumbers.Count == 0)
            return;
        int sn = serialNumbers.ToList () [0];

        if (sender.name == "Lamp")
        {
            Unio.LampControlData l = new Unio.LampControlData (serialNumber: sn, duration: 0x80, red: 0xff, green: 0x00, blue: 0xff);
            string s = JsonUtility.ToJson (l);
            Debug.Log (s);
            client.Send (s);
        }
        if (sender.name == "MotorRun")
        {
            Unio.MotorControlData md = new Unio.MotorControlData (serialNumber: sn, isLeftForward: true, leftSpeed: 100, isRightForward: true, rightSpeed: 100);
            string s = JsonUtility.ToJson (md);
            Debug.Log (s);
            client.Send (s);
        }
        if (sender.name == "MotorStop")
        {
            Unio.MotorControlData md = new Unio.MotorControlData (serialNumber: sn, isLeftForward: true, leftSpeed: 0, isRightForward: true, rightSpeed: 0);
            string s = JsonUtility.ToJson (md);
            Debug.Log (s);
            client.Send (s);

        }
        if (sender.name == "Posture")
        {
            Unio.PostureAngleRequestData md = new Unio.PostureAngleRequestData (sn, Unio.PostureAngleRequestData.NotificationTypeQuaternion);
            string s = JsonUtility.ToJson (md);
            Debug.Log (s);
            client.Send (s);
        }
        if (sender.name == "Motion")
        {
            Unio.MotionSensorRequestData md = new Unio.MotionSensorRequestData (sn);
            string s = JsonUtility.ToJson (md);
            Debug.Log (s);
            client.Send (s);
        }
        if (sender.name == "Magnet")
        {
            Unio.MagneticSensorRequestData md = new Unio.MagneticSensorRequestData (sn);
            string s = JsonUtility.ToJson (md);
            Debug.Log (s);
            client.Send (s);
        }
        if (sender.name == "SoundEffect")
        {
            Unio.SoundControlData d = new Unio.SoundControlData (serialNumber: sn, controlType: Unio.SoundControlData.ControlTypeSoundEffect, soundEffectId: 0, volume: 0x80);
            string s = JsonUtility.ToJson (d);
            Debug.Log (s);
            client.Send (s);

        }
        if (sender.name == "SoundMidiNote")
        {
            // 72 means C6
            Unio.SoundControlData d = new Unio.SoundControlData (serialNumber: sn, controlType: Unio.SoundControlData.ControlTypeMidiNote, repeatCount: 0x01, duration: 10, midiNoteNumber: 72, volume: 0x80);
            string s = JsonUtility.ToJson (d);
            Debug.Log (s);
            client.Send (s);

        }
    }
}
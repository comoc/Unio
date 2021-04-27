using System;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using WebSocketSharp;
using WebSocketSharp.Net;

namespace Unio
{
    public class NetData
    {
        public int serial;
        public string uuid;
        public int[] data;
        public NetData()
        {
            serial = 0;
            uuid = "";
            data = new int[0];
        }
    }

    public class Data
    {
        public int serial;
        public string uuid;
        public byte[] data;
        public Data()
        {
            serial = 0;
            uuid = "";
            data = new byte[0];
        }
    }


    public class DataConverter
    {
        public static Data TryConvert(NetData rd)
        {
            if (rd == null || rd.uuid == null || rd.uuid == "" || rd.data == null)
                return null;

            Data data = new Data();
            data.serial = rd.serial;
            data.uuid = rd.uuid.ToLower();
            data.data = new byte[rd.data.Length];
            for (int i = 0; i < rd.data.Length; i++)
            {
                if (rd.data[i] < 0)
                    rd.data[i] = 0;
                else if (rd.data[i] > 255)
                    rd.data[i] = 255;

                data.data[i] = (byte)rd.data[i];
            }

            return data;
        }

        public static NetData TryConvert(Data rd)
        {
            if (rd == null || rd.uuid == null || rd.uuid == "" || rd.data == null)
                return null;

            NetData data = new NetData();
            data.serial = rd.serial;
            data.uuid = rd.uuid.ToUpper();
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
        public ConnectionRequestData()
        {
            // Special meaning
            serial = 0;
            uuid = "";
            data = null;
        }
    }

    //
    public class PostureAngleRequestData : Data
    {
        public static readonly string Uuid = "10B20106-5B3B-4571-9508-CF3EFCD7BBAE";
        public PostureAngleRequestData(int serialNumber)
        {
            serial = serialNumber;
            uuid = Uuid;
            data = new byte[2] {0x83, 0x02};
        }
    }

    public class MotorControlData : Data
    {
        public static readonly string Uuid = "10B20102-5B3B-4571-9508-CF3EFCD7BBAE";
        public static readonly int DataLength = 7;

        public static readonly byte Type = 0x01;
        public static readonly byte LeftId = 0x01;
        public static readonly byte RightId = 0x02;

        public MotorControlData(int serialNumber)
        {
            serial = serialNumber;
            uuid = Uuid;
        }

        public MotorControlData(int serialNumber, bool isLeftForward, byte leftSpeed, bool isRightForward, byte rightSpeed)
        {
            serial = serialNumber;
            uuid = Uuid;
            data = new byte[7] {Type,
                LeftId, (byte)(isLeftForward ? 0x01 : 0x02), leftSpeed,
                RightId, (byte)(isRightForward ? 0x01 : 0x02), rightSpeed};
        }
    }

    public class MotionSensorRequestData : Data
    {
        public static readonly  string Uuid = "10B20106-5B3B-4571-9508-CF3EFCD7BBAE";

        public MotionSensorRequestData(int serialNumber)
        {
            serial = serialNumber;
            uuid = Uuid;
            data = new byte[1] { 0x81 };
        }
    }



    public class LedData : Data
    {
        public static readonly  string Uuid = "10B20103-5B3B-4571-9508-CF3EFCD7BBAE";

        public LedData(int serialNumber)
        {
            serial = serialNumber;
            uuid = Uuid;
            // data = new byte[1] { 0x81 };

            /*
            0	UInt8	制御の種類	0x03（点灯・消灯）
1	UInt8	ランプを制御する時間	0x10（160 ミリ秒）
2	UInt8	制御するランプの数	0x01
3	UInt8	制御するランプの ID	0x01
4	UInt8	ランプの Red の値	0xFF
5	UInt8	ランプの Green の値	0x00
6	UInt8	ランプの Blue の値	0x00
*/
            data = new byte[7]
            {
                0x03, 0x80, 0x01, 0x01, 0xff, 0x00, 0xff
            };
        }
    }
}

public class UnioTest : MonoBehaviour
{
    [SerializeField] string webSocketServerAddress = "127.0.0.1";
    [SerializeField] int webSocketServerPort = 12345;
    WebSocket client;

    HashSet<int> serialNumbers = new HashSet<int>();

    private Quaternion localRotation;
    void Start ()
    {
        localRotation = transform.localRotation;

        string serverUrl = "ws://" + webSocketServerAddress + ":" + webSocketServerPort + "/";
        client = new WebSocket(serverUrl);
        client.OnMessage += (sender, e) => {

            Debug.Log("OnMessage: " + e.Data);

            Unio.NetData nd = JsonUtility.FromJson<Unio.NetData>(e.Data);

            if (nd.serial == 0)
                return;
            serialNumbers.Add(nd.serial);

            Unio.Data d = Unio.DataConverter.TryConvert(nd);

            if (nd.uuid == Unio.PostureAngleRequestData.Uuid) // Posture
            {
                int w = BitConverter.ToInt16(d.data, 2);
                int x = BitConverter.ToInt16(d.data, 4);
                int y = BitConverter.ToInt16(d.data, 6);
                int z = BitConverter.ToInt16(d.data, 8);
                float fw = w / 1000f;
                float fx = x / 1000f;
                float fy = y / 1000f;
                float fz = z / 1000f;
                // Convert right-handed to left-handed
                Quaternion qin = new Quaternion(-fy, fz, fx, -fw);
                // Flip around z-axis
                qin.eulerAngles = new Vector3(qin.eulerAngles.x, qin.eulerAngles.y, qin.eulerAngles.z + 180);
                localRotation = qin;
                Debug.Log("Posture quaternion: " + nd.data);
            }

        };

        client.OnOpen += (sender, e) => {

        };

        client.OnClose += (sender, e) => {
            client = null;
        };

        client.Connect();
    }

    void Update()
    {
        transform.localRotation = localRotation;

        if (client == null || !client.IsAlive)
            return;

        if (Input.GetKeyDown(KeyCode.C)) // Connect
        {
            Unio.ConnectionRequestData d = new Unio.ConnectionRequestData();
            string s = JsonUtility.ToJson(d);
            Debug.Log(s);
            client.Send(s);
        }

        if (serialNumbers.Count == 0)
            return;
        int sn = serialNumbers.ToList()[0];

        if (Input.GetKeyDown(KeyCode.G)) // Go forward
        {
            Unio.MotorControlData md = new Unio.MotorControlData(sn, true, 100, true, 100);
            string s = JsonUtility.ToJson(md);
            Debug.Log(s);
            client.Send(s);
        }
        else if (Input.GetKeyUp(KeyCode.G)) // Stop
        {
            Unio.MotorControlData md = new Unio.MotorControlData(sn, true, 0, true, 0);
            string s = JsonUtility.ToJson(md);
            Debug.Log(s);
            client.Send(s);
        }

        if (Input.GetKey(KeyCode.P)) // Posture angle request
        {
            Unio.PostureAngleRequestData md = new Unio.PostureAngleRequestData(sn);
            string s = JsonUtility.ToJson(md);
            Debug.Log(s);
            client.Send(s);
        }

        if (Input.GetKeyDown(KeyCode.M)) // Motion sensor request
        {
            Unio.MotionSensorRequestData md = new Unio.MotionSensorRequestData(sn);
            string s = JsonUtility.ToJson(md);
            Debug.Log(s);
            client.Send(s);
        }

        if (Input.GetKeyDown(KeyCode.L)) // LED control
        {
            Unio.LedData l = new Unio.LedData(sn);
            string s = JsonUtility.ToJson(l);
            Debug.Log(s);
            client.Send(s);
        }
        else if (Input.GetKeyUp(KeyCode.L))
        {

        }
    }

    void OnDestroy()
    {
        if (client != null)
        {
            client.Close();
            client = null;
        }
    }

    public void OnButtonClick(UnityEngine.UI.Button sender)
    {
        if (sender == null)
            return;

        if (client == null || !client.IsAlive)
            return;

        if (sender.name == "Connect")
        {
            Unio.ConnectionRequestData d = new Unio.ConnectionRequestData();
            string s = JsonUtility.ToJson(d);
            Debug.Log(s);
            client.Send(s);

        }

        if (serialNumbers.Count == 0)
            return;
        int sn = serialNumbers.ToList()[0];

        if (sender.name == "LED")
        {
            Unio.LedData l = new Unio.LedData(sn);
            string s = JsonUtility.ToJson(l);
            Debug.Log(s);
            client.Send(s);
        }
        if (sender.name == "MotorRun")
        {
            Unio.MotorControlData md = new Unio.MotorControlData(sn, true, 100, true, 100);
            string s = JsonUtility.ToJson(md);
            Debug.Log(s);
            client.Send(s);
        }
        if (sender.name == "MotorStop")
        {
            Unio.MotorControlData md = new Unio.MotorControlData(sn, true, 0, true, 0);
            string s = JsonUtility.ToJson(md);
            Debug.Log(s);
            client.Send(s);

        }
        if (sender.name == "Posture")
        {
            Unio.PostureAngleRequestData md = new Unio.PostureAngleRequestData(sn);
            string s = JsonUtility.ToJson(md);
            Debug.Log(s);
            client.Send(s);
        }
        if (sender.name == "Motion")
        {
            Unio.MotionSensorRequestData md = new Unio.MotionSensorRequestData(sn);
            string s = JsonUtility.ToJson(md);
            Debug.Log(s);
            client.Send(s);
        }
    }
}

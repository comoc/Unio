using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using WebSocketSharp;
using WebSocketSharp.Server;

// Note
// Dealing with "Must use package reference" error
// https://ameblo.jp/tubutappuri-san/entry-12598949900.html

// References
// https://so-zou.jp/software/tech/programming/c-sharp/thread/thread-safe-call.htm
// https://ameblo.jp/tubutappuri-san/entry-12598949900.html
// https://qiita.com/ta-yamaoka/items/a7ff1d9651310ade4e76
// https://qiita.com/FumiyaHr/items/13de3dcbd9b81d9d27f0
// https://qiita.com/riyosy/items/5789ccdeee644b34a743
namespace Unio
{
    class Program
    {
        static void Main(string[] args)
        {
            MainAsync().Wait();
        }

        private static async Task MainAsync()
        {
            var cancellationTokenSource = new CancellationTokenSource();
            CancellationToken cancellationToken = cancellationTokenSource.Token;


            var task = Task.Run(() =>
            {
                cancellationToken.ThrowIfCancellationRequested();

                const int WebSocketServerPort = 12345;
                WebSocketServer server = new WebSocketServer(WebSocketServerPort);
                server.AddWebSocketService<ExWebSocketBehavior>("/");
                server.Start();

                bool moreToDo = true;
                while (moreToDo)
                {
 
                    if (cancellationToken.IsCancellationRequested)
                    {
                        cancellationToken.ThrowIfCancellationRequested();
                    }

                    Console.WriteLine("Press any key to exit");
                    Console.ReadKey();
                    cancellationTokenSource.Cancel();

                    Task.Yield();
                }

                if (server != null)
                    server.Stop();


            }, cancellationTokenSource.Token);


            try
            {
                await task;
            }
            catch (OperationCanceledException e)
            {
                Console.WriteLine($"{nameof(OperationCanceledException)} thrown with message: {e.Message}");
            }
            finally
            {
                cancellationTokenSource.Dispose();
            }
        }
    }


    public class ExWebSocketBehavior : WebSocketBehavior
    {
        public static List<ExWebSocketBehavior> clientList = new List<ExWebSocketBehavior>();
        static int globalSeq = 0;
        int seq;

        protected override void OnOpen()
        {
            globalSeq++;
            this.seq = globalSeq;
            clientList.Add(this);
            // Console.WriteLine("Seq" + this.seq + " Login. (" + this.ID + ")");

            string msg = "{\"unio\":\"connected\"}";
            Send(msg);

            for (int i = 0; i < Unio.ToioManager.Instance.GetToioCount(); i++)
            {
                Toio toio = Unio.ToioManager.Instance.GetToio(i);
                NetData nd = new NetData();
                nd.serial = toio.SerialNumber;
                string json = JsonConvert.SerializeObject(nd);
                Sessions.Broadcast(json);
            }
        }

        protected override void OnMessage(MessageEventArgs e)
        {
            Console.WriteLine("Message from client: " + e.Data);
            if (e.Data.Length > 0 && e.Data[0] == '{')
            {
                try
                {
                    Unio.NetData netData = JsonConvert.DeserializeObject<Unio.NetData>(e.Data);
                    if (netData != null)
                    {
                        if (netData.uuid == "" && (netData.data == null || netData.data.Length == 0)) // Connect
                        {
                            Unio.ToioManager.Instance.Search(3000, NewToioFound);
                            return;
                        }
                        Unio.Data data = Unio.DataConverter.Convert(netData);
                        if (data != null)
                        {
                            for (int i = 0; i < Unio.ToioManager.Instance.GetToioCount(); i++)
                            {
                                Unio.Toio toio = Unio.ToioManager.Instance.GetToio(i);
                                if (toio.SerialNumber == data.serial)
                                    toio.Write(data.uuid, data.data);
                            }
                            Console.WriteLine(data.uuid);
                        }
                    }
                }
                catch (Exception ex)
                {
                    Console.WriteLine(ex.ToString());
                }
            }
        }

        protected override void OnClose(CloseEventArgs e)
        {
            string msg = "{\"unio\":\"close\"}";
            Send(msg);
        }

        private void NewToioFound(Unio.Toio toio)
        {
            string msg = $"Toio count: {Unio.ToioManager.Instance.GetToioCount()}";
            //SetText(msg);
            Console.WriteLine(msg);

            NetData nd = new NetData();
            nd.serial = toio.SerialNumber;
            string json = JsonConvert.SerializeObject(nd);
            Sessions.Broadcast(json);

            toio.onValueChanged += OnValueChanged;
        }

        private void OnValueChanged(int serial, string uuid, byte[] data)
        {
            Unio.Data d = new Unio.Data();
            d.serial = serial;
            d.uuid = uuid;
            d.data = data;
            Unio.NetData rd = Unio.DataConverter.Convert(d);
            string json = JsonConvert.SerializeObject(rd);

            Console.WriteLine("OnValueChanged: " + json);
            Sessions.Broadcast(json);
        }
    }
}

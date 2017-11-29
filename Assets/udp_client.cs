using UnityEngine;
using System.Collections;
using System.Net;
using System.Net.Sockets;
using System.Text;
using UnityEngine.UI;
using System.Threading;
using System.IO;

public class udp_client : MonoBehaviour {
    static Vector3 current_position;
    static readonly object locker = new object();
    Thread m_ListeningThread;


    //
    public GameObject sphere;
    //public Vector3[] calibration_points;
    //

    // Use this for initialization
    public void Start () {
        current_position = new Vector3(0f, 0.01f, 0f);
        m_ListeningThread = new Thread(client);
        m_ListeningThread.Start();

        //
        //calibration_points = new Vector3[]{new Vector3(0.0f, 0.01f, 0.0f),
        //                                    new Vector3(0.5f, 0.01f, -0.3f),
        //                                    new Vector3(-0.5f, 0.01f, -0.3f),
        //                                    new Vector3(-0.5f, 0.01f, 0.3f),
        //                                    new Vector3(0.5f, 0.01f, 0.3f)};
        //point = 0;
        //sphere.transform.localPosition = calibration_points[0];
        //
    }
    //public void next_point()
    //{
    //    point++;
    //    if (point < calibration_points.Length)
    //    {
    //        Debug.Log("Proximo punto");
    //        Debug.Log(point);
    //        Debug.Log(calibration_points[point]);
    //        sphere.transform.localPosition = calibration_points[point];
    //        Debug.Log(sphere.transform.position.x);
    //    }
    //}

    public void OnClick()
    {
        m_ListeningThread.Join();
    }

    public void read()
    {
        lock (locker)
        {
            Debug.Log("coordinates");
            Debug.Log(current_position.x);
            Debug.Log(current_position.y);
            Debug.Log(current_position.z);
        }
    }

    void client()
    {
        Pair ipep_server = set_client("10.0.0.1", 9050);
        IPEndPoint ipep = ipep_server.ipep;
        Socket server = ipep_server.socket;

        establish_connection(ipep, server);
        var coordinates = new Vector3[] { new Vector3(1.0f, 0.0f, 1.0f),
                                          new Vector3(2.0f, 0.0f, 2.0f),
                                          new Vector3(3.0f, 0.0f, 3.0f)};

        send_coordinates(ipep, server, coordinates);
        receive_coordinates(server);
        
    }

    public void receive_coordinates(Socket server)
    {
        while (true)
        {
            IPEndPoint sender = new IPEndPoint(IPAddress.Any, 0);
            EndPoint Remote = (EndPoint)sender;

            var data = new byte[1024];
            int recv = server.ReceiveFrom(data, ref Remote);
            var m = new MemoryStream(data);
            var br = new BinaryReader(m);
            float x = br.ReadSingle();
            float y = br.ReadSingle();
            float z = br.ReadSingle();
            br.Close();
            lock (locker)
            {
                current_position.x = x;
                current_position.y = y;
                current_position.z = z;
            }
        }
    }

    public Pair set_client(string host, int port)
    {
        IPEndPoint ipep = new IPEndPoint(
                        IPAddress.Parse(host), port);

        Socket server = new Socket(AddressFamily.InterNetwork,
                       SocketType.Dgram, ProtocolType.Udp);
        return new Pair(ipep, server);
    }

    static void establish_connection(IPEndPoint ipep, Socket server)
    {
        while (true)
        {
            var m = new MemoryStream();
            var bw = new BinaryWriter(m);
            bw.Write(0);
            bw.Flush();
            var data = m.ToArray();
            server.SendTo(data, data.Length, SocketFlags.None, ipep);


            ArrayList listenList = new ArrayList();
            listenList.Add(server);
            Socket.Select(listenList, null, null, (int)1e6);

            if (listenList.Count == 0)
            {
                continue;
            }

            IPEndPoint sender = new IPEndPoint(IPAddress.Any, 0);
            EndPoint Remote = (EndPoint)sender;

            data = new byte[1024];
            int recv = server.ReceiveFrom(data, ref Remote);
            m = new MemoryStream(data);
            var br = new BinaryReader(m);
            if (br.ReadInt32() != 0) { br.Close(); continue; }
            break;
        }

    }

    void send_coordinates(IPEndPoint ipep, Socket server, Vector3[] coordinates)
    {
        int i = 1;
        while (i <= coordinates.Length)
        {
            var m = new MemoryStream();
            var bw = new BinaryWriter(m);
            bw.Write(i);
            bw.Write(coordinates[i - 1].x);
            bw.Write(coordinates[i - 1].y);
            bw.Write(coordinates[i - 1].z);
            bw.Flush();
            lock (locker)
            {
                current_position.x = coordinates[i - 1].x;
                current_position.y = coordinates[i - 1].y;
                current_position.z = coordinates[i - 1].z;
            }
            read();
            var data = m.ToArray();
            server.SendTo(data, data.Length, SocketFlags.None, ipep);
            i++;
            ArrayList listenList = new ArrayList();
            listenList.Add(server);
            Socket.Select(listenList, null, null, (int)1e6);

            if (listenList.Count == 0 && i< coordinates.Length)
            {
                Debug.Log("no escucho nada");
                continue;
            }

            IPEndPoint sender = new IPEndPoint(IPAddress.Any, 0);
            EndPoint Remote = (EndPoint)sender;

            data = new byte[1024];
            int recv = server.ReceiveFrom(data, ref Remote);
            m = new MemoryStream(data);
            var br = new BinaryReader(m);
            var msg = br.ReadInt32();
            if (msg != 1) { continue; }
            
        }
    }

        // Update is called once per frame
    void Update () {
        
    }
    void FixedUpdate()
    {
        sphere.transform.localPosition = current_position;
    }

    
}

public class Pair
{
    public IPEndPoint ipep;
    public Socket socket;

    public Pair(IPEndPoint ipep, Socket socket)
    {
        this.ipep = ipep;
        this.socket = socket;
    }
}

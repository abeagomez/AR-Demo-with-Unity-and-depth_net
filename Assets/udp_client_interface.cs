using UnityEngine;
using System.Collections;
using System.Net;
using System.Net.Sockets;
using System.Text;
using UnityEngine.UI;
using System.Threading;
using System.IO;

public class udp_client_interface : MonoBehaviour {
    static Vector3 current_position;
    static Socket listener;
    static readonly object locker = new object();
    Thread m_ListeningThread;
    static IPEndPoint addr;
    public GameObject sphere;
    public InputField port;
    public InputField host;
    //public static Text system_info;

    int current_calibration_point;

    // Use this for initialization
    public void Start()
    {
        current_position = new Vector3(0f, 0.01f, 0f);

        host.text = PlayerPrefs.GetString("host");
        port.text = PlayerPrefs.GetString("port");

        //m_ListeningThread = new Thread(client);
        //m_ListeningThread.Start();
    }

    public void OnExitClick()
    {
        lock (locker)
        {
            if (listener != null)
                listener.Close();
        }
    }

    void read()
    {
        lock (locker)
        {
            Debug.Log("coordinates");
            Debug.Log(current_position.x.ToString() + "," + current_position.y.ToString() + "," + current_position.z.ToString());
        }
    }

    public void start_client()
    {
        var h = host.text;
        var p = port.text;

        PlayerPrefs.SetString("host", h);
        PlayerPrefs.SetString("port", p);

        addr = new IPEndPoint(
                        IPAddress.Parse(h), int.Parse(p));
        listener = new Socket(AddressFamily.InterNetwork,
                       SocketType.Dgram, ProtocolType.Udp);
        
        m_ListeningThread = new Thread(set_client_up);
        Debug.Log("Se crea el nuevo hilo y ahora se va a empezar a usar");
        m_ListeningThread.Start();
    }

    //Este método llama a set_client dentro de un nuevo hilo
    void set_client_up()
    {
        //Pair ipep_server = set_client(h, int.Parse(p));
        //lock (locker)
        //{
        //    listener = ipep_server.socket;
        //}
        UnityEngine.Debug.Log("Las variables están listas para empezar la calibracion");
        establish_connection(addr);
        //establish_connection(ipep_server.ipep);
        //lock (locker)
        //{
        //    listener.Close();
        //}
        m_ListeningThread.Abort();
    }

    public void receive_coordinates_button()
    {
        m_ListeningThread = new Thread(receive_coordinates_up);
        m_ListeningThread.Start();
    }

    void printList(string name, ArrayList l)
    {
        UnityEngine.Debug.Log(name + ":");
        for (int i = 0; i < l.Count; i++)
        {
            UnityEngine.Debug.Log("  " + l[i].ToString());
        }
    }

    void printBytes(byte[] data, int count)
    {
        string s = "[";
        for (int i = 0; i < count; i++)
        {
            s += data[i].ToString();
            if (i < (count))
            {
                s += ", ";
            }
        }
        s += "]";
        UnityEngine.Debug.Log(s);
    }

    //Este método manda la confirmación de que se han recibido todos
    //los puntos y llama al que comienza a recibir coordenadas en
    //cuanto vea que le están mandando algo.
    void receive_coordinates_up()
    {
        //var h = host.text;
        //var p = port.text;
        //Pair ipep_server = set_client(h, int.Parse(p));
        //lock (locker)
        //{
        //    listener = ipep_server.socket;
        //}

        while (true) {

            //Mandando confirmacion del ultimo punto
            //El identificador de esta confirmación es 2
            var m = new MemoryStream();
            var bw = new BinaryWriter(m);
            bw.Write(2);
            bw.Flush();
            var data = m.ToArray();
            lock (locker)
            {
                //listener.SendTo(data, data.Length, SocketFlags.None, ipep_server.ipep);
                listener.SendTo(data, data.Length, SocketFlags.None, addr);
            }
            UnityEngine.Debug.Log("Se mandó un 2");

            ArrayList listenList = new ArrayList();
            lock (locker)
            {
                UnityEngine.Debug.Log("Se agregó el socket a la lista que recibe select");
                listenList.Add(listener);
            }
            Socket.Select(listenList, null, null, 1000000); // espera 1 segundo como máximo

            printList("listenList", listenList);
            
            UnityEngine.Debug.Log("Select retorno");
            if (listenList.Count == 0)
            {
                UnityEngine.Debug.Log("no se ha recibido confirmacion del servidor para el 2");
                continue;
            }

            //EndPoint Remote = (EndPoint)ipep_server.ipep;
            EndPoint Remote = (EndPoint)addr;

            data = new byte[1024];
            lock (locker)
            {
                int recv = listener.ReceiveFrom(data, ref Remote);
            }

            m = new MemoryStream(data);
            var br = new BinaryReader(m);
            // Los trios de coordenadas que se reciben comienzan con 
            // 3 como identificador.
            int identifier = br.ReadInt32();
            UnityEngine.Debug.Log("Se recibió un " + identifier.ToString());
            if (identifier == 3) break;
        }
        receive_coordinates();
    }

    //void client()
    //{
    //    Pair ipep_server = set_client("10.0.0.1", 9050);
    //    IPEndPoint ipep = ipep_server.ipep;
    //    Socket server = ipep_server.socket;

    //    establish_connection(ipep, server);
    //    var coordinates = new Vector3[] { new Vector3(1.0f, 0.0f, 1.0f),
    //                                      new Vector3(2.0f, 0.0f, 2.0f),
    //                                      new Vector3(3.0f, 0.0f, 3.0f)};

    //    send_coordinates(ipep, server, coordinates);
    //    //receive_coordinates(server);
    //}

    //Este metodo recibe ininterrumpidamente las coordenadas que envia 
    //el servidor y las asigna como poscion al modelo
    //no es necesario corregir el caso en el que python no detecta nada
    //porque en ese caso el servidor no envía.
    void receive_coordinates()
    {
        while (true)
        {
            ArrayList listenList = new ArrayList();
            lock (locker)
            {
                listenList.Add(listener);
            }
            Socket.Select(listenList, null, null, (int)1e6);

            if (listenList.Count == 0)
            {
                UnityEngine.Debug.Log("no se ha recibido ningun punto del servidor");
                continue;
            }

            IPEndPoint sender = new IPEndPoint(IPAddress.Any, 0);
            EndPoint Remote = (EndPoint)sender;

            var data = new byte[1024];
            lock (locker)
            {
                int recv = listener.ReceiveFrom(data, ref Remote);
            }
            var m = new MemoryStream(data);
            var br = new BinaryReader(m);
            int identifier = br.ReadInt32();
            float x = br.ReadSingle();
            float y = br.ReadSingle();
            float z = br.ReadSingle();
            if (identifier != 3) continue;
            br.Close();
            lock (locker)
            {
                current_position.x = x;
                current_position.y = y;
                current_position.z = z;
            }
            read();
        }
    }

    //Este método inicializa variables necesarias para levantar
    //el cliente UDP
    public Pair set_client(string host, int port)
    {
        IPEndPoint ipep = new IPEndPoint(
                        IPAddress.Parse(host), port);

        Socket server = new Socket(AddressFamily.InterNetwork,
                       SocketType.Dgram, ProtocolType.Udp);
        return new Pair(ipep, server);
    }

    //Este metodo envia una señal al servidor y espera su respuesta. Si la respuesta es correcta,
    //el metodo termina, en caso contrario, continua enviando la señal hasta obtener la respuesta
    //esperada.
    public void establish_connection(IPEndPoint ipep)
    {
        while (true)
        {
            var m = new MemoryStream();
            var bw = new BinaryWriter(m);
            bw.Write(0);
            bw.Flush();
            var data = m.ToArray();
            lock (locker)
            {
                listener.SendTo(data, data.Length, SocketFlags.None, ipep);
            }
            UnityEngine.Debug.Log("se envía la primera señal al servidor");

            ArrayList listenList = new ArrayList();
            lock (locker)
            {
                listenList.Add(listener);
            }
            Socket.Select(listenList, null, null, (int)1e6);

            if (listenList.Count == 0)
            {
                continue;
            }
            
            EndPoint Remote = (EndPoint)ipep;

            data = new byte[1024];
            lock (locker)
            {
                int recv = listener.ReceiveFrom(data, ref Remote);
            }
            m = new MemoryStream(data);
            var br = new BinaryReader(m);

            if (br.ReadInt32() != 0) {
                UnityEngine.Debug.Log("El servidor no envió la respuesta esperada");
                br.Close(); continue;
            }
            UnityEngine.Debug.Log("El servidor respondio con exito. Listo todo para comenzar la calibracion");
            break;
        }

    }

    public void send_coordinates_button() {
        m_ListeningThread = new Thread(set_coordinates_sending_up);
        m_ListeningThread.Start();
    }

    void set_coordinates_sending_up()
    {
        //var h = host.text;
        //var p = port.text;
        //Pair ipep_server = set_client(h, int.Parse(p));
        //lock (locker)
        //{
        //    listener = ipep_server.socket;
        //}
        //send_current_marker_coordintes(ipep_server.ipep);
        send_current_marker_coordintes(addr);
        //lock (locker)
        //{
        //    listener.Close();
        //}
        //m_ListeningThread.Abort();
    }

    //Este método manda las coordenadas actuales de calibración y espera a que el servidor
    //responda que las ha recibido. Cuando el servidor las recibe, el método aumenta el índice
    //(current_calibration_point que es la posición actual en el array de puntos de calibración)
    //de las coordenadas y termina.
    public void send_current_marker_coordintes(IPEndPoint ipep)
    {
        var coordinates = new Vector3[] { new Vector3(0.0f, 0.01f, 0.0f),
                                          new Vector3(0.5f, 0.01f, 0.3f),
                                          new Vector3(0.5f, 0.01f, -0.3f),
                                          new Vector3(-0.5f, 0.01f, 0.3f),
                                          new Vector3(-0.5f, 0.01f, -0.3f),
                                          new Vector3(0.0f, 0.3f, 0.0f),
                                          new Vector3(0.5f, 0.3f, 0.3f),
                                          new Vector3(0.5f, 0.3f, -0.3f),
                                          new Vector3(-0.5f, 0.3f, 0.3f),
                                          new Vector3(-0.5f, 0.3f, -0.3f)};
        while (true){
            if(current_calibration_point >= coordinates.Length) {
                UnityEngine.Debug.Log("Se ha terminado la calibración");
                break; }
            //El formato de un punto de calibracion es:
            //Identificador del mensaje (1), número del punto de calibración
            //(comenzando por 0), f,f,f (coordenadas del punto)
            var m = new MemoryStream();
            var bw = new BinaryWriter(m);
            bw.Write(1);
            bw.Write(current_calibration_point);
            bw.Write(coordinates[current_calibration_point].x);
            bw.Write(coordinates[current_calibration_point].y);
            bw.Write(coordinates[current_calibration_point].z);
            bw.Flush();

            //system_info.text = "se está enviando el punto numero " + current_calibration_point.ToString();
            Debug.Log("se está enviando el punto numero " + current_calibration_point.ToString());

            lock (locker)
            {
                current_position.x = coordinates[current_calibration_point].x;
                current_position.y = coordinates[current_calibration_point].y;
                current_position.z = coordinates[current_calibration_point].z;
            }


            read();
            var data = m.ToArray();
            lock (locker)
            {
                listener.SendTo(data, data.Length, SocketFlags.None, ipep);
            }

            ArrayList listenList = new ArrayList();
            lock (locker)
            {
                listenList.Add(listener);
            }
            Socket.Select(listenList, null, null, (int)1e6);

            if (listenList.Count == 0)
            {
                UnityEngine.Debug.Log("no se ha recibido confirmacion del servidor en el recibo de la coordenada " + current_calibration_point.ToString());
                continue;
            }
            
            EndPoint Remote = (EndPoint)ipep;

            data = new byte[1024];
            lock (locker) {
                int recv = listener.ReceiveFrom(data, ref Remote);
                UnityEngine.Debug.Log("Recibi " + recv.ToString() + " bytes");
                printBytes(data, recv);
            }
            //El formato del mensaje de confirmación es:
            //Identificador del mensaje (1), número del punto que se confirma (debe coincidir con 
            //el último punto que se mandó).
            m = new MemoryStream(data);
            var br = new BinaryReader(m);
            var msg = br.ReadInt32();
            var point = br.ReadInt32();
            if (msg != 1 || point != current_calibration_point) { continue; }
            UnityEngine.Debug.Log("Se ha recibido confirmación del punto " + current_calibration_point.ToString());
            lock (locker) {
                current_calibration_point++;
            }
            break;

        }
    }

    //Este método lo sustituí por send_current_marker_coordinates
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

            if (listenList.Count == 0 && i < coordinates.Length)
            {
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
    void Update()
    {

    }
    void FixedUpdate()
    {
        sphere.transform.localPosition = current_position;
    }


}


using System.Text;
using UnityEngine;
using UnityEngine.Networking;
using UnityEngine.Events;

#pragma warning disable CS0618 // Unity generates a warning for every networking call, as they have deprecated the old networking system, but have yet to make a new one.
                               // So to use this system without generating a load of warnings, I have to disable the CS0618 warning entirely in this script

public class NetworkManager : MonoBehaviour
{
    [Header("Network Information")]
    [Tooltip("The address of the server to connect to")]
    public string RemoteAddress;
    [Tooltip("The port that the connection should be run on")]
    public int RemotePort;

    private int ConnectionID;
    private int HostID;
    private int ChannelID;

    [HideInInspector]
    public bool IsInitialised = false; //Stops the script trying to recieve packets when the socket hasnt been opened yet

    [Header("Events")]
    [Tooltip("This event will be called whenever the connection is made, either by the client or by the server")]
    public UnityEvent ConnectionEvent;
    [Tooltip("This event is called whenever a packet is received, and contains the data received")]
    public StringEvent MessageEvent;
    [Tooltip("This event is called whenever a packet is sent")]
    public UnityEvent SendEvent;
    [Tooltip("This event will be called whenever the connection is dropped, either by the client or by the server")]
    public UnityEvent DisconnectionEvent;
    

    public void Host()
    {
        NetworkServer.Reset(); //Reset all the connections to make sure the port is free

        NetworkTransport.Init(); //Initialise the networking stack

        ConnectionConfig config = new ConnectionConfig(); //Create a blank config
        ChannelID = config.AddChannel(QosType.Reliable); //Add a single reliable network channel to the config

        HostTopology topology = new HostTopology(config, 1); //Create a network topology from this network config (1 simultaneous connection)
        HostID = NetworkTransport.AddHost(topology, RemotePort); //Start hosting that network topology on the specified port

        IsInitialised = true;
        print("Hosting connection on port " + RemotePort + ", ready for connections");
    }

    //connect method
    public void Connect()
    {
        NetworkServer.Reset(); //Reset all the connections to make sure the port is free
        NetworkTransport.Init(); //Initialise the networking stack

        ConnectionConfig config = new ConnectionConfig(); //Create a blank config
        ChannelID = config.AddChannel(QosType.Reliable); //Add a single reliable network channel to the config

        HostTopology topology = new HostTopology(config, 1); //Create a network topology from this network config (1 simultaneous connection)
        HostID = NetworkTransport.AddHost(topology); //Start hosting that network topology on a free port

        byte error;
        ConnectionID = NetworkTransport.Connect(HostID, RemoteAddress, RemotePort, 0, out error); //Attempt to connect with the server on its addess and port

        if ((NetworkError)error == NetworkError.Ok)
        {
            Debug.Log("Connection was successful");
        }
        else
        {
            Debug.LogWarning("Error on connecting: " + (NetworkError)error);
        }

        IsInitialised = true;
    }

    public void Update()
    {
        if (IsInitialised)
        {
            int recHostId;
            int connectionId;
            int channelId;
            byte[] recBuffer = new byte[1024];
            int bufferSize = 1024;
            int dataSize;
            byte error;
            NetworkEventType eventType = NetworkTransport.Receive(out recHostId, out connectionId, out channelId, recBuffer, bufferSize, out dataSize, out error); //Attempt to recieve data
            
            switch (eventType)
            {
                case NetworkEventType.Nothing:
                    break;

                case NetworkEventType.ConnectEvent:
                    Debug.Log("Connection Created");
                    ConnectionID = connectionId;
                    ConnectionEvent.Invoke();
                    break;

                case NetworkEventType.DataEvent:
                    Debug.Log("Data Received");
                    Receive(recBuffer);
                    break;

                case NetworkEventType.DisconnectEvent:
                    Debug.Log("Disconnected from socket");
                    DisconnectionEvent.Invoke();
                    break;

                case NetworkEventType.BroadcastEvent:
                    Debug.Log("Broadcast event");
                    break;
            }
        }
    }

    //send string
    public void Send(string message)
    {
        if (IsInitialised)
        {
            byte error;
            byte[] buffer = Encoding.Default.GetBytes(message); //Encode a string as bytes to be sent

            //Send the message from the "client" with the serialized message and the connection information
            NetworkTransport.Send(HostID, ConnectionID, ChannelID, buffer, buffer.Length, out error);

            //If there is an error, output message error to the console
            if ((NetworkError)error != NetworkError.Ok)
            {
                Debug.LogWarning("Error on message send: " + (NetworkError)error);
            }
            SendEvent.Invoke();
        }
    }

    //receive string
    public void Receive(byte[] buffer)
    {
       MessageEvent.Invoke(Encoding.Default.GetString(buffer));
    }


    public void Disconnect()
    {
        byte error;
        NetworkTransport.Disconnect(HostID, ConnectionID, out error);

        if ((NetworkError)error != NetworkError.Ok)
        {
            Debug.LogWarning("Error on disconnecting: " + (NetworkError)error);
        }
    }
}

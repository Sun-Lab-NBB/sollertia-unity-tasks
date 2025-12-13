/// <summary>
/// Provides the MQTTClient class for managing connectivity with the MQTT broker.
///
/// Handles connection establishment, topic subscription, and message routing for
/// bidirectional communication between Unity and external systems like sl-experiment.
/// </summary>
using System;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Text;
using System.Threading.Tasks;
using UnityEngine;
using uPLibrary.Networking.M2Mqtt;
using uPLibrary.Networking.M2Mqtt.Messages;

namespace Gimbl
{
    /// <summary>
    /// Manages the MQTT broker connection and routes messages to subscribed channels.
    /// </summary>
    /// <remarks>
    /// This MonoBehaviour should be attached to a GameObject named "MQTT Client" in the scene.
    /// Connection settings (IP and port) are loaded from Unity EditorPrefs.
    /// </remarks>
    public class MQTTClient : MonoBehaviour
    {
        /// <summary>The IP address of the MQTT broker.</summary>
        [HideInInspector]
        public string ip;

        /// <summary>The port number of the MQTT broker.</summary>
        [HideInInspector]
        public int port;

        /// <summary>The underlying M2Mqtt client instance.</summary>
        public MqttClient client;

        /// <summary>The internal class that maps topics to their corresponding channel handlers.</summary>
        private class Channel
        {
            public string topic;
            public MQTTChannel channel;
        }

        /// <summary>The list of all subscribed channels for message routing.</summary>
        private List<Channel> _channelList = new List<Channel>();

        /// <summary>The channel for broadcasting session start events.</summary>
        private MQTTChannel _startChannel;

        /// <summary>The channel for broadcasting session stop events.</summary>
        private MQTTChannel _stopChannel;

        /// <summary>Initializes connection settings and sets up session channels on start.</summary>
        void Start()
        {
            // Loads connection settings from EditorPrefs
            ip = UnityEditor.EditorPrefs.GetString("JaneliaVR_MQTT_IP");
            port = UnityEditor.EditorPrefs.GetInt("JaneliaVR_MQTT_Port");

            // Subscribes to standard session channels
            _startChannel = new MQTTChannel("Gimbl/Session/Start", false);
            _stopChannel = new MQTTChannel("Gimbl/Session/Stop", false);
            StartSession();
        }

        /// <summary>Sends the session start message after a brief delay.</summary>
        private async void StartSession()
        {
            await Task.Delay(1000);
            _startChannel.Send();
        }

        /// <summary>Sends session stop message and cleans up subscriptions on application quit.</summary>
        void OnApplicationQuit()
        {
            _stopChannel.Send();

            if (_channelList.Count > 0)
            {
                client.Unsubscribe(_channelList.Select(x => x.topic).ToArray());
            }

            _channelList = new List<Channel>();
        }

        /// <summary>Establishes a connection to the MQTT broker.</summary>
        /// <param name="verbose">If true, logs successful connection to the console.</param>
        public void Connect(bool verbose)
        {
            IPAddress ipAddress = IPAddress.Parse(ip);

#pragma warning disable 618
            client = new MqttClient(ipAddress, port, false, null, null, MqttSslProtocols.None);
#pragma warning restore 618

            // Runs connection in a task with timeout to avoid blocking
            Task t = Task.Run(() =>
            {
                byte msg = client.Connect(Guid.NewGuid().ToString());
                MqttMsgConnack connack = new MqttMsgConnack();
                connack.GetBytes(msg);

                client.MqttMsgPublishReceived += ReceivedMessage;

                if (verbose)
                {
                    UnityEngine.Debug.Log($"Successfully connected to MQTT Broker at: {ip}:{port}");
                }
            });

            TimeSpan timeout = TimeSpan.FromMilliseconds(1000);
            if (!t.Wait(timeout))
            {
                UnityEngine.Debug.LogError($"Could not connect to MQTT broker at {ip}:{port}");
            }
        }

        /// <summary>Disconnects from the MQTT broker if currently connected.</summary>
        public void Disconnect()
        {
            if (IsConnected())
            {
                client.Disconnect();
            }
        }

        /// <summary>Checks whether the client is currently connected to the broker.</summary>
        /// <returns>True if connected, false otherwise.</returns>
        public bool IsConnected()
        {
            bool isConnected = false;
            try
            {
                isConnected = client.IsConnected;
            }
            catch
            {
                // Handles connection check failure
            }
            return isConnected;
        }

        /// <summary>Subscribes a channel to receive messages on the specified topic.</summary>
        /// <param name="obj">The MQTTChannel to receive messages.</param>
        /// <param name="topic">The MQTT topic to subscribe to.</param>
        /// <param name="qosLevel">The Quality of Service level for the subscription.</param>
        public void Subscribe(MQTTChannel obj, string topic, byte qosLevel)
        {
            if (IsConnected())
            {
                client.Subscribe(new string[] { topic }, new byte[] { qosLevel });

                lock (_channelList)
                {
                    _channelList.Add(new Channel() { topic = topic, channel = obj });
                }
            }
        }

        /// <summary>Routes received messages to the appropriate subscribed channels.</summary>
        /// <param name="sender">The event sender.</param>
        /// <param name="e">The message event arguments containing topic and payload.</param>
        public void ReceivedMessage(object sender, MqttMsgPublishEventArgs e)
        {
            lock (_channelList)
            {
                foreach (Channel chn in _channelList)
                {
                    if (string.Equals(e.Topic, chn.topic))
                    {
                        chn.channel.ReceivedMessage(Encoding.UTF8.GetString(e.Message));
                    }
                }
            }
        }
    }
}

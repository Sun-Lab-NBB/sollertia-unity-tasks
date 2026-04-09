/// <summary>
/// Provides the MQTTClient class for managing connectivity with the MQTT broker.
///
/// Handles connection establishment, topic subscription, and message routing for
/// bidirectional communication between Unity and external systems like sl-experiment.
/// </summary>
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using MQTTnet;
using MQTTnet.Client;
using MQTTnet.Protocol;
using UnityEngine;

namespace Gimbl
{
    /// <summary>
    /// Manages the MQTT broker connection and routes messages to subscribed channels.
    /// </summary>
    /// <remarks>
    /// This MonoBehaviour should be attached to a GameObject named "MQTT Client" in the scene.
    /// Connection settings (IP and port) are loaded from Unity EditorPrefs.
    /// Access via the static Instance property instead of GameObject.Find().
    /// </remarks>
    public class MQTTClient : MonoBehaviour
    {
        /// <summary>The singleton instance of the MQTTClient.</summary>
        public static MQTTClient Instance { get; private set; }

        /// <summary>The IP address of the MQTT broker.</summary>
        [HideInInspector]
        public string ip;

        /// <summary>The port number of the MQTT broker.</summary>
        [HideInInspector]
        public int port;

        /// <summary>The underlying MQTTnet client instance.</summary>
        public IMqttClient client;

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

        /// <summary>Registers this instance as the singleton on awake.</summary>
        void Awake()
        {
            if (Instance != null && Instance != this)
            {
                Debug.LogWarning("MQTTClient: Multiple instances found, using existing instance");
                return;
            }
            Instance = this;
        }

        /// <summary>Initializes connection settings and sets up session channels on start.</summary>
        void Start()
        {
            // Loads connection settings from EditorPrefs
            ip = UnityEditor.EditorPrefs.GetString("SollertiaVR_MQTT_IP");
            port = UnityEditor.EditorPrefs.GetInt("SollertiaVR_MQTT_Port");

            // Subscribes to standard session channels
            _startChannel = new MQTTChannel("Gimbl/Session/Start", false);
            _stopChannel = new MQTTChannel("Gimbl/Session/Stop", false);
            StartSession();
        }

        /// <summary>Sends the session start message after a brief delay.</summary>
        private async void StartSession()
        {
            try
            {
                await Task.Delay(1000);
                _startChannel.Send();
            }
            catch (Exception ex)
            {
                Debug.LogError($"MQTTClient.StartSession failed: {ex.Message}");
            }
        }

        /// <summary>Sends session stop message and cleans up subscriptions on application quit.</summary>
        void OnApplicationQuit()
        {
            _stopChannel.Send();

            if (_channelList.Count > 0 && IsConnected())
            {
                var unsubscribeOptions = new MqttClientUnsubscribeOptionsBuilder();
                foreach (string topic in _channelList.Select(x => x.topic))
                {
                    unsubscribeOptions.WithTopicFilter(topic);
                }
                client.UnsubscribeAsync(unsubscribeOptions.Build()).GetAwaiter().GetResult();
            }

            _channelList = new List<Channel>();

            Disconnect();

            if (Instance == this)
            {
                Instance = null;
            }
        }

        /// <summary>Establishes a connection to the MQTT broker.</summary>
        /// <param name="verbose">If true, logs successful connection to the console.</param>
        public void Connect(bool verbose)
        {
            var factory = new MqttFactory();
            client = factory.CreateMqttClient();

            // Routes received messages to the appropriate subscribed channels.
            client.ApplicationMessageReceivedAsync += e =>
            {
                string payload = Encoding.UTF8.GetString(
                    e.ApplicationMessage.PayloadSegment.Array ?? Array.Empty<byte>(),
                    e.ApplicationMessage.PayloadSegment.Offset,
                    e.ApplicationMessage.PayloadSegment.Count
                );

                lock (_channelList)
                {
                    foreach (Channel chn in _channelList)
                    {
                        if (string.Equals(e.ApplicationMessage.Topic, chn.topic))
                        {
                            chn.channel.ReceivedMessage(payload);
                        }
                    }
                }

                return Task.CompletedTask;
            };

            var options = new MqttClientOptionsBuilder()
                .WithTcpServer(ip, port)
                .WithClientId(Guid.NewGuid().ToString())
                .WithProtocolVersion(MQTTnet.Formatter.MqttProtocolVersion.V500)
                .Build();

            // Runs connection in a task with timeout to avoid blocking
            Task t = Task.Run(() => client.ConnectAsync(options));
            TimeSpan timeout = TimeSpan.FromMilliseconds(1000);
            if (!t.Wait(timeout))
            {
                Debug.LogError($"Could not connect to MQTT broker at {ip}:{port}");
            }
            else if (verbose)
            {
                Debug.Log($"Successfully connected to MQTT Broker at: {ip}:{port}");
            }
        }

        /// <summary>Disconnects from the MQTT broker if currently connected.</summary>
        public void Disconnect()
        {
            if (IsConnected())
            {
                client.DisconnectAsync().GetAwaiter().GetResult();
            }
        }

        /// <summary>Checks whether the client is currently connected to the broker.</summary>
        /// <returns>True if connected, false otherwise.</returns>
        public bool IsConnected()
        {
            try
            {
                return client != null && client.IsConnected;
            }
            catch (Exception ex)
            {
                Debug.LogWarning($"MQTTClient.IsConnected check failed: {ex.Message}");
                return false;
            }
        }

        /// <summary>Subscribes a channel to receive messages on the specified topic.</summary>
        /// <param name="obj">The MQTTChannel to receive messages.</param>
        /// <param name="topic">The MQTT topic to subscribe to.</param>
        /// <param name="qosLevel">The Quality of Service level for the subscription.</param>
        public void Subscribe(MQTTChannel obj, string topic, byte qosLevel)
        {
            if (IsConnected())
            {
                var qos = (MqttQualityOfServiceLevel)qosLevel;
                client
                    .SubscribeAsync(
                        new MqttClientSubscribeOptionsBuilder()
                            .WithTopicFilter(f => f.WithTopic(topic).WithQualityOfServiceLevel(qos))
                            .Build()
                    )
                    .ContinueWith(t =>
                    {
                        if (t.IsFaulted)
                        {
                            string message =
                                $"MQTT subscribe failed for '{topic}': "
                                + $"{t.Exception?.InnerException?.Message}";
                            Debug.LogError(message);
                        }
                    });

                lock (_channelList)
                {
                    _channelList.Add(new Channel() { topic = topic, channel = obj });
                }
            }
        }

        /// <summary>Publishes a message to the specified topic.</summary>
        /// <param name="topic">The MQTT topic to publish to.</param>
        /// <param name="payload">The message payload as a byte array, or null for trigger messages.</param>
        public void Publish(string topic, byte[] payload)
        {
            if (!IsConnected())
                return;

            var message = new MqttApplicationMessageBuilder()
                .WithTopic(topic)
                .WithPayload(payload ?? Array.Empty<byte>())
                .WithQualityOfServiceLevel(MqttQualityOfServiceLevel.ExactlyOnce)
                .Build();

            client.PublishAsync(message).ContinueWith(t =>
            {
                if (t.IsFaulted)
                {
                    Debug.LogError($"MQTT publish failed on '{topic}': {t.Exception?.InnerException?.Message}");
                }
            });
        }
    }
}

/// <summary>
/// Provides the MQTTChannel classes for type-safe MQTT messaging.
///
/// Includes the base MQTTChannel for simple trigger messages and the generic MQTTChannel&lt;T&gt;
/// for JSON-serialized typed messages.
/// </summary>
using System.Text;
using UnityEngine;
using UnityEngine.Events;
using uPLibrary.Networking.M2Mqtt.Messages;

namespace Gimbl
{
    /// <summary>
    /// Handles simple trigger-based MQTT messaging without payload data.
    /// </summary>
    public class MQTTChannel
    {
        /// <summary>The MQTT topic string for this channel.</summary>
        public string topic;

        /// <summary>The reference to the MQTTClient managing the broker connection.</summary>
        public MQTTClient client;

        /// <summary>The Unity event invoked when a message is received on this channel.</summary>
        public UnityEvent Event = new UnityEvent();

        /// <summary>Creates a new MQTT channel for the specified topic.</summary>
        /// <param name="topicStr">The MQTT topic to subscribe to or publish on.</param>
        /// <param name="isListener">If true, subscribes to receive messages on this topic.</param>
        /// <param name="qosLevel">The Quality of Service level for the subscription.</param>
        public MQTTChannel(string topicStr, bool isListener = true, byte qosLevel = MqttMsgBase.QOS_LEVEL_EXACTLY_ONCE)
        {
            Init(topicStr, isListener, qosLevel);
        }

        /// <summary>Initializes the channel with the specified topic and subscription settings.</summary>
        /// <param name="topicStr">The MQTT topic to subscribe to or publish on.</param>
        /// <param name="isListener">If true, subscribes to receive messages on this topic.</param>
        /// <param name="qosLevel">The Quality of Service level for the subscription.</param>
        public void Init(string topicStr, bool isListener, byte qosLevel)
        {
            topic = topicStr;
            client = GameObject.Find("MQTT Client").GetComponent<MQTTClient>();

            if (isListener)
            {
                client.Subscribe(this, topic, qosLevel);
            }
        }

        /// <summary>Handles received messages by invoking the Event.</summary>
        /// <param name="msgStr">The received message string (ignored for trigger channels).</param>
        public virtual void ReceivedMessage(string msgStr)
        {
            Event.Invoke();
        }

        /// <summary>Publishes a trigger message (null payload) to this channel's topic.</summary>
        public void Send()
        {
            client.client.Publish(topic, null);
        }
    }

    /// <summary>
    /// Handles typed MQTT messaging with JSON serialization for the payload.
    /// </summary>
    /// <typeparam name="T">The type of the message payload to serialize/deserialize.</typeparam>
    public class MQTTChannel<T> : MQTTChannel
    {
        /// <summary>The typed Unity event class for this channel.</summary>
        public class ChannelEvent : UnityEvent<T> { }

        /// <summary>The typed Unity event invoked when a message is received on this channel.</summary>
        public new ChannelEvent Event = new ChannelEvent();

        /// <summary>Creates a new typed MQTT channel for the specified topic.</summary>
        /// <param name="topicStr">The MQTT topic to subscribe to or publish on.</param>
        /// <param name="isListener">If true, subscribes to receive messages on this topic.</param>
        /// <param name="qosLevel">The Quality of Service level for the subscription.</param>
        public MQTTChannel(string topicStr, bool isListener = true, byte qosLevel = MqttMsgBase.QOS_LEVEL_EXACTLY_ONCE)
            : base(topicStr, isListener, qosLevel) { }

        /// <summary>Handles received messages by deserializing JSON and invoking the typed Event.</summary>
        /// <param name="msgStr">The received JSON message string.</param>
        public override void ReceivedMessage(string msgStr)
        {
            Event.Invoke(JsonUtility.FromJson<T>(msgStr));
        }

        /// <summary>Publishes a typed message as JSON to this channel's topic.</summary>
        /// <param name="msg">The message object to serialize and publish.</param>
        public void Send(T msg)
        {
            client.client.Publish(topic, Encoding.UTF8.GetBytes(JsonUtility.ToJson(msg)));
        }
    }
}

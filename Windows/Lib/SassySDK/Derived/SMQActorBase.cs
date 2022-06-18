using RabbitMQ.Client;
using RabbitMQ.Client.Events;
using RabbitMQ.Client.MessagePatterns;
using SassyMQ.Lib.RabbitMQ;
using System;
using System.Collections.Generic;
using System.Configuration;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Web;
using System.IO;
using Newtonsoft.Json;
using System.Threading;
using SassyMQ.SSOTME.Lib.RMQActors;
using SSoTme.OST.Lib.Extensions;

namespace SassyMQ.Lib.RabbitMQ.Payload
{
    /// <summary>
    /// Summary description for SMQActorBase
    /// </summary>
    public abstract class SMQActorBase<T>
        where T : StandardPayload<T>, new()
    {
        public DMProxy ReplyProxy { get; set; }

        public IModel RMQChannel;
        public IConnection RMQConnection;
        protected ConnectionFactory RMQFactory;
        public bool IsConnected { get; protected set; }
        public Task MonitorTask { get; private set; }
        public String QueueName { get; private set; }

        public string Password { get; private set; }
        public string RabbitEndpoint { get; set; }
        public string Username { get; private set; }
        public string VirtualHost { get; set; }
        public Guid SenderId { get; private set; }
        public String SenderName { get; set; }
        public String AllExchange { get; set; }
        public bool IsAutoConnect { get; set; }
        public static bool IsDebugMode { get; private set; }
        public static bool ShowPings { get; private set; }
        public bool StayConnected { get; set; }

        public void WaitForComplete(int timeout = -1)
        {
            var actorWaitTask = Task.Factory.StartNew(() =>
            {
                while (this.IsConnected)
                {
                    Thread.Sleep(500);
                }
            });

            if (timeout == -1) actorWaitTask.Wait();
            else actorWaitTask.Wait(timeout);
        }

        public SMQActorBase(String allExchange, bool isAutoConnect)
        {
            //
            // TODO: Add constructor logic here
            //
            this.StayConnected = true;
            this.IsAutoConnect = isAutoConnect;
            this.AllExchange = allExchange;
            this.HandleInvokeExternal += SMQActorBase_HandleInvokeExternal;
            if (this.IsAutoConnect) this.AutoConnect();
        }

        private void SMQActorBase_HandleInvokeExternal(object sender, InvokeEventArgs<T> e)
        {
            e.MethodDelegate.Invoke(sender, e.PayloadEventArgs);
        }

        public void AutoConnect()
        {
            String virtualHost = ConfigurationManager.AppSettings["rmq_virtual_host"];
            String username = ConfigurationManager.AppSettings["rmq_username"];
            if (String.IsNullOrEmpty(username)) username = "guest";
            String password = ConfigurationManager.AppSettings["rmq_password"];
            if (String.IsNullOrEmpty(password)) password = "guest";

            this.Connect(virtualHost, username, password);

        }

        public virtual bool Connect(String virtualHost, String username, String password)
        {
            if (this.IsConnected) throw new Exception("Can't connect - already connected.");

            this.IsConnected = true;
            this.VirtualHost = virtualHost;
            this.Username = username;
            this.Password = password;
            this.RabbitEndpoint = ConfigurationManager.AppSettings["rmq_endpoint"];
            this.SenderId = Guid.NewGuid();
            if (string.IsNullOrEmpty(this.AllExchange)) throw new ArgumentException("actor.AllExchange property must be assigned before connect can be called.");
            this.AllExchange = this.AllExchange;

            this.AttemptConnect();
            this.AfterConnect();

            this.MonitorTask = new Task(() =>
            {
                while (this.IsConnected)
                {
                    try
                    {
                        if (this.IsConnected && !this.RMQChannel.IsOpen) this.AttemptConnect();
                        var subscription = new Subscription(RMQChannel, QueueName);
                        this.MonitorMessages(subscription);
                        if (this.IsConnected) Thread.Sleep(5000);
                    }
                    catch // Ignore errors in this loop
                    {
                        if (!this.StayConnected) this.IsConnected = false;
                    }
                }

                this.RMQChannel.Close();
                if (this.RMQConnection.IsOpen) this.RMQConnection.Close();
            });

            this.MonitorTask.Start();

            return true;
        }

        public void AttemptConnect()
        {
            if (!ReferenceEquals(this.RMQChannel, null) && this.RMQChannel.IsOpen)
            {
                this.RMQChannel.Close();
                if (this.RMQConnection.IsOpen) this.RMQConnection.Close();
                this.RMQChannel = null;
                this.RMQConnection = null;
            }

            this.RMQFactory = new ConnectionFactory() { HostName = this.RabbitEndpoint, Port = 5672, VirtualHost = this.VirtualHost, UserName = this.Username, Password = this.Password, Ssl = new SslOption() { ServerName = "explore.ssot.me", Enabled = false } };
            this.RMQConnection = this.RMQFactory.CreateConnection();
            this.RMQChannel = this.RMQConnection.CreateModel();

            var consumer = new EventingBasicConsumer(this.RMQChannel);
            consumer.Received += Consumer_Received;
            RMQChannel.BasicConsume("amq.rabbitmq.reply-to", true, consumer);

            this.QueueName = RMQChannel.QueueDeclare().QueueName;

            RMQChannel.QueueBind(queue: QueueName, exchange: this.AllExchange, routingKey: "#");

            System.Console.WriteLine("\n\nCONNECTED: {0}: Waiting for messages. To exit press CTRL+C\n", this.GetType().Name.ToTitle());
        }

        private void MonitorMessages(Subscription subscription)
        {
            int count = 0;
            while (this.IsConnected && this.RMQChannel.IsOpen)
            {
                BasicDeliverEventArgs bdea = default(BasicDeliverEventArgs);
                var gotMessage = subscription.Next(100, out bdea);
                if (gotMessage)
                {
                    try
                    {
                        var msgText = string.Format("{0}{1}. {2} => {3}{0}", Environment.NewLine, ++count, bdea.Exchange, bdea.RoutingKey);
                        //var msgText = string.Format("{3}. {0}: {1} -> '{2}'", bdea.Exchange, bdea.RoutingKey, Encoding.UTF8.GetString(bdea.Body), ++count);

                        var print = SMQActorBase<T>.IsDebugMode;

                        print = print && (!bdea.IsPing() || SMQActorBase<T>.ShowPings);

                        if (print) System.Console.WriteLine(msgText);

                        var body = Encoding.UTF8.GetString(bdea.Body);
                        T payload = StandardPayload<T>.FromJSonString(body) as T;

                        payload.DeliveryTag = bdea.DeliveryTag.SafeToString();
                        if (String.IsNullOrEmpty(payload.RoutingKey)) payload.RoutingKey = bdea.RoutingKey;
                        payload.ReplyTo = bdea.BasicProperties.ReplyTo;
                        payload.CorrelationId = bdea.BasicProperties.CorrelationId;
                        if (String.IsNullOrEmpty(payload.CorrelationId)) payload.CorrelationId = Guid.NewGuid().ToString();
                        payload.Exchange = bdea.Exchange;

                        this.OnMessageReceived(payload);
                        Task.Factory.StartNew(() =>
                        {
                            try
                            {
                                this.CheckRouting(payload);
                            }
                            catch (Exception ex)
                            {
                                payload.Exception = ex;
                            }
                            this.OnAfterMessageReceived(payload);
                        });
                    }
                    catch (Exception ex)
                    {
                        Console.WriteLine("ERROR: " + ex.Message);
                    }
                }
            }

        }
        public void ReportException(StandardPayload<T> payload)
        {
            this.RMQChannel.BasicPublish("", payload.ReplyTo, null, Encoding.UTF8.GetBytes(payload.ToJSonString()));
        }

        protected virtual void AfterConnect()
        {
            // do nothing
        }
        public event EventHandler<PayloadEventArgs<T>> MessageReceived;
        protected virtual void OnMessageReceived(T payload)
        {
            var plea = new PayloadEventArgs<T>(payload);
            this.Invoke(this.MessageReceived, plea);
        }

        public event EventHandler<PayloadEventArgs<T>> AfterMessageReceived;
        protected virtual void OnAfterMessageReceived(T payload)
        {
            var plea = new PayloadEventArgs<T>(payload);
            this.Invoke(this.AfterMessageReceived, plea);
        }

        protected abstract void CheckRouting(T payload);
        protected abstract void CheckRouting(T payload, bool isDirectMessage);

        private void Consumer_Received(object sender, BasicDeliverEventArgs e)
        {
            var body = Encoding.UTF8.GetString(e.Body);
            var payload = StandardPayload<T>.FromJSonString(body);
            this.OnReplyTo(payload);
        }

        public event System.EventHandler<PayloadEventArgs<T>> ReplyTo;
        protected virtual void OnReplyTo(T payload)
        {
            var plea = new PayloadEventArgs<T>(payload);
            this.Invoke(this.ReplyTo, plea);
        }

        public void Disconnect()
        {
            this.IsConnected = false;
        }

        public event System.EventHandler<InvokeEventArgs<T>> HandleInvokeExternal;

        protected void Invoke(System.EventHandler<PayloadEventArgs<T>> methodDelegate, PayloadEventArgs<T> plea)
        {
            if (!ReferenceEquals(methodDelegate, null))
            {
                if (!ReferenceEquals(HandleInvokeExternal, null))
                {
                    InvokeEventArgs<T> iea = new InvokeEventArgs<T>()
                    {
                        MethodDelegate = methodDelegate,
                        PayloadEventArgs = plea
                    };
                    this.HandleInvokeExternal(this, iea);
                }
                else methodDelegate.Invoke(methodDelegate.Target, plea);
                plea.Payload.IsHandled = true;
            }
        }


        public virtual T CreatePayload()
        {
            T payload = new T();
            payload.SenderId = this.SenderId.GuidToKey();
            payload.SenderName = this.SenderName;
            payload.PayloadId = Guid.NewGuid().GuidToLongKey();
            return payload;
        }

        protected void SendMessage(T payload, string description, string mic, string routingKey, string directRoutingKey = "")
        {
            if (!this.RMQChannel.IsOpen) this.AttemptConnect();

            if (IsDebugMode)
            {
                System.Console.WriteLine(description);
                System.Console.WriteLine("payload: " + payload.SafeToString());
            }

            var finalRoute = payload.RoutingKey = routingKey;

            if (!string.IsNullOrEmpty(directRoutingKey))
            {
                finalRoute = directRoutingKey;
                mic = "";
            }

            this.ReplyTo += payload.HandleReplyTo;

            IBasicProperties props = this.RMQChannel.CreateBasicProperties();
            props.ReplyTo = "amq.rabbitmq.reply-to";
            this.RMQChannel.BasicPublish(mic, finalRoute, props, Encoding.UTF8.GetBytes(payload.ToJSonString()));
        }
    }
}
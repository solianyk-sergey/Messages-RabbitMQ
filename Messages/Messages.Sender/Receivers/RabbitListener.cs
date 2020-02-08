using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Messages.Common;
using Messages.Common.Messages;
using Messages.Sender.Controllers;
using RabbitMQ.Client;
using RabbitMQ.Client.Events;

namespace Messages.Sender.Receivers
{
    public class RabbitListener
    {
        ConnectionFactory factory { get; set; }
        IConnection connection { get; set; }
        IModel channel { get; set; }

        public void Register()
        {
            channel.ExchangeDeclare(exchange: "sms", type: "direct");
            var queueName = channel.QueueDeclare().QueueName;

            channel.QueueBind(queue: queueName, exchange: "sms", routingKey: "receive");

            var consumer = new EventingBasicConsumer(channel);
            consumer.Received += Received;
            channel.BasicConsume(queue: queueName,
                autoAck: false,
                consumer: consumer);
        }

        public void Deregister()
        {
            this.connection.Close();
        }

        public RabbitListener()
        {
            this.factory = new ConnectionFactory() { HostName = "localhost" };
            this.connection = factory.CreateConnection();
            this.channel = connection.CreateModel();
        }

        private static void Received(object model, BasicDeliverEventArgs ea)
        {
            var factory = new ConnectionFactory() { HostName = "localhost" };
            using (var connection = factory.CreateConnection())
            using (var channel = connection.CreateModel())
            {
                var body = ea.Body;

                var binaryFormatter = new CustomBinaryFormatter();
                var sms = binaryFormatter.FromByteArray<SmsIsSendEvent>(body);


                var controller = new MessagesController(null);
                controller.SmsSend(sms);

                // processed successfully
                channel.BasicAck(deliveryTag: ea.DeliveryTag, multiple: false);
            }
        }
    }
}

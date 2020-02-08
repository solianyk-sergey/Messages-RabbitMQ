using System;
using System.Text;
using System.Threading;
using Messages.Common;
using Messages.Common.Messages;
using RabbitMQ.Client;
using RabbitMQ.Client.Events;

namespace Messages.Receiver
{
    class Program
    {
        static void Main(string[] args)
        {
            var factory = new ConnectionFactory() {HostName = "localhost"};
            using (var connection = factory.CreateConnection())
            using (var channel = connection.CreateModel())
            {
                channel.ExchangeDeclare(exchange: "sms", type: "direct");
                var queueName = channel.QueueDeclare().QueueName;

                channel.QueueBind(queue: queueName, exchange: "sms", routingKey: "send");

                var consumer = new EventingBasicConsumer(channel);
                consumer.Received += (model, ea) =>
                {
                    var body = ea.Body;

                    var binaryFormatter = new CustomBinaryFormatter();
                    var sms = binaryFormatter.FromByteArray<SendSmsEvent>(body);
                    
                    // emulate some work - using Twillio to send message, for example
                    Thread.Sleep(1000);

                    Console.WriteLine($"Received message (version - {sms.Version}) from '{sms.From}' to '{sms.To}' with next text: {sms.Body}");

                    // send message, that sms was send
                    var replyProps = channel.CreateBasicProperties();
                    replyProps.CorrelationId = ea.BasicProperties.CorrelationId;

                    var smsSend = new SmsIsSendEvent {Id = sms.Id};
                    byte[] smsSendBody = binaryFormatter.ToByteArray(smsSend);

                    channel.BasicPublish(exchange: "sms", routingKey: ea.BasicProperties.ReplyTo, basicProperties: replyProps, body: smsSendBody);
                    
                    // processed successfully
                    channel.BasicAck(deliveryTag: ea.DeliveryTag, multiple: false);
                };

                channel.BasicConsume(queue: queueName,
                    autoAck: false,
                    consumer: consumer);

                Console.WriteLine(" Press [enter] to exit.");
                Console.ReadLine();
            }
        }
    }
}

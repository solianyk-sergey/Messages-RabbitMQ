using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Runtime.Serialization.Formatters.Binary;
using System.Threading.Tasks;
using Messages.Common;
using Messages.Common.Messages;
using Messages.Sender.Models;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Logging;
using RabbitMQ.Client;

namespace Messages.Sender.Controllers
{
    [ApiController]
    [Route("[controller]")]
    public class MessagesController : ControllerBase
    {
        private readonly ILogger<MessagesController> _logger;
        private static readonly List<string> Storage = new List<string>();

        public MessagesController(ILogger<MessagesController> logger)
        {
            _logger = logger;
        }

        [HttpGet]
        public List<string> Get()
        {
            return Storage.ToList();
        }

        [HttpPost]
        public async Task<string> SendMessage(MessageViewModel message)
        {
            var sendSmsEvent = new SendSmsEvent
            {
                Id = message.Id,
                Body = message.Body,
                From = message.From,
                To = message.To,
                Version = "v1.0"
            };

            var factory = new ConnectionFactory() { HostName = "localhost" };
            using (var connection = factory.CreateConnection())
            using (var channel = connection.CreateModel())
            {
                channel.ExchangeDeclare(exchange: "sms", type: ExchangeType.Direct);

                IBasicProperties props = channel.CreateBasicProperties();
                props.CorrelationId = Guid.NewGuid().ToString();
                props.ReplyTo = "receive";

                var binaryFormatter = new CustomBinaryFormatter();
                byte[] body = binaryFormatter.ToByteArray(sendSmsEvent);

                channel.BasicPublish(exchange: "sms",
                    routingKey: "send",
                    basicProperties: props,
                    body: body);
            }

            return "Your message will be send soon";
        }

        public void SmsSend(SmsIsSendEvent smsIsSendEvent)
        {
            Storage.Add(smsIsSendEvent.Id.ToString());
        }
    }
}

﻿namespace Kwetterprise.EventSourcing.Client.Kafka
{
    using System.Net;
    using System.Text.Json;
    using System.Threading.Tasks;
    using Confluent.Kafka;
    using Kwetterprise.EventSourcing.Client.Interface;
    using Kwetterprise.EventSourcing.Client.Models.Event;
    using Microsoft.Extensions.Logging;

    public sealed class KafkaEventPublisher : IEventPublisher
    {
        private readonly ILogger<KafkaEventPublisher> logger;
        private readonly IProducer<Null, string> producerBuilder;

        public KafkaEventPublisher(ILogger<KafkaEventPublisher> logger, KafkaConfiguration configuration)
        {
            this.logger = logger;
            var config = new ProducerConfig
            {
                BootstrapServers = configuration.Servers,
                ClientId = Dns.GetHostName(),
                SocketTimeoutMs = configuration.SocketTimeoutMs,
            };

            this.producerBuilder = new ProducerBuilder<Null, string>(config).Build();
        }

        public async Task Publish<T>(T @event, Topic topic)
            where T : EventBase
        {
            try
            {
                var message = JsonSerializer.Serialize(@event);
                var dr = await this.producerBuilder.ProduceAsync(
                    topic.Value,
                    new Message<Null, string> { Value = message });
                this.logger.LogDebug($"Delivered '{dr.Value}' to '{dr.TopicPartitionOffset}'");
            }
            catch (ProduceException<Null, string> e)
            {
                this.logger.LogError($"Delivery failed: {e.Error.Reason}");
            }
        }

        public void Dispose()
        {
            this.producerBuilder.Dispose();
        }
    }
}

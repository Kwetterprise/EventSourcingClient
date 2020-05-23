﻿// ---------------------------------------------------------------------------------------------------------------------
//  <copyright file="KafkaEventListener.cs" company="Prodrive B.V.">
//      Copyright (c) Prodrive B.V. All rights reserved.
//  </copyright>
// ---------------------------------------------------------------------------------------------------------------------

namespace Kwetterprise.EventSourcing.Client.Kafka
{
    using System;
    using System.Collections.Generic;
    using System.Linq;
    using System.Reactive.Subjects;
    using System.Text.Json;
    using System.Threading;
    using System.Threading.Tasks;
    using Confluent.Kafka;
    using Kwetterprise.EventSourcing.Client.Interface;
    using Kwetterprise.EventSourcing.Client.Models.Event;
    using Microsoft.Extensions.Logging;

    public sealed class KafkaEventListener : IEventListener
    {
        private readonly ILogger<KafkaEventListener> logger;
        private readonly Subject<EventBase> subject = new Subject<EventBase>();
        private readonly IConsumer<Ignore, string> consumer;

        private CancellationTokenSource token = new CancellationTokenSource();
        private Task? task;

        /// <inheritdoc />
        public KafkaEventListener(ILogger<KafkaEventListener> logger, KafkaConsumerConfiguration configuration)
        {
            this.logger = logger;
            var config = new ConsumerConfig
            {
                BootstrapServers = configuration.Servers,
                GroupId = configuration.GroupId,
                AutoOffsetReset = configuration.Offset,
            };

            this.consumer = new ConsumerBuilder<Ignore, string>(config).Build();
        }

        public Task StartListening(List<Topic> topics)
        {
            if (this.token != null && !this.token.IsCancellationRequested)
            {
                throw new InvalidOperationException("Already listening.");
            }

            this.token = new CancellationTokenSource();

            this.consumer.Subscribe(topics.Select(x => x.ToString()));

            this.task = Task.Run(
                () =>
                {
                    while (!this.token.Token.IsCancellationRequested)
                    {
                        try
                        {
                            this.DoWork();
                        }
                        catch (Exception e)
                        {
                            this.subject.OnError(e);
                            return;
                        }
                    }

                    this.subject.OnCompleted();
                },
                this.token.Token);

            return this.task;
        }

        public void Stop()
        {
            this.token.Cancel();
        }

        public IDisposable Subscribe(IObserver<EventBase> observer)
        {
            return this.subject.Subscribe(observer);
        }

        public void Dispose()
        {
            this.consumer.Close();
            this.consumer.Dispose();

            this.subject.Dispose();
        }

        private void DoWork()
        {
            ConsumeResult<Ignore, string> consumeResult;
            try
            {

                consumeResult = this.consumer.Consume(this.token!.Token);
            }
            catch (ConsumeException e)
            {
                this.logger.LogError(e, "Unable to consume event(s).");
                throw;
            }

            EventBase deserializedEvent;
            try
            {
                deserializedEvent =
                    JsonSerializer.Deserialize<EventBase>(
                        consumeResult.Message.Value,
                        new JsonSerializerOptions
                        {
                            Converters = { new KwetterpriseEventConverter(), },
                        });
            }
            catch (JsonException)
            {
                this.logger.LogError($"Failed to deserialize event: \"{consumeResult.Message.Value}\".");
                throw;
            }

            this.subject.OnNext(deserializedEvent);
        }
    }
}
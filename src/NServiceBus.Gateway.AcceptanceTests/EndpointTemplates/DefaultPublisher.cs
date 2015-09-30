namespace NServiceBus.AcceptanceTests.EndpointTemplates
{
    using System;
    using System.Collections.Generic;
    using System.Linq;
    using System.Threading.Tasks;
    using AcceptanceTesting;
    using AcceptanceTesting.Support;
    using Config.ConfigurationSource;
    using Pipeline;
    using Pipeline.Contexts;
    using Routing.StorageDrivenPublishing;

    public class DefaultPublisher : IEndpointSetupTemplate
    {
        public Task<BusConfiguration> GetConfiguration(RunDescriptor runDescriptor, EndpointConfiguration endpointConfiguration, IConfigurationSource configSource, Action<BusConfiguration> configurationBuilderCustomization)
        {
            return new DefaultServer(new List<Type> { typeof(SubscriptionTracer), typeof(SubscriptionTracer.Registration) }).GetConfiguration(runDescriptor, endpointConfiguration, configSource, b =>
            {
                b.Pipeline.Register<SubscriptionTracer.Registration>();
                configurationBuilderCustomization(b);
            });
        }

        class SubscriptionTracer : Behavior<OutgoingContext>
        {
            public ScenarioContext Context { get; set; }

            public override async Task Invoke(OutgoingContext context, Func<Task> next)
            {
                await next().ConfigureAwait(false);

                SubscribersForEvent subscribersForEvent;

                if (context.TryGet(out  subscribersForEvent))
                {
                    Context.AddTrace($"Subscribers for {subscribersForEvent.EventType.Name} : {string.Join(";", subscribersForEvent)}");

                    if (!subscribersForEvent.Subscribers.Any())
                    {
                        Context.AddTrace($"No Subscribers found for message {subscribersForEvent.EventType.Name}");
                    }
                }
            }

            public class Registration : RegisterStep
            {
                public Registration()
                    : base("SubscriptionTracer", typeof(SubscriptionTracer), "Traces the list of found subscribers")
                {
                }
            }
        }
    }
}
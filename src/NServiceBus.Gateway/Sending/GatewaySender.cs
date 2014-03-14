namespace NServiceBus.Gateway.V2.Sending
{
    using System;
    using System.Collections.Generic;
    using System.Linq;
    using Channels;
    using Notifications;
    using NServiceBus.Features;
    using ObjectBuilder;
    using Receiving;
    using Routing;
    using Satellites;
    using Settings;
    using Transports;
    using Unicast.Transport;

    public class GatewaySender : IAdvancedSatellite
    {
        public IBuilder Builder { get; set; }
        public IManageReceiveChannels ChannelManager { get; set; }
        public IMessageNotifier Notifier { get; set; }
        public ISendMessages MessageSender { get; set; }
        public IChannelTypeRegistry TypeRegistry { get; set; }

        public bool Handle(TransportMessage message)
        {
            var destinationSites = GetDestinationSitesFor(message);

            //if there is more than 1 destination we break it up into multiple messages
            if (destinationSites.Count() > 1)
            {
                foreach (var destinationSite in destinationSites)
                {
                    CloneAndSendLocal(message, destinationSite);
                }

                return true;
            }

            var destination = destinationSites.FirstOrDefault();

            if (destination == null)
            {
                throw new InvalidOperationException("No destination found for message");
            }

            SendToSite(message, destination);

            return true;
        }

        public Address InputAddress
        {
            get { return SettingsHolder.Get<Address>("Gateway.V2.InputAddress"); }
        }

        public bool Disabled
        {
            get { return !Feature.IsEnabled<NServiceBus.Gateway.V2.Features.Gateway>(); }
        }

        public void Start()
        {
        }

        public void Stop()
        {
        }

        public Action<TransportReceiver> GetReceiverCustomization()
        {
            return transport =>
            {
                transport.TransactionSettings.TransactionTimeout =
                    GatewayTransaction.Timeout(transport.TransactionSettings.TransactionTimeout);
            };
        }

        IList<Site> GetDestinationSitesFor(TransportMessage messageToDispatch)
        {
            return Builder.BuildAll<IRouteMessagesToSites>()
                .SelectMany(r => r.GetDestinationSitesFor(messageToDispatch)).ToList();
        }

        void CloneAndSendLocal(TransportMessage messageToDispatch, Site destinationSite)
        {
            //todo - do we need to clone? check with Jonathan O
            messageToDispatch.Headers[Headers.DestinationSites] = destinationSite.Key;

            MessageSender.Send(messageToDispatch, InputAddress);
        }

        void SendToSite(TransportMessage transportMessage, Site targetSite)
        {
            transportMessage.Headers[Headers.OriginatingSite] = GetDefaultAddressForThisSite(targetSite);

            var forwarder = Builder.Build<IForwardMessagesToSites>();
            forwarder.Forward(transportMessage, targetSite);

            Notifier.RaiseMessageForwarded(Address.Local.ToString(), targetSite.Channel.Type, transportMessage);
        }

        string GetDefaultAddressForThisSite(Site targetSite)
        {
            var senderType = TypeRegistry.GetSenderType(targetSite.Channel.Type);
            var types = TypeRegistry.GetChannelTypesForSenderType(senderType);
            var defaultChannel = ChannelManager.GetDefaultChannel(types);
            return string.Format("{0},{1}", defaultChannel.Type, defaultChannel.Address);
        }
    }
}

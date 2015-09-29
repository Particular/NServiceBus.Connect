namespace NServiceBus.Gateway.Routing.Sites
{
    using System.Collections.Generic;
    using Channels;

    class OriginatingSiteHeaderRouter : IRouteMessagesToSites
    {
        public IEnumerable<Site> GetDestinationSitesFor(Dictionary<string, string> headers)
        {
            string originatingSite;
            if (headers.TryGetValue(Headers.OriginatingSite, out originatingSite))
            {
                yield return new Site
                {
                    Channel = Channel.Parse(originatingSite),
                    Key = "Default reply channel",
                    LegacyMode = headers.IsLegacyGatewayMessage()
                };
            }
        }
    }
}
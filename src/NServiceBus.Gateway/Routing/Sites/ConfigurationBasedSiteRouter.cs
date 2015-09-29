namespace NServiceBus.Gateway.Routing.Sites
{
    using System.Collections.Generic;

    class ConfigurationBasedSiteRouter : IRouteMessagesToSites
    {
        public IEnumerable<Site> GetDestinationSitesFor(Dictionary<string, string> headers)
        {
            string destinationSites;
            if (headers.TryGetValue(Headers.DestinationSites, out destinationSites))
            {
                var siteKeys = destinationSites.Split(',');

                foreach (var siteKey in siteKeys)
                {
                    Site site;
                    if (Sites.TryGetValue(siteKey, out site))
                    {
                        yield return site;
                    }
                }
            }
        }

        public IDictionary<string, Site> Sites { get; set; }
    }
}

using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Hosting.Internal;
using Microsoft.Extensions.FileProviders;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Microwave.Discovery;
using Microwave.Discovery.Domain;
using Microwave.Discovery.Domain.Events;
using Microwave.Discovery.Domain.Services;
using Microwave.UI.Areas.MicrowaveDashboard.Pages;
using Moq;

namespace Microwave.UI.UnitTests
{
    [TestClass]
    public class RestOfPagesTests
    {
        [TestMethod]
        public async Task GetConsumingServices()
        {
            var mock = new Mock<IDiscoveryHandler>();
            mock.Setup(m => m.GetConsumingServices()).ReturnsAsync(new EventLocationDto(
                new List<ServiceNode>(),
                new List<EventSchema>(),
                new List<ReadModelSubscription>(),
                "name"));
            var indexModel = new IndexModel(mock.Object, new MicrowaveConfiguration());

            await indexModel.OnGetAsync();

            Assert.IsNotNull(indexModel.ConsumingServices);
        }

        [TestMethod]
        public async Task GetNodesAndEdges()
        {
            var mock = new Mock<IDiscoveryHandler>();mock.Setup(m => m.GetConsumingServices()).ReturnsAsync(new EventLocationDto(
                new List<ServiceNode>(),
                new List<EventSchema>(),
                new List<ReadModelSubscription>(),
                "name"));

            var indexModel = new IndexModel(mock.Object, new MicrowaveConfiguration());

            await indexModel.OnGetAsync();

            Assert.IsFalse(indexModel.HasMissingEvents);
        }

        [TestMethod]
        public async Task Discover()
        {
            var mock = new Mock<IDiscoveryHandler>();
            var indexModel = new IndexModel(mock.Object, new MicrowaveConfiguration());

            await indexModel.OnPostAsync();

            mock.Verify(m => m.DiscoverConsumingServices(), Times.Once);
        }

        [TestMethod]
        public void DiscoveryOption()
        {
            var hostingEnvironment = new HostingEnvironment();
            var fileProvider = new EmbeddedFileProvider(typeof(RestOfPagesTests).Assembly);
            hostingEnvironment.WebRootFileProvider = fileProvider;
            var microwaveUiConfigureOptions = new MicrowaveUiConfigureOptions(hostingEnvironment);

            var staticFileOptions = new StaticFileOptions();
            staticFileOptions.FileProvider = fileProvider;
            microwaveUiConfigureOptions.PostConfigure("hm", staticFileOptions);

            Assert.AreEqual(typeof(CompositeFileProvider), staticFileOptions.FileProvider.GetType());
        }

        [TestMethod]
        public void DiscoveryOptionThrowsException()
        {
            var microwaveUiConfigureOptions = new MicrowaveUiConfigureOptions(new HostingEnvironment());

            Assert.ThrowsException<InvalidOperationException>(() =>
                microwaveUiConfigureOptions.PostConfigure("hm", new StaticFileOptions()));
        }
    }
}
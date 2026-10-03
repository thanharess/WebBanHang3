using System.Net;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Mvc.Testing;
using Xunit;

namespace WebBanHang.Tests
{
    public class RenderSectionReproTests : IClassFixture<WebApplicationFactory<Program>>
    {
        private readonly WebApplicationFactory<Program> _factory;

        public RenderSectionReproTests(WebApplicationFactory<Program> factory)
        {
            _factory = factory;
        }

        [Fact]
        public async Task Request_PageWithScriptsSection_ShouldNotThrowInvalidOperation()
        {
            // Arrange
            var client = _factory.CreateClient(new WebApplicationFactoryClientOptions
            {
                AllowAutoRedirect = false
            });

            // Act
            var response = await client.GetAsync("/Cart/Index");

            // Assert - we expect the request to succeed (200 or redirect), not to throw during rendering
            Assert.False(response.StatusCode == HttpStatusCode.InternalServerError, "Request returned 500 - render may have thrown InvalidOperationException for unrendered section");
        }
    }
}

using System.Net;
using System.Net.Http.Json;
using System.Text;
using BankLite.Application.DTOs;
using Bogus;
using FluentAssertions;
using Moq;
using Xunit;

namespace BankLite.Tests.Integration
{
    [Collection("Integration")]
    public class ChatIntegrationTests : IAsyncLifetime
    {
        private readonly HttpClient _client;
        private readonly BankLiteWebApplicationFactory _factory;
        private readonly Faker _faker = new Faker();

        public ChatIntegrationTests(BankLiteWebApplicationFactory factory)
        {
            _factory = factory;
            _client = factory.CreateClient(new Microsoft.AspNetCore.Mvc.Testing.WebApplicationFactoryClientOptions
            {
                AllowAutoRedirect = false,
                HandleCookies = false,
            });
        }

        public async Task InitializeAsync() => await _factory.ResetDatabaseAsync();
        public Task DisposeAsync() => Task.CompletedTask;

        private async Task<string> RegisterAndGetTokenAsync()
        {
            var response = await _client.PostAsJsonAsync("/api/auth/register", new RegisterUserDto
            {
                FullName = "Test User",
                Email = _faker.Internet.Email(),
                Password = "Password123!"
            });
            var cookies = response.Headers.GetValues("Set-Cookie").ToList();
            return cookies.First(c => c.Contains("accessToken")).Split(";")[0].Split("=")[1];
        }

        private void AuthenticateClient(string token)
        {
            _client.DefaultRequestHeaders.Remove("Cookie");
            _client.DefaultRequestHeaders.Add("Cookie", $"accessToken={token}");
        }

        [Fact]
        public async Task SendMessage_ValidMessage_Returns200()
        {
            var token = await RegisterAndGetTokenAsync();
            AuthenticateClient(token);

            var response = await _client.PostAsJsonAsync("/api/chat/message",
                new ChatMessageDto("Hello Alfred"));

            response.StatusCode.Should().Be(HttpStatusCode.OK);
        }

        [Fact]
        public async Task SendMessage_ValidMessage_ReturnsMockResponse()
        {
            var token = await RegisterAndGetTokenAsync();
            AuthenticateClient(token);

            var response = await _client.PostAsJsonAsync("/api/chat/message",
                new ChatMessageDto("Hello Alfred"));
            var content = await response.Content.ReadAsStringAsync();

            content.Should().Contain("Alfred");
        }

        [Fact]
        public async Task SendMessage_ValidMessage_ReturnsCorrectJsonStructure()
        {
            var token = await RegisterAndGetTokenAsync();
            AuthenticateClient(token);

            var response = await _client.PostAsJsonAsync("/api/chat/message",
                new ChatMessageDto("Hello Alfred"));
            var data = await response.Content.ReadFromJsonAsync<Dictionary<string, string>>();

            data.Should().NotBeNull();
            data!.Should().ContainKey("response");
            data!["response"].Should().NotBeNullOrEmpty();
        }

        [Fact]
        public async Task SendMessage_ValidMessage_ReturnsJsonContentType()
        {
            var token = await RegisterAndGetTokenAsync();
            AuthenticateClient(token);

            var response = await _client.PostAsJsonAsync("/api/chat/message",
                new ChatMessageDto("Hello Alfred"));

            response.Content.Headers.ContentType!.MediaType.Should().Be("application/json");
        }

        [Fact]
        public async Task SendMessage_ValidMessage_ResponseIsNotNull()
        {
            var token = await RegisterAndGetTokenAsync();
            AuthenticateClient(token);

            var response = await _client.PostAsJsonAsync("/api/chat/message",
                new ChatMessageDto("Hello Alfred"));
            var data = await response.Content.ReadFromJsonAsync<Dictionary<string, string>>();

            data!["response"].Should().NotBeNull();
        }

        [Fact]
        public async Task SendMessage_SingleCharMessage_Returns200()
        {
            var token = await RegisterAndGetTokenAsync();
            AuthenticateClient(token);

            var response = await _client.PostAsJsonAsync("/api/chat/message",
                new ChatMessageDto("H"));

            response.StatusCode.Should().Be(HttpStatusCode.OK);
        }

        [Fact]
        public async Task SendMessage_Exactly200Chars_Returns200()
        {
            var token = await RegisterAndGetTokenAsync();
            AuthenticateClient(token);

            var response = await _client.PostAsJsonAsync("/api/chat/message",
                new ChatMessageDto(new string('a', 200)));

            response.StatusCode.Should().Be(HttpStatusCode.OK);
        }

        [Fact]
        public async Task SendMessage_SpecialCharacters_Returns200()
        {
            var token = await RegisterAndGetTokenAsync();
            AuthenticateClient(token);

            var response = await _client.PostAsJsonAsync("/api/chat/message",
                new ChatMessageDto("What's my balance? $100 & more!"));

            response.StatusCode.Should().Be(HttpStatusCode.OK);
        }

        [Fact]
        public async Task SendMessage_UnicodeCharacters_Returns200()
        {
            var token = await RegisterAndGetTokenAsync();
            AuthenticateClient(token);

            var response = await _client.PostAsJsonAsync("/api/chat/message",
                new ChatMessageDto("Bonjour Alfred, ça va?"));

            response.StatusCode.Should().Be(HttpStatusCode.OK);
        }

        [Fact]
        public async Task SendMessage_SqlInjectionAttempt_HandledSafely()
        {
            var token = await RegisterAndGetTokenAsync();
            AuthenticateClient(token);

            var response = await _client.PostAsJsonAsync("/api/chat/message",
                new ChatMessageDto("'; DROP TABLE Users; --"));

            ((int)response.StatusCode).Should().BeOneOf(200, 400);
        }

        [Fact]
        public async Task SendMessage_XssAttempt_HandledSafely()
        {
            var token = await RegisterAndGetTokenAsync();
            AuthenticateClient(token);

            var response = await _client.PostAsJsonAsync("/api/chat/message",
                new ChatMessageDto("<script>alert('xss')</script>"));

            ((int)response.StatusCode).Should().BeOneOf(200, 400);
        }

        [Fact]
        public async Task SendMessage_EmptyMessage_Returns400()
        {
            var token = await RegisterAndGetTokenAsync();
            AuthenticateClient(token);

            var response = await _client.PostAsJsonAsync("/api/chat/message",
                new ChatMessageDto(""));

            response.StatusCode.Should().Be(HttpStatusCode.BadRequest);
        }

        [Fact]
        public async Task SendMessage_WhitespaceMessage_Returns400()
        {
            var token = await RegisterAndGetTokenAsync();
            AuthenticateClient(token);

            var response = await _client.PostAsJsonAsync("/api/chat/message",
                new ChatMessageDto("   "));

            response.StatusCode.Should().Be(HttpStatusCode.BadRequest);
        }

        [Fact]
        public async Task SendMessage_201Chars_Returns400()
        {
            var token = await RegisterAndGetTokenAsync();
            AuthenticateClient(token);

            var response = await _client.PostAsJsonAsync("/api/chat/message",
                new ChatMessageDto(new string('a', 201)));

            response.StatusCode.Should().Be(HttpStatusCode.BadRequest);
        }

        [Fact]
        public async Task SendMessage_NullMessage_Returns400()
        {
            var token = await RegisterAndGetTokenAsync();
            AuthenticateClient(token);

            var response = await _client.PostAsync("/api/chat/message",
                new StringContent("null", Encoding.UTF8, "application/json"));

            response.StatusCode.Should().Be(HttpStatusCode.BadRequest);
        }

        [Fact]
        public async Task SendMessage_EmptyBody_Returns400()
        {
            var token = await RegisterAndGetTokenAsync();
            AuthenticateClient(token);

            var response = await _client.PostAsync("/api/chat/message",
                new StringContent("{}", Encoding.UTF8, "application/json"));

            response.StatusCode.Should().Be(HttpStatusCode.BadRequest);
        }

        [Fact]
        public async Task SendMessage_Unauthenticated_Returns401()
        {
            _client.DefaultRequestHeaders.Remove("Cookie");

            var response = await _client.PostAsJsonAsync("/api/chat/message",
                new ChatMessageDto("Hello Alfred"));

            response.StatusCode.Should().Be(HttpStatusCode.Unauthorized);
        }

        [Fact]
        public async Task SendMessage_InvalidJwt_Returns401()
        {
            AuthenticateClient("invalid.jwt.token");

            var response = await _client.PostAsJsonAsync("/api/chat/message",
                new ChatMessageDto("Hello Alfred"));

            response.StatusCode.Should().Be(HttpStatusCode.Unauthorized);
        }

        [Fact]
        public async Task SendMessage_ValidMessage_CallsGroqServiceOnce()
        {
            _factory.GroqServiceMock.Invocations.Clear();
            var token = await RegisterAndGetTokenAsync();
            AuthenticateClient(token);

            await _client.PostAsJsonAsync("/api/chat/message",
                new ChatMessageDto("Hello Alfred"));

            _factory.GroqServiceMock.Verify(g => g.GetChatResponseAsync(It.IsAny<string>()), Times.Once);
        }

        [Fact]
        public async Task SendMessage_MultipleMessages_AllSucceed()
        {
            var token = await RegisterAndGetTokenAsync();
            AuthenticateClient(token);

            var response1 = await _client.PostAsJsonAsync("/api/chat/message",
                new ChatMessageDto("First message"));
            var response2 = await _client.PostAsJsonAsync("/api/chat/message",
                new ChatMessageDto("Second message"));

            response1.StatusCode.Should().Be(HttpStatusCode.OK);
            response2.StatusCode.Should().Be(HttpStatusCode.OK);
        }

        [Fact]
        public async Task SendMessage_ValidMessage_ResponseContainsNoSensitiveData()
        {
            var token = await RegisterAndGetTokenAsync();
            AuthenticateClient(token);

            var response = await _client.PostAsJsonAsync("/api/chat/message",
                new ChatMessageDto("Hello Alfred"));
            var content = await response.Content.ReadAsStringAsync();

            content.Should().NotContain("passwordHash");
            content.Should().NotContain("PasswordHash");
        }
    }
}
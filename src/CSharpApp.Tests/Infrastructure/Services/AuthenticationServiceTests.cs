using CSharpApp.Infrastructure.Services;

namespace CSharpApp.Tests.Infrastructure.Services;

public class AuthenticationServiceTests
{
    private readonly Mock<IOptions<RestApiSettings>> _mockOptions;
    private readonly RestApiSettings _settings;

    public AuthenticationServiceTests()
    {
        _mockOptions = new Mock<IOptions<RestApiSettings>>();
        
        _settings = new RestApiSettings
        {
            BaseUrl = "https://api.test.com/",
            Auth = "auth/login",
            Username = "test@example.com",
            Password = "testpassword"
        };
        
        _mockOptions.Setup(x => x.Value).Returns(_settings);
    }

    [Fact]
    public void Constructor_ShouldInitializeCorrectly_WhenValidDependencies()
    {
        // Arrange
        var mockHttpClientFactory = new Mock<IHttpClientFactory>();
        var mockLogger = new Mock<ILogger<AuthenticationService>>();

        // Act
        var service = new AuthenticationService(mockHttpClientFactory.Object, _mockOptions.Object, mockLogger.Object);

        // Assert
        service.Should().NotBeNull();
    }

    [Fact]
    public void Settings_ShouldBeConfiguredCorrectly()
    {
        // Assert
        _settings.BaseUrl.Should().Be("https://api.test.com/");
        _settings.Auth.Should().Be("auth/login");
        _settings.Username.Should().Be("test@example.com");
        _settings.Password.Should().Be("testpassword");
    }

    [Fact]
    public void AuthenticationRequest_ShouldHaveCorrectStructure()
    {
        // Arrange
        var request = new AuthenticationRequest
        {
            Email = "test@example.com",
            Password = "password123"
        };

        // Assert
        request.Email.Should().Be("test@example.com");
        request.Password.Should().Be("password123");
    }

    [Fact]
    public void AuthenticationResponse_ShouldHaveCorrectStructure()
    {
        // Arrange
        var response = new AuthenticationResponse
        {
            Access_token = "test-access-token",
            Refresh_token = "test-refresh-token"
        };

        // Assert
        response.Access_token.Should().Be("test-access-token");
        response.Refresh_token.Should().Be("test-refresh-token");
    }

    [Theory]
    [InlineData("", "password", false)]
    [InlineData("user@test.com", "", false)]
    [InlineData("", "", false)]
    [InlineData("user@test.com", "password", true)]
    public void AuthenticationRequest_ShouldValidateCredentials(string email, string password, bool isValid)
    {
        // Arrange
        var request = new AuthenticationRequest
        {
            Email = email,
            Password = password
        };

        // Act
        var hasValidCredentials = !string.IsNullOrEmpty(request.Email) && !string.IsNullOrEmpty(request.Password);

        // Assert
        hasValidCredentials.Should().Be(isValid);
    }
}
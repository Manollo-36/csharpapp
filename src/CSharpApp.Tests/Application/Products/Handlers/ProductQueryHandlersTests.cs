using CSharpApp.Application.Products.Handlers;

namespace CSharpApp.Tests.Application.Products.Handlers;

public class ProductQueryHandlersTests
{
    private readonly Mock<IApiClient> _mockApiClient;
    private readonly Mock<IOptions<RestApiSettings>> _mockOptions;
    private readonly Mock<ILogger<GetProductsQueryHandler>> _mockLogger;
    private readonly GetProductsQueryHandler _handler;
    private readonly RestApiSettings _restApiSettings;

    public ProductQueryHandlersTests()
    {
        _mockApiClient = new Mock<IApiClient>();
        _mockOptions = new Mock<IOptions<RestApiSettings>>();
        _mockLogger = new Mock<ILogger<GetProductsQueryHandler>>();
        
        _restApiSettings = new RestApiSettings 
        { 
            Products = "products",
            BaseUrl = "https://api.example.com/"
        };
        
        _mockOptions.Setup(x => x.Value).Returns(_restApiSettings);
        
        _handler = new GetProductsQueryHandler(_mockApiClient.Object, _mockOptions.Object, _mockLogger.Object);
    }

    [Fact]
    public async Task Handle_GetProductsQuery_ShouldReturnProducts_WhenApiReturnsData()
    {
        // Arrange
        var expectedProducts = new List<Product>
        {
            new() { Id = 1, Title = "Product 1", Price = 10, Description = "Description 1" },
            new() { Id = 2, Title = "Product 2", Price = 20, Description = "Description 2" }
        };
        
        _mockApiClient
            .Setup(x => x.GetAsync<List<Product>>(It.IsAny<string>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(expectedProducts);

        var query = new GetProductsQuery();

        // Act
        var result = await _handler.Handle(query, CancellationToken.None);

        // Assert
        result.Should().NotBeNull();
        result.Should().HaveCount(2);
        result.Should().BeEquivalentTo(expectedProducts);
        
        _mockApiClient.Verify(x => x.GetAsync<List<Product>>(It.IsAny<string>(), It.IsAny<CancellationToken>()), Times.Once);
    }

    [Fact]
    public async Task Handle_GetProductsQuery_ShouldReturnEmpty_WhenApiReturnsNull()
    {
        // Arrange
        _mockApiClient
            .Setup(x => x.GetAsync<List<Product>>(It.IsAny<string>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync((List<Product>?)null);

        var query = new GetProductsQuery();

        // Act
        var result = await _handler.Handle(query, CancellationToken.None);

        // Assert
        result.Should().NotBeNull();
        result.Should().BeEmpty();
    }

    [Fact]
    public async Task Handle_GetProductsQuery_ShouldThrowException_WhenApiThrows()
    {
        // Arrange
        _mockApiClient
            .Setup(x => x.GetAsync<List<Product>>(It.IsAny<string>(), It.IsAny<CancellationToken>()))
            .ThrowsAsync(new HttpRequestException("External API error"));

        var query = new GetProductsQuery();

        // Act & Assert
        await _handler.Invoking(x => x.Handle(query, CancellationToken.None))
            .Should().ThrowAsync<HttpRequestException>()
            .WithMessage("External API error");
    }
}

public class GetProductByIdQueryHandlerTests
{
    private readonly Mock<IApiClient> _mockApiClient;
    private readonly Mock<IOptions<RestApiSettings>> _mockOptions;
    private readonly Mock<ILogger<GetProductByIdQueryHandler>> _mockLogger;
    private readonly GetProductByIdQueryHandler _handler;
    private readonly RestApiSettings _restApiSettings;

    public GetProductByIdQueryHandlerTests()
    {
        _mockApiClient = new Mock<IApiClient>();
        _mockOptions = new Mock<IOptions<RestApiSettings>>();
        _mockLogger = new Mock<ILogger<GetProductByIdQueryHandler>>();
        
        _restApiSettings = new RestApiSettings 
        { 
            Products = "products",
            BaseUrl = "https://api.example.com/"
        };
        
        _mockOptions.Setup(x => x.Value).Returns(_restApiSettings);
        
        _handler = new GetProductByIdQueryHandler(_mockApiClient.Object, _mockOptions.Object, _mockLogger.Object);
    }

    [Fact]
    public async Task Handle_GetProductByIdQuery_ShouldReturnProduct_WhenProductExists()
    {
        // Arrange
        var productId = 1;
        var expectedProduct = new Product 
        { 
            Id = productId, 
            Title = "Test Product", 
            Price = 19, 
            Description = "Test Description" 
        };

        _mockApiClient
            .Setup(x => x.GetAsync<Product>(It.IsAny<string>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(expectedProduct);

        var query = new GetProductByIdQuery(productId);

        // Act
        var result = await _handler.Handle(query, CancellationToken.None);

        // Assert
        result.Should().NotBeNull();
        result.Should().BeEquivalentTo(expectedProduct);
        
        _mockApiClient.Verify(x => x.GetAsync<Product>(It.IsAny<string>(), It.IsAny<CancellationToken>()), Times.Once);
    }

    [Fact]
    public async Task Handle_GetProductByIdQuery_ShouldReturnNull_WhenProductDoesNotExist()
    {
        // Arrange
        var productId = 999;
        
        _mockApiClient
            .Setup(x => x.GetAsync<Product>(It.IsAny<string>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync((Product?)null);

        var query = new GetProductByIdQuery(productId);

        // Act
        var result = await _handler.Handle(query, CancellationToken.None);

        // Assert
        result.Should().BeNull();
        
        _mockApiClient.Verify(x => x.GetAsync<Product>(It.IsAny<string>(), It.IsAny<CancellationToken>()), Times.Once);
    }

    [Theory]
    [InlineData(0)]
    [InlineData(-1)]
    [InlineData(-100)]
    public async Task Handle_GetProductByIdQuery_ShouldHandleInvalidIds(int invalidId)
    {
        // Arrange
        _mockApiClient
            .Setup(x => x.GetAsync<Product>(It.IsAny<string>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync((Product?)null);

        var query = new GetProductByIdQuery(invalidId);

        // Act
        var result = await _handler.Handle(query, CancellationToken.None);

        // Assert
        result.Should().BeNull();
    }
}
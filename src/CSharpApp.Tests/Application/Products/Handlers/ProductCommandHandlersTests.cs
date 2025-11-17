using CSharpApp.Application.Products.Handlers;

namespace CSharpApp.Tests.Application.Products.Handlers;

public class ProductCommandHandlersTests
{
    private readonly Mock<IApiClient> _mockApiClient;
    private readonly Mock<IOptions<RestApiSettings>> _mockOptions;
    private readonly Mock<ILogger<CreateProductCommandHandler>> _mockCreateLogger;
    private readonly Mock<ILogger<UpdateProductCommandHandler>> _mockUpdateLogger;
    private readonly Mock<ILogger<DeleteProductCommandHandler>> _mockDeleteLogger;
    private readonly RestApiSettings _restApiSettings;

    public ProductCommandHandlersTests()
    {
        _mockApiClient = new Mock<IApiClient>();
        _mockOptions = new Mock<IOptions<RestApiSettings>>();
        _mockCreateLogger = new Mock<ILogger<CreateProductCommandHandler>>();
        _mockUpdateLogger = new Mock<ILogger<UpdateProductCommandHandler>>();
        _mockDeleteLogger = new Mock<ILogger<DeleteProductCommandHandler>>();
        
        _restApiSettings = new RestApiSettings 
        { 
            Products = "products",
            BaseUrl = "https://api.example.com/"
        };
        
        _mockOptions.Setup(x => x.Value).Returns(_restApiSettings);
    }

    public class CreateProductCommandHandlerTests : ProductCommandHandlersTests
    {
        private readonly CreateProductCommandHandler _handler;

        public CreateProductCommandHandlerTests()
        {
            _handler = new CreateProductCommandHandler(_mockApiClient.Object, _mockOptions.Object, _mockCreateLogger.Object);
        }

        [Fact]
        public async Task Handle_CreateProductCommand_ShouldCreateProduct_WhenValidRequest()
        {
            // Arrange
            var command = new CreateProductCommand("Test Product", 29m, "Test Description", 1, new List<string> { "image1.jpg" });
            var expectedProduct = new Product
            {
                Id = 123,
                Title = command.Title,
                Price = (int?)command.Price,
                Description = command.Description
            };

            _mockApiClient
                .Setup(x => x.PostAsync<Product>(It.IsAny<string>(), It.IsAny<object>(), It.IsAny<CancellationToken>()))
                .ReturnsAsync(expectedProduct);

            // Act
            var result = await _handler.Handle(command, CancellationToken.None);

            // Assert
            result.Should().NotBeNull();
            result.Should().BeEquivalentTo(expectedProduct);
            
            _mockApiClient.Verify(x => x.PostAsync<Product>(It.IsAny<string>(), It.IsAny<object>(), It.IsAny<CancellationToken>()), Times.Once);
        }

        [Fact]
        public async Task Handle_CreateProductCommand_ShouldReturnNull_WhenApiReturnsNull()
        {
            // Arrange
            var command = new CreateProductCommand("Test Product", 29m, "Test Description", 1, new List<string>());

            _mockApiClient
                .Setup(x => x.PostAsync<Product>(It.IsAny<string>(), It.IsAny<object>(), It.IsAny<CancellationToken>()))
                .ReturnsAsync((Product?)null);

            // Act
            var result = await _handler.Handle(command, CancellationToken.None);

            // Assert
            result.Should().BeNull();
        }

        [Fact]
        public async Task Handle_CreateProductCommand_ShouldThrowException_WhenApiThrows()
        {
            // Arrange
            var command = new CreateProductCommand("Test Product", 29m, "Test Description", 1, new List<string>());

            _mockApiClient
                .Setup(x => x.PostAsync<Product>(It.IsAny<string>(), It.IsAny<object>(), It.IsAny<CancellationToken>()))
                .ThrowsAsync(new HttpRequestException("API Error"));

            // Act & Assert
            await _handler.Invoking(x => x.Handle(command, CancellationToken.None))
                .Should().ThrowAsync<HttpRequestException>()
                .WithMessage("API Error");
        }
    }

    public class UpdateProductCommandHandlerTests : ProductCommandHandlersTests
    {
        private readonly UpdateProductCommandHandler _handler;

        public UpdateProductCommandHandlerTests()
        {
            _handler = new UpdateProductCommandHandler(_mockApiClient.Object, _mockOptions.Object, _mockUpdateLogger.Object);
        }

        [Fact]
        public async Task Handle_UpdateProductCommand_ShouldUpdateProduct_WhenValidRequest()
        {
            // Arrange
            var productId = 123;
            var command = new UpdateProductCommand(productId, "Updated Product", 39m, "Updated Description", 1, new List<string>());
            var expectedProduct = new Product
            {
                Id = productId,
                Title = command.Title,
                Price = (int?)command.Price,
                Description = command.Description
            };

            _mockApiClient
                .Setup(x => x.PutAsync<Product>(It.IsAny<string>(), It.IsAny<object>(), It.IsAny<CancellationToken>()))
                .ReturnsAsync(expectedProduct);

            // Act
            var result = await _handler.Handle(command, CancellationToken.None);

            // Assert
            result.Should().NotBeNull();
            result.Should().BeEquivalentTo(expectedProduct);
            
            _mockApiClient.Verify(x => x.PutAsync<Product>(It.IsAny<string>(), It.IsAny<object>(), It.IsAny<CancellationToken>()), Times.Once);
        }

        [Fact]
        public async Task Handle_UpdateProductCommand_ShouldReturnNull_WhenProductNotFound()
        {
            // Arrange
            var command = new UpdateProductCommand(999, "Updated Product", 39m, "Updated Description", 1, new List<string>());

            _mockApiClient
                .Setup(x => x.PutAsync<Product>(It.IsAny<string>(), It.IsAny<object>(), It.IsAny<CancellationToken>()))
                .ReturnsAsync((Product?)null);

            // Act
            var result = await _handler.Handle(command, CancellationToken.None);

            // Assert
            result.Should().BeNull();
        }
    }

    public class DeleteProductCommandHandlerTests : ProductCommandHandlersTests
    {
        private readonly DeleteProductCommandHandler _handler;

        public DeleteProductCommandHandlerTests()
        {
            _handler = new DeleteProductCommandHandler(_mockApiClient.Object, _mockOptions.Object, _mockDeleteLogger.Object);
        }

        [Fact]
        public async Task Handle_DeleteProductCommand_ShouldReturnTrue_WhenProductDeleted()
        {
            // Arrange
            var productId = 123;
            var command = new DeleteProductCommand(productId);

            _mockApiClient
                .Setup(x => x.DeleteAsync(It.IsAny<string>(), It.IsAny<CancellationToken>()))
                .ReturnsAsync(true);

            // Act
            var result = await _handler.Handle(command, CancellationToken.None);

            // Assert
            result.Should().BeTrue();
            
            _mockApiClient.Verify(x => x.DeleteAsync(It.IsAny<string>(), It.IsAny<CancellationToken>()), Times.Once);
        }

        [Fact]
        public async Task Handle_DeleteProductCommand_ShouldReturnFalse_WhenProductNotFound()
        {
            // Arrange
            var productId = 999;
            var command = new DeleteProductCommand(productId);

            _mockApiClient
                .Setup(x => x.DeleteAsync(It.IsAny<string>(), It.IsAny<CancellationToken>()))
                .ReturnsAsync(false);

            // Act
            var result = await _handler.Handle(command, CancellationToken.None);

            // Assert
            result.Should().BeFalse();
        }

        [Fact]
        public async Task Handle_DeleteProductCommand_ShouldThrowException_WhenApiThrows()
        {
            // Arrange
            var command = new DeleteProductCommand(123);

            _mockApiClient
                .Setup(x => x.DeleteAsync(It.IsAny<string>(), It.IsAny<CancellationToken>()))
                .ThrowsAsync(new HttpRequestException("API Error"));

            // Act & Assert
            await _handler.Invoking(x => x.Handle(command, CancellationToken.None))
                .Should().ThrowAsync<HttpRequestException>()
                .WithMessage("API Error");
        }
    }
}
using FluentAssertions;
using MailSender.Application.DTOs.ClientApps;
using MailSender.Application.Interfaces;
using MailSender.Application.Services;
using MailSender.Domain.Entities;
using NSubstitute;
using Xunit;

namespace MailSender.UnitTests.Services;

public class ClientApplicationServiceTests
{
    private readonly IClientApplicationRepository _repository;
    private readonly IJwtTokenService _jwtTokenService;
    private readonly IRegistrationPasswordValidator _passwordValidator;
    private readonly ClientApplicationService _service;

    public ClientApplicationServiceTests()
    {
        _repository = Substitute.For<IClientApplicationRepository>();
        _jwtTokenService = Substitute.For<IJwtTokenService>();
        _passwordValidator = Substitute.For<IRegistrationPasswordValidator>();

        _service = new ClientApplicationService(
            _repository,
            _jwtTokenService,
            _passwordValidator);
    }

    [Fact]
    public async Task RegisterAsync_WhenPasswordIsInvalid_ReturnsFailure()
    {
        // Arrange
        const string expectedError = "Invalid registration password";

        var request = new RegisterClientAppRequest(
            "test-app-id",
            "Test Application",
            "invalid-password");

        _passwordValidator
            .IsValid(Arg.Any<string>())
            .Returns(false);

        _passwordValidator
            .GetInvalidPasswordMessage()
            .Returns(expectedError);

        // Act
        var result = await _service.RegisterAsync(request);

        // Assert
        result.IsSuccess.Should().BeFalse();
        result.Data.Should().BeNull();
        result.Error.Should().Be(expectedError);

        _passwordValidator
            .Received(1)
            .IsValid(request.Pass);

        _passwordValidator
            .Received(1)
            .GetInvalidPasswordMessage();

        await _repository
            .DidNotReceive()
            .GetByAppIdAsync(Arg.Any<string>());

        await _repository
            .DidNotReceive()
            .GetByAppNameAsync(Arg.Any<string>());

        await _repository
            .DidNotReceive()
            .AddAsync(Arg.Any<ClientApplication>());

        _jwtTokenService
            .DidNotReceive()
            .GenerateToken(Arg.Any<ClientApplication>());
    }

    [Fact]
    public async Task RegisterAsync_WhenAppIdAlreadyExists_ReturnsFailure()
    {
        // Arrange
        var request = new RegisterClientAppRequest(
            "test-app-id",
            "Test Application",
            "correct-password");

        var existingApplication = new ClientApplication(
            request.AppId,
            "Existing Application");

        _passwordValidator
            .IsValid(request.Pass)
            .Returns(true);

        _repository
            .GetByAppIdAsync(request.AppId)
            .Returns(existingApplication);

        // Act
        var result = await _service.RegisterAsync(request);

        // Assert
        result.IsSuccess.Should().BeFalse();
        result.Data.Should().BeNull();
        result.Error.Should()
            .Be($"client app duplication. Existing {existingApplication.AppId} {existingApplication.AppName}");

        _passwordValidator
            .Received(1)
            .IsValid(request.Pass);

        await _repository
            .Received(1)
            .GetByAppIdAsync(request.AppId);

        await _repository
            .DidNotReceive()
            .GetByAppNameAsync(Arg.Any<string>());

        await _repository
            .DidNotReceive()
            .AddAsync(Arg.Any<ClientApplication>());

        _jwtTokenService
            .DidNotReceive()
            .GenerateToken(Arg.Any<ClientApplication>());
    }

    [Fact]
    public async Task RegisterAsync_WhenAppNameAlreadyExists_ReturnsFailure()
    {
        // Arrange
        var request = new RegisterClientAppRequest(
            "test-app-id",
            "Test Application",
            "correct-password");

        var existingApplication = new ClientApplication(
            "existing-app-id",
            request.AppName);

        _passwordValidator
            .IsValid(request.Pass)
            .Returns(true);

        _repository
            .GetByAppIdAsync(request.AppId)
            .Returns((ClientApplication?)null);

        _repository
            .GetByAppNameAsync(request.AppName)
            .Returns(existingApplication);

        // Act
        var result = await _service.RegisterAsync(request);

        // Assert
        result.IsSuccess.Should().BeFalse();
        result.Data.Should().BeNull();
        result.Error.Should()
            .Be($"client app duplication. Existing {existingApplication.AppId} {existingApplication.AppName}");

        _passwordValidator
            .Received(1)
            .IsValid(request.Pass);

        await _repository
            .Received(1)
            .GetByAppIdAsync(request.AppId);

        await _repository
            .Received(1)
            .GetByAppNameAsync(request.AppName);

        await _repository
            .DidNotReceive()
            .AddAsync(Arg.Any<ClientApplication>());

        _jwtTokenService
            .DidNotReceive()
            .GenerateToken(Arg.Any<ClientApplication>());
    }

    [Fact]
    public async Task RegisterAsync_WhenRegistrationSucceeds_ReturnsSuccess()
    {
        // Arrange
        const string expectedToken = "generated-jwt-token";

        var request = new RegisterClientAppRequest(
            "test-app-id",
            "Test Application",
            "correct-password");

        _passwordValidator
            .IsValid(request.Pass)
            .Returns(true);

        _repository
            .GetByAppIdAsync(request.AppId)
            .Returns((ClientApplication?)null);

        _repository
            .GetByAppNameAsync(request.AppName)
            .Returns((ClientApplication?)null);

        _jwtTokenService
            .GenerateToken(Arg.Any<ClientApplication>())
            .Returns(expectedToken);

        // Act
        var result = await _service.RegisterAsync(request);

        // Assert
        result.IsSuccess.Should().BeTrue();
        result.Error.Should().BeNull();

        result.Data.Should().NotBeNull();
        result.Data!.AppId.Should().Be(request.AppId);
        result.Data.AppName.Should().Be(request.AppName);
        result.Data.Key.Should().Be(expectedToken);

        _passwordValidator
            .Received(1)
            .IsValid(request.Pass);

        await _repository
            .Received(1)
            .GetByAppIdAsync(request.AppId);

        await _repository
            .Received(1)
            .GetByAppNameAsync(request.AppName);

        await _repository
            .Received(1)
            .AddAsync(Arg.Is<ClientApplication>(application =>
                application.AppId == request.AppId &&
                application.AppName == request.AppName));

        _jwtTokenService
            .Received(1)
            .GenerateToken(Arg.Is<ClientApplication>(application =>
                application.AppId == request.AppId &&
                application.AppName == request.AppName));
    }
}
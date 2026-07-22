using AiVideoStudio.Application.Configuration;
using AiVideoStudio.Application.Events;
using AiVideoStudio.Application.Features.Media.Commands;
using AiVideoStudio.Application.Features.Media.Handlers;
using AiVideoStudio.Application.Features.Media.Mappings;
using AiVideoStudio.Application.Interfaces;
using AiVideoStudio.Application.Storage;
using AiVideoStudio.Domain.Entities;
using AiVideoStudio.Domain.Enums;
using AiVideoStudio.Domain.Events.Media;
using AiVideoStudio.Domain.Interfaces;
using AiVideoStudio.Shared.DomainErrors;
using AutoMapper;
using FluentAssertions;
using Microsoft.Extensions.Options;
using NSubstitute;
using System;
using System.Collections.Generic;
using System.IO;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using Xunit;

namespace AiVideoStudio.UnitTests.Features.Media;

public class UploadMediaCommandHandlerTests
{
    private readonly IMediaAssetRepository _mediaAssetRepository = Substitute.For<IMediaAssetRepository>();
    private readonly IProjectRepository _projectRepository = Substitute.For<IProjectRepository>();
    private readonly IStorageProvider _storageProvider = Substitute.For<IStorageProvider>();
    private readonly ICurrentUser _currentUser = Substitute.For<ICurrentUser>();
    private readonly IMapper _mapper;
    private readonly IEventBus _eventBus = Substitute.For<IEventBus>();
    private readonly IOptions<StorageOptions> _storageOptions;

    private readonly UploadMediaCommandHandler _handler;

    public UploadMediaCommandHandlerTests()
    {
        var config = new MapperConfiguration(cfg => cfg.AddProfile<MediaProfile>());
        _mapper = config.CreateMapper();

        _storageOptions = Options.Create(new StorageOptions
        {
            MaxFileSizeBytes = 100 * 1024 * 1024, // 100 MB
            AllowedExtensions = new List<string> { ".jpg", ".mp4", ".mp3", ".vtt" },
            AllowedMimeTypes = new List<string> { "image/jpeg", "video/mp4", "audio/mpeg", "text/vtt" }
        });

        _handler = new UploadMediaCommandHandler(
            _mediaAssetRepository,
            _projectRepository,
            _storageProvider,
            _currentUser,
            _mapper,
            _eventBus,
            _storageOptions);
    }

    [Fact]
    public async Task Handle_ShouldUploadMediaSuccessfully_WhenValidRequestAndAuthorizedUser()
    {
        // Arrange
        var userId = "user_owner_123";
        var projectId = "proj_001";
        var project = Project.Create("Demo Project", userId);

        _currentUser.IsAuthenticated.Returns(true);
        _currentUser.UserId.Returns(userId);
        _currentUser.Roles.Returns(new List<string> { "User" });

        _projectRepository.GetByIdAsync(projectId, Arg.Any<CancellationToken>()).Returns(project);
        _storageProvider.UploadAsync(projectId, Arg.Any<string>(), Arg.Any<Stream>(), "image/jpeg", Arg.Any<CancellationToken>())
            .Returns("proj_001/random_guid.jpg");

        using var stream = new MemoryStream(Encoding.UTF8.GetBytes("sample image bytes"));
        var command = new UploadMediaCommand(projectId, "avatar.jpg", "image/jpeg", 1024, stream);

        // Act
        var result = await _handler.Handle(command, CancellationToken.None);

        // Assert
        result.IsSuccess.Should().BeTrue();
        result.Value.Should().NotBeNull();
        result.Value!.OriginalFileName.Should().Be("avatar.jpg");
        result.Value.AssetType.Should().Be(AssetType.Image);
        result.Value.Status.Should().Be(MediaStatus.Ready);

        await _mediaAssetRepository.Received(1).AddAsync(Arg.Is<MediaAsset>(m => m.ProjectId == projectId && m.OwnerId == userId), Arg.Any<CancellationToken>());
        await _eventBus.Received(1).PublishAsync(Arg.Is<MediaUploadedEvent>(e => e.ProjectId == projectId), Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task Handle_ShouldReturnFailure_WhenUserIsNotAuthenticated()
    {
        // Arrange
        _currentUser.IsAuthenticated.Returns(false);
        using var stream = new MemoryStream(new byte[] { 1, 2, 3 });
        var command = new UploadMediaCommand("p1", "test.mp4", "video/mp4", 500, stream);

        // Act
        var result = await _handler.Handle(command, CancellationToken.None);

        // Assert
        result.IsSuccess.Should().BeFalse();
        result.Error.Should().Be(AuthErrors.Unauthorized);
    }

    [Fact]
    public async Task Handle_ShouldReturnFailure_WhenProjectNotFound()
    {
        // Arrange
        _currentUser.IsAuthenticated.Returns(true);
        _currentUser.UserId.Returns("user_1");

        _projectRepository.GetByIdAsync("missing_proj", Arg.Any<CancellationToken>()).Returns((Project?)null);

        using var stream = new MemoryStream(new byte[] { 1, 2, 3 });
        var command = new UploadMediaCommand("missing_proj", "test.jpg", "image/jpeg", 500, stream);

        // Act
        var result = await _handler.Handle(command, CancellationToken.None);

        // Assert
        result.IsSuccess.Should().BeFalse();
        result.Error.Should().Be(MediaErrors.ProjectNotFound);
    }

    [Fact]
    public async Task Handle_ShouldReturnFailure_WhenUserIsNotOwnerNorAdmin()
    {
        // Arrange
        var ownerId = "owner_123";
        var strangerId = "stranger_456";
        var project = Project.Create("Demo Project", ownerId);

        _currentUser.IsAuthenticated.Returns(true);
        _currentUser.UserId.Returns(strangerId);
        _currentUser.Roles.Returns(new List<string> { "User" });

        _projectRepository.GetByIdAsync(project.Id, Arg.Any<CancellationToken>()).Returns(project);

        using var stream = new MemoryStream(new byte[] { 1, 2, 3 });
        var command = new UploadMediaCommand(project.Id, "test.jpg", "image/jpeg", 500, stream);

        // Act
        var result = await _handler.Handle(command, CancellationToken.None);

        // Assert
        result.IsSuccess.Should().BeFalse();
        result.Error.Should().Be(MediaErrors.UnauthorizedAccess);
    }

    [Fact]
    public async Task Handle_ShouldReturnFailure_WhenFileExceedsMaxSize()
    {
        // Arrange
        var userId = "user_owner_123";
        var project = Project.Create("Demo Project", userId);

        _currentUser.IsAuthenticated.Returns(true);
        _currentUser.UserId.Returns(userId);

        _projectRepository.GetByIdAsync(project.Id, Arg.Any<CancellationToken>()).Returns(project);

        using var stream = new MemoryStream(new byte[] { 1, 2, 3 });
        var overflowSize = 200 * 1024 * 1024; // 200 MB > 100 MB max
        var command = new UploadMediaCommand(project.Id, "large.mp4", "video/mp4", overflowSize, stream);

        // Act
        var result = await _handler.Handle(command, CancellationToken.None);

        // Assert
        result.IsSuccess.Should().BeFalse();
        result.Error.Should().Be(MediaErrors.FileTooLarge);
    }

    [Fact]
    public async Task Handle_ShouldReturnFailure_WhenInvalidExtensionOrMimeType()
    {
        // Arrange
        var userId = "user_owner_123";
        var project = Project.Create("Demo Project", userId);

        _currentUser.IsAuthenticated.Returns(true);
        _currentUser.UserId.Returns(userId);

        _projectRepository.GetByIdAsync(project.Id, Arg.Any<CancellationToken>()).Returns(project);

        using var stream = new MemoryStream(new byte[] { 1, 2, 3 });
        var command = new UploadMediaCommand(project.Id, "dangerous_script.exe", "application/x-msdownload", 500, stream);

        // Act
        var result = await _handler.Handle(command, CancellationToken.None);

        // Assert
        result.IsSuccess.Should().BeFalse();
        result.Error.Should().Be(MediaErrors.InvalidFileType);
    }
}

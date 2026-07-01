using FluentAssertions;
using Moq;
using WrenchBox.Application.Common;
using WrenchBox.Application.Features.Parts;
using WrenchBox.Domain.Entities;
using WrenchBox.Domain.Repositories;

namespace WrenchBox.Application.Tests.Features;

public class PartCommandHandlerTests
{
    private readonly Mock<IUnitOfWork> _unitOfWork = new();

    [Fact]
    public async Task Create_Success_ReturnsDto()
    {
        var repo = new Mock<IPartRepository>();
        var handler = new CreatePartCommandHandler(repo.Object, _unitOfWork.Object);
        var result = await handler.Handle(
            new CreatePartCommand("Oil Filter", "FLT-001", 25m, 100, 10),
            CancellationToken.None);

        result.Sku.Should().Be("FLT-001");
        repo.Verify(r => r.AddAsync(It.IsAny<Part>(), It.IsAny<CancellationToken>()), Times.Once);
    }

    [Fact]
    public async Task Update_Success_ReturnsDto()
    {
        var part = Part.Create("Oil Filter", "FLT-001", 25m, 100, 10);
        var repo = new Mock<IPartRepository>();
        repo.Setup(r => r.GetByIdAsync(part.Id, It.IsAny<CancellationToken>())).ReturnsAsync(part);

        var handler = new UpdatePartCommandHandler(repo.Object, _unitOfWork.Object);
        var result = await handler.Handle(
            new UpdatePartCommand(part.Id, "Oil Filter Premium", 30m, 15, true),
            CancellationToken.None);

        result.UnitPrice.Should().Be(30m);
    }

    [Fact]
    public async Task Update_NotFound_Throws()
    {
        var repo = new Mock<IPartRepository>();
        repo.Setup(r => r.GetByIdAsync(It.IsAny<Guid>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync((Part?)null);

        var handler = new UpdatePartCommandHandler(repo.Object, _unitOfWork.Object);
        var act = async () => await handler.Handle(
            new UpdatePartCommand(Guid.NewGuid(), "X", 10m, 5, true),
            CancellationToken.None);

        await act.Should().ThrowAsync<NotFoundException>();
    }

    [Fact]
    public async Task AdjustStock_Success_ReturnsDto()
    {
        var part = Part.Create("Oil Filter", "FLT-001", 25m, 100, 10);
        var repo = new Mock<IPartRepository>();
        repo.Setup(r => r.GetByIdAsync(part.Id, It.IsAny<CancellationToken>())).ReturnsAsync(part);

        var handler = new AdjustPartStockCommandHandler(repo.Object, _unitOfWork.Object);
        var result = await handler.Handle(
            new AdjustPartStockCommand(part.Id, 5, "Restock"),
            CancellationToken.None);

        result.StockQuantity.Should().Be(105);
    }

    [Fact]
    public async Task AdjustStock_NotFound_Throws()
    {
        var repo = new Mock<IPartRepository>();
        repo.Setup(r => r.GetByIdForUpdateAsync(It.IsAny<Guid>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync((Part?)null);

        var handler = new AdjustPartStockCommandHandler(repo.Object, _unitOfWork.Object);
        var act = async () => await handler.Handle(
            new AdjustPartStockCommand(Guid.NewGuid(), 5, "Restock"),
            CancellationToken.None);

        await act.Should().ThrowAsync<NotFoundException>();
    }

    [Fact]
    public async Task Delete_Success()
    {
        var part = Part.Create("Oil Filter", "FLT-001", 25m, 100, 10);
        var repo = new Mock<IPartRepository>();
        repo.Setup(r => r.GetByIdAsync(part.Id, It.IsAny<CancellationToken>())).ReturnsAsync(part);

        var handler = new DeletePartCommandHandler(repo.Object, _unitOfWork.Object);
        await handler.Handle(new DeletePartCommand(part.Id), CancellationToken.None);

        repo.Verify(r => r.Remove(part), Times.Once);
    }

    [Fact]
    public async Task GetById_Success_ReturnsDto()
    {
        var part = Part.Create("Oil Filter", "FLT-001", 25m, 100, 10);
        var repo = new Mock<IPartRepository>();
        repo.Setup(r => r.GetByIdAsync(part.Id, It.IsAny<CancellationToken>())).ReturnsAsync(part);

        var handler = new GetPartByIdQueryHandler(repo.Object);
        var result = await handler.Handle(new GetPartByIdQuery(part.Id), CancellationToken.None);

        result.Name.Should().Be("Oil Filter");
    }

    [Fact]
    public async Task GetParts_ReturnsPagedResult()
    {
        var part = Part.Create("Oil Filter", "FLT-001", 25m, 100, 10);
        var repo = new Mock<IPartRepository>();
        repo.Setup(r => r.GetPagedAsync(1, 20, null, It.IsAny<CancellationToken>()))
            .ReturnsAsync((new List<Part> { part }, 1));

        var handler = new GetPartsQueryHandler(repo.Object);
        var result = await handler.Handle(new GetPartsQuery(), CancellationToken.None);

        result.Items.Should().HaveCount(1);
    }
}

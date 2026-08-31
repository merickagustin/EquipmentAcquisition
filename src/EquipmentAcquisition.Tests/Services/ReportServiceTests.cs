using EquipmentAcquisition.Core.Dtos;
using EquipmentAcquisition.Core.Exceptions;
using EquipmentAcquisition.Core.Repositories.Interfaces;
using EquipmentAcquisition.Core.Services;
using Moq;

namespace EquipmentAcquisition.Tests.Services;

public class ReportServiceTests
{
    private readonly Mock<IReportRepository> _repository = new();
    private readonly ReportService _sut;

    public ReportServiceTests()
    {
        _sut = new ReportService(_repository.Object);
    }

    // From/To are nullable specifically so a missing query param is distinguishable
    // from an explicit value — see DetailCacheRepository's identical fix. Without
    // this check, a missing param would default to DateTime.MinValue and crash with
    // a raw SqlTypeException instead of this clean 400.
    [Fact]
    public async Task GetDepartmentSpendAsync_MissingFrom_ThrowsValidationException()
    {
        await Assert.ThrowsAsync<ValidationException>(() => _sut.GetDepartmentSpendAsync(null, DateTime.UtcNow, null));

        _repository.Verify(r => r.GetDepartmentSpendAsync(It.IsAny<DateTime>(), It.IsAny<DateTime>(), It.IsAny<int?>()), Times.Never);
    }

    [Fact]
    public async Task GetDepartmentSpendAsync_MissingTo_ThrowsValidationException()
    {
        await Assert.ThrowsAsync<ValidationException>(() => _sut.GetDepartmentSpendAsync(DateTime.UtcNow, null, null));

        _repository.Verify(r => r.GetDepartmentSpendAsync(It.IsAny<DateTime>(), It.IsAny<DateTime>(), It.IsAny<int?>()), Times.Never);
    }

    [Fact]
    public async Task GetDepartmentSpendAsync_BothSupplied_CallsRepositoryWithResolvedValues()
    {
        var from = new DateTime(2026, 1, 1);
        var to = new DateTime(2026, 12, 31);
        _repository.Setup(r => r.GetDepartmentSpendAsync(from, to, 3)).ReturnsAsync([]);

        var result = await _sut.GetDepartmentSpendAsync(from, to, 3);

        Assert.Empty(result);
        _repository.Verify(r => r.GetDepartmentSpendAsync(from, to, 3), Times.Once);
    }
}

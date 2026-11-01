using NhatDucSoftware.Core.Services;

namespace NhatDucSoftware.Core.Tests;

public class HomeworkDriveFolderChoiceTests
{
    [Fact]
    public void UseExisting_KeepsTheStoredFolderAfterRename()
    {
        Assert.True(HomeworkDriveFolderChoice.UseExisting("folder-1"));
        Assert.False(HomeworkDriveFolderChoice.UseExisting("  "));
    }
}

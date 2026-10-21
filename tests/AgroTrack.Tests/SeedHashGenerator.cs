using AgroTrack.Security;
using Xunit;
using Xunit.Abstractions;

namespace AgroTrack.Tests;

public class SeedHashGenerator
{
    private readonly ITestOutputHelper _output;
    private readonly PasswordHasher _hasher = new();

    public SeedHashGenerator(ITestOutputHelper output)
    {
        _output = output;
    }

    [Fact]
    public void GenerateDemoHashes()
    {
        string adminHash = _hasher.HashPassword("AdminPassword@123");
        string managerHash = _hasher.HashPassword("ManagerPassword@123");
        string petugasHash = _hasher.HashPassword("PetugasPassword@123");
        string inactiveHash = _hasher.HashPassword("UserPassword@123");

        _output.WriteLine($"ADMIN_HASH={adminHash}");
        _output.WriteLine($"MANAGER_HASH={managerHash}");
        _output.WriteLine($"PETUGAS_HASH={petugasHash}");
        _output.WriteLine($"INACTIVE_HASH={inactiveHash}");

        Assert.True(_hasher.VerifyPassword("AdminPassword@123", adminHash));
        Assert.True(_hasher.VerifyPassword("ManagerPassword@123", managerHash));
        Assert.True(_hasher.VerifyPassword("PetugasPassword@123", petugasHash));
        Assert.True(_hasher.VerifyPassword("UserPassword@123", inactiveHash));
    }
}

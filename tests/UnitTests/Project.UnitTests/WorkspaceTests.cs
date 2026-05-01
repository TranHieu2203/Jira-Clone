using BB.Common;
using FluentAssertions;
using Project.Domain;

namespace Project.UnitTests;

public class WorkspaceTests
{
    [Fact]
    public void Constructor_ValidInput_OwnerBecomesMember()
    {
        var ownerId = Guid.NewGuid();
        var w = new Workspace("Acme", "acme", ownerId);

        w.Members.Should().ContainSingle();
        w.Members[0].UserId.Should().Be(ownerId);
        w.Members[0].Role.Should().Be(WorkspaceRole.Owner);
    }

    [Theory]
    [InlineData("ACME")]      // uppercase
    [InlineData("a")]         // too short
    [InlineData("-acme")]     // starts with hyphen
    [InlineData("acme!")]     // special char
    public void Constructor_InvalidSlug_Throws(string slug)
    {
        var act = () => new Workspace("X", slug, Guid.NewGuid());
        act.Should().Throw<DomainException>().Where(ex => ex.Code == ProjectErrors.WsSlugInvalid);
    }

    [Fact]
    public void AddMember_DuplicateUser_Throws()
    {
        var owner = Guid.NewGuid();
        var w = new Workspace("X", "x-corp", owner);

        var act = () => w.AddMember(owner, WorkspaceRole.Admin);
        act.Should().Throw<DomainException>().Where(ex => ex.Code == ProjectErrors.WsMemberDuplicated);
    }

    [Fact]
    public void RemoveMember_Owner_Throws()
    {
        var owner = Guid.NewGuid();
        var w = new Workspace("X", "x-corp", owner);

        var act = () => w.RemoveMember(owner);
        act.Should().Throw<DomainException>().Where(ex => ex.Code == ProjectErrors.WsCannotRemoveOwner);
    }

    [Fact]
    public void TransferOwnership_OldOwnerBecomesAdmin()
    {
        var oldOwner = Guid.NewGuid();
        var newOwner = Guid.NewGuid();
        var w = new Workspace("X", "x-corp", oldOwner);

        w.TransferOwnership(newOwner);

        w.OwnerId.Should().Be(newOwner);
        w.RoleOf(newOwner).Should().Be(WorkspaceRole.Owner);
        w.RoleOf(oldOwner).Should().Be(WorkspaceRole.Admin);
    }
}

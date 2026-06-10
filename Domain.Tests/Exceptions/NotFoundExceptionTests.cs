using Domain.Exceptions;
using Shouldly;

namespace Domain.Tests.Exceptions;

public class NotFoundExceptionTests
{
    [Fact]
    public void For_BuildsTheStandardEntityWithIdMessage()
    {
        var ex = NotFoundException.For("Item", 42);

        ex.Message.ShouldBe("Item with id 42 not found.");
    }

    [Fact]
    public void For_WorksWithNonIntegerIdentifiers()
    {
        var id = Guid.Parse("11111111-1111-1111-1111-111111111111");

        NotFoundException.For("Person", id)
            .Message.ShouldBe("Person with id 11111111-1111-1111-1111-111111111111 not found.");
    }

    [Fact]
    public void For_ReturnsANotFoundExceptionInstance()
    {
        NotFoundException.For("Room", 1).ShouldBeOfType<NotFoundException>();
    }
}

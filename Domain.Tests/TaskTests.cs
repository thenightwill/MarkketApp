using Domain.Entities;
using Domain.Enums;
using Domain.Exceptions;

namespace Market.Tests.Domain;

// TASK MODULE (GenAI demonstration)
// These tests were the first artifact written for the Task aggregate (RED step of TDD).
// They pin the two rules the assistant had to respect: the only legal state transitions
// are Pending -> InProgress -> Completed, and a task always belongs to exactly one user.
[TestClass]
public class TaskTests
{
    private static readonly DateTime DueDate = DateTime.UtcNow.AddDays(3);

    private static UserTask CreateTask(Guid? userId = null) =>
        UserTask.Create(userId ?? Guid.NewGuid(), "Revisar lotes por vencer", "Revisar bodega A", DueDate);

    [TestMethod]
    public void Create_ShouldCreatePendingTaskOwnedByUser()
    {
        var userId = Guid.NewGuid();

        var task = UserTask.Create(userId, "Revisar lotes por vencer", "Revisar bodega A", DueDate);

        Assert.AreNotEqual(Guid.Empty, task.Id);
        Assert.AreEqual(userId, task.UserId);
        Assert.AreEqual("Revisar lotes por vencer", task.Title);
        Assert.AreEqual("Revisar bodega A", task.Description);
        Assert.AreEqual(UserTaskStatus.Pending, task.Status);
        Assert.AreEqual(DueDate, task.DueDate);
        Assert.IsNull(task.UpdatedAt);
    }

    [TestMethod]
    public void Create_ShouldAllowMissingDescription()
    {
        var task = UserTask.Create(Guid.NewGuid(), "Titulo", null, DueDate);

        Assert.AreEqual(string.Empty, task.Description);
    }

    [TestMethod]
    public void Create_ShouldRejectEmptyTitle()
    {
        Assert.Throws<DomainException>(() => UserTask.Create(Guid.NewGuid(), string.Empty, "x", DueDate));
    }

    [TestMethod]
    public void Create_ShouldRejectWhitespaceTitle()
    {
        Assert.Throws<DomainException>(() => UserTask.Create(Guid.NewGuid(), "   ", "x", DueDate));
    }

    [TestMethod]
    public void Create_ShouldRejectEmptyUserId()
    {
        Assert.Throws<DomainException>(() => UserTask.Create(Guid.Empty, "Titulo", "x", DueDate));
    }

    [TestMethod]
    public void Start_ShouldMovePendingTaskToInProgress()
    {
        var task = CreateTask();

        task.Start();

        Assert.AreEqual(UserTaskStatus.InProgress, task.Status);
        Assert.IsNotNull(task.UpdatedAt);
    }

    [TestMethod]
    public void Complete_ShouldMoveInProgressTaskToCompleted()
    {
        var task = CreateTask();
        task.Start();

        task.Complete();

        Assert.AreEqual(UserTaskStatus.Completed, task.Status);
    }

    [TestMethod]
    public void Complete_ShouldRejectPendingTask()
    {
        var task = CreateTask();

        var ex = Assert.Throws<DomainException>(() => task.Complete());

        Assert.AreEqual(DomainErrorCodes.InvalidTaskTransition, ex.Code);
        Assert.AreEqual(UserTaskStatus.Pending, task.Status);
    }

    [TestMethod]
    public void Start_ShouldRejectInProgressTask()
    {
        var task = CreateTask();
        task.Start();

        var ex = Assert.Throws<DomainException>(() => task.Start());

        Assert.AreEqual(DomainErrorCodes.InvalidTaskTransition, ex.Code);
    }

    [TestMethod]
    public void Start_ShouldRejectCompletedTask()
    {
        var task = CreateTask();
        task.Start();
        task.Complete();

        Assert.Throws<DomainException>(() => task.Start());
    }

    [TestMethod]
    public void Complete_ShouldRejectCompletedTask()
    {
        var task = CreateTask();
        task.Start();
        task.Complete();

        Assert.Throws<DomainException>(() => task.Complete());
    }

    [TestMethod]
    public void TransitionTo_ShouldFollowTheAllowedSequence()
    {
        var task = CreateTask();

        task.TransitionTo(UserTaskStatus.InProgress);
        task.TransitionTo(UserTaskStatus.Completed);

        Assert.AreEqual(UserTaskStatus.Completed, task.Status);
    }

    [TestMethod]
    public void TransitionTo_ShouldRejectSkippingInProgress()
    {
        var task = CreateTask();

        var ex = Assert.Throws<DomainException>(() => task.TransitionTo(UserTaskStatus.Completed));

        Assert.AreEqual(DomainErrorCodes.InvalidTaskTransition, ex.Code);
    }

    [TestMethod]
    public void TransitionTo_ShouldRejectGoingBackwards()
    {
        var task = CreateTask();
        task.Start();

        Assert.Throws<DomainException>(() => task.TransitionTo(UserTaskStatus.Pending));
    }

    [TestMethod]
    public void TransitionTo_ShouldBeNoOpWhenStatusDoesNotChange()
    {
        var task = CreateTask();

        task.TransitionTo(UserTaskStatus.Pending);

        Assert.AreEqual(UserTaskStatus.Pending, task.Status);
        Assert.IsNull(task.UpdatedAt);
    }

    [TestMethod]
    public void UpdateDetails_ShouldUpdateTitleDescriptionAndDueDate()
    {
        var task = CreateTask();
        var newDueDate = DueDate.AddDays(5);

        task.UpdateDetails("Nuevo titulo", "Nueva descripcion", newDueDate);

        Assert.AreEqual("Nuevo titulo", task.Title);
        Assert.AreEqual("Nueva descripcion", task.Description);
        Assert.AreEqual(newDueDate, task.DueDate);
        Assert.IsNotNull(task.UpdatedAt);
    }

    [TestMethod]
    public void UpdateDetails_ShouldNotModifyTaskWhenTitleIsInvalid()
    {
        var task = CreateTask();

        Assert.Throws<DomainException>(() => task.UpdateDetails(" ", "Nueva descripcion", DueDate.AddDays(5)));

        Assert.AreEqual("Revisar lotes por vencer", task.Title);
        Assert.AreEqual("Revisar bodega A", task.Description);
        Assert.AreEqual(DueDate, task.DueDate);
        Assert.IsNull(task.UpdatedAt);
    }

    [TestMethod]
    public void IsOwnedBy_ShouldOnlyMatchTheOwner()
    {
        var owner = Guid.NewGuid();
        var task = CreateTask(owner);

        Assert.IsTrue(task.IsOwnedBy(owner));
        Assert.IsFalse(task.IsOwnedBy(Guid.NewGuid()));
    }
}

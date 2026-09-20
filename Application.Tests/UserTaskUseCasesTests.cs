using Application.Common;
using Application.Tasks;
using Domain.Enums;
using Domain.Exceptions;
using Market.Tests.Application.Fakes;

namespace Market.Tests.Application;

// TASK MODULE (GenAI demonstration)
// The critical review of the AI output focused on one thing: every use case must go through the current
// user's id. These tests build two users and check that nothing leaks from one to the other, including
// for administrators, because the requirement says a user must not access tasks of other users.
[TestClass]
public class UserTaskUseCasesTests
{
    private static readonly DateTime Due = DateTime.UtcNow.AddDays(3);

    private readonly InMemoryUserTaskRepository _tasks = new();
    private readonly FakeUnitOfWork _uow = new();
    private readonly FakeCurrentUser _me = new();
    private readonly FakeCurrentUser _other = new();

    private CreateUserTaskUseCase Create(FakeCurrentUser user) => new(_tasks, _uow, user);

    private UpdateUserTaskUseCase Update(FakeCurrentUser user) => new(_tasks, _uow, user);

    private GetUserTasksUseCase List(FakeCurrentUser user) => new(_tasks, user);

    private GetUserTaskByIdUseCase Get(FakeCurrentUser user) => new(_tasks, user);

    private DeleteUserTaskUseCase Delete(FakeCurrentUser user) => new(_tasks, _uow, user);

    [TestMethod]
    public async Task Create_ShouldAssignTheTaskToTheCurrentUser()
    {
        var response = await Create(_me).ExecuteAsync(new CreateUserTaskRequest("Revisar lotes", "Bodega A", Due));

        var task = _tasks.Items.Single();
        Assert.AreEqual(_me.UserId, task.UserId);
        Assert.AreEqual(_me.UserId, response.UserId);
        Assert.AreEqual("Pending", response.Status);
        Assert.AreEqual(1, _uow.SaveCount);
    }

    [TestMethod]
    public async Task Create_ShouldRejectEmptyTitle()
    {
        await Assert.ThrowsAsync<DomainException>(() => Create(_me).ExecuteAsync(new CreateUserTaskRequest(" ", null, Due)));

        Assert.IsEmpty(_tasks.Items);
    }

    [TestMethod]
    public async Task List_ShouldReturnOnlyTheTasksOfTheCurrentUser()
    {
        await Create(_me).ExecuteAsync(new CreateUserTaskRequest("Mia", null, Due));
        await Create(_other).ExecuteAsync(new CreateUserTaskRequest("Ajena", null, Due));

        var result = await List(_me).ExecuteAsync();

        Assert.AreEqual("Mia", result.Single().Title);
    }

    [TestMethod]
    public async Task List_ShouldNotExposeTasksToAdministratorsEither()
    {
        _me.Role = UserRole.Administrator;
        await Create(_other).ExecuteAsync(new CreateUserTaskRequest("Ajena", null, Due));

        var result = await List(_me).ExecuteAsync();

        Assert.IsEmpty(result);
    }

    [TestMethod]
    public async Task Get_ShouldReturnNotFoundForTasksOfOtherUsers()
    {
        var theirs = await Create(_other).ExecuteAsync(new CreateUserTaskRequest("Ajena", null, Due));

        var ex = await Assert.ThrowsAsync<AppException>(() => Get(_me).ExecuteAsync(theirs.Id));

        Assert.AreEqual(AppErrorCodes.TaskNotFound, ex.Code);
        Assert.AreEqual(AppErrorKind.NotFound, ex.Kind);
    }

    [TestMethod]
    public async Task Get_ShouldReturnOwnTask()
    {
        var mine = await Create(_me).ExecuteAsync(new CreateUserTaskRequest("Mia", "desc", Due));

        var result = await Get(_me).ExecuteAsync(mine.Id);

        Assert.AreEqual("Mia", result.Title);
    }

    [TestMethod]
    public async Task Update_ShouldChangeDetailsAndFollowTheStateMachine()
    {
        var mine = await Create(_me).ExecuteAsync(new CreateUserTaskRequest("Mia", "desc", Due));

        var started = await Update(_me).ExecuteAsync(mine.Id, new UpdateUserTaskRequest("Mia editada", "nueva", Due.AddDays(1), UserTaskStatus.InProgress));
        var completed = await Update(_me).ExecuteAsync(mine.Id, new UpdateUserTaskRequest("Mia editada", "nueva", Due.AddDays(1), UserTaskStatus.Completed));

        Assert.AreEqual("Mia editada", started.Title);
        Assert.AreEqual("InProgress", started.Status);
        Assert.AreEqual("Completed", completed.Status);
    }

    [TestMethod]
    public async Task Update_ShouldRejectInvalidTransitionAndKeepTheTaskUntouched()
    {
        var mine = await Create(_me).ExecuteAsync(new CreateUserTaskRequest("Mia", "desc", Due));

        var ex = await Assert.ThrowsAsync<DomainException>(() =>
            Update(_me).ExecuteAsync(mine.Id, new UpdateUserTaskRequest("Cambiada", "otra", Due.AddDays(9), UserTaskStatus.Completed)));

        Assert.AreEqual(DomainErrorCodes.InvalidTaskTransition, ex.Code);
        var task = _tasks.Items.Single();
        Assert.AreEqual("Mia", task.Title);
        Assert.AreEqual(UserTaskStatus.Pending, task.Status);
    }

    [TestMethod]
    public async Task Update_ShouldReturnNotFoundForTasksOfOtherUsers()
    {
        var theirs = await Create(_other).ExecuteAsync(new CreateUserTaskRequest("Ajena", null, Due));

        var ex = await Assert.ThrowsAsync<AppException>(() =>
            Update(_me).ExecuteAsync(theirs.Id, new UpdateUserTaskRequest("Hack", null, Due, UserTaskStatus.Pending)));

        Assert.AreEqual(AppErrorCodes.TaskNotFound, ex.Code);
        Assert.AreEqual("Ajena", _tasks.Items.Single().Title);
    }

    [TestMethod]
    public async Task Delete_ShouldRemoveOwnTask()
    {
        var mine = await Create(_me).ExecuteAsync(new CreateUserTaskRequest("Mia", null, Due));

        await Delete(_me).ExecuteAsync(mine.Id);

        Assert.IsEmpty(_tasks.Items);
    }

    [TestMethod]
    public async Task Delete_ShouldReturnNotFoundForTasksOfOtherUsers()
    {
        var theirs = await Create(_other).ExecuteAsync(new CreateUserTaskRequest("Ajena", null, Due));

        var ex = await Assert.ThrowsAsync<AppException>(() => Delete(_me).ExecuteAsync(theirs.Id));

        Assert.AreEqual(AppErrorCodes.TaskNotFound, ex.Code);
        Assert.HasCount(1, _tasks.Items);
    }
}

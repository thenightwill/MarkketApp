using System.Net;
using Application.Tasks;
using Domain.Enums;

namespace Market.Tests.Api;

// TASK MODULE (GenAI demonstration)
// The API-level tests exist to prove the security requirement end to end: over HTTP, with real JWTs,
// one user must never be able to read, change or delete another user's tasks (not even an administrator).
[TestClass]
public class TasksApiTests
{
    private static async Task<UserTaskResponse> CreateAsync(HttpClient client, string title = "Revisar lotes")
    {
        var response = await client.PostJsonAsync("/api/tasks", new CreateUserTaskRequest(title, "detalle", DateTime.UtcNow.AddDays(2)));
        response.EnsureSuccessStatusCode();
        return await response.ReadAsync<UserTaskResponse>();
    }

    private static UpdateUserTaskRequest Update(UserTaskResponse task, UserTaskStatus status, string? title = null) =>
        new(title ?? task.Title, task.Description, task.DueDate, status);

    [TestMethod]
    public async Task Post_ShouldCreateAPendingTaskOwnedByTheCaller()
    {
        var user = await ApiHelpers.NewEmployeeAsync();

        var response = await user.Client.PostJsonAsync(
            "/api/tasks",
            new { userId = Guid.NewGuid(), title = "Mi tarea", description = "d", dueDate = DateTime.UtcNow.AddDays(1), status = "Completed" });

        Assert.AreEqual(HttpStatusCode.Created, response.StatusCode);
        var task = await response.ReadAsync<UserTaskResponse>();
        Assert.AreEqual(user.UserId, task.UserId);
        Assert.AreEqual("Pending", task.Status);
        Assert.IsNotNull(response.Headers.Location);
    }

    [TestMethod]
    public async Task Post_ShouldRejectEmptyTitles()
    {
        var user = await ApiHelpers.NewEmployeeAsync();

        var response = await user.Client.PostJsonAsync("/api/tasks", new CreateUserTaskRequest("  ", null, DateTime.UtcNow.AddDays(1)));

        Assert.AreEqual(HttpStatusCode.BadRequest, response.StatusCode);
        Assert.AreEqual("INVALID_TASK", await response.CodeAsync());
    }

    [TestMethod]
    public async Task Get_ShouldListOnlyTheCallersTasks()
    {
        var mine = await ApiHelpers.NewEmployeeAsync();
        var theirs = await ApiHelpers.NewEmployeeAsync();
        var myTask = await CreateAsync(mine.Client, "mia");
        await CreateAsync(theirs.Client, "ajena");

        var tasks = await (await mine.Client.GetAsync("/api/tasks")).ReadAsync<List<UserTaskResponse>>();

        Assert.AreEqual(myTask.Id, tasks.Single().Id);
    }

    [TestMethod]
    public async Task Get_ShouldNotLetAdministratorsSeeOtherUsersTasks()
    {
        var admin = await ApiHelpers.AdminAsync();
        var employee = await ApiHelpers.SeededEmployeeAsync();

        var adminTasks = await (await admin.Client.GetAsync("/api/tasks")).ReadAsync<List<UserTaskResponse>>();
        var employeeTasks = await (await employee.Client.GetAsync("/api/tasks")).ReadAsync<List<UserTaskResponse>>();

        Assert.IsTrue(adminTasks.All(t => t.UserId == admin.UserId));
        Assert.IsGreaterThanOrEqualTo(3, employeeTasks.Count);
        Assert.IsTrue(employeeTasks.All(t => t.UserId == employee.UserId));
    }

    [TestMethod]
    public async Task GetPutDelete_ShouldReturnNotFoundForTasksOfOtherUsers()
    {
        var owner = await ApiHelpers.NewEmployeeAsync();
        var intruder = await ApiHelpers.NewEmployeeAsync();
        var task = await CreateAsync(owner.Client);

        var get = await intruder.Client.GetAsync($"/api/tasks/{task.Id}");
        var put = await intruder.Client.PutJsonAsync($"/api/tasks/{task.Id}", Update(task, UserTaskStatus.InProgress, "hack"));
        var delete = await intruder.Client.DeleteAsync($"/api/tasks/{task.Id}");

        Assert.AreEqual(HttpStatusCode.NotFound, get.StatusCode);
        Assert.AreEqual("TASK_NOT_FOUND", await get.CodeAsync());
        Assert.AreEqual(HttpStatusCode.NotFound, put.StatusCode);
        Assert.AreEqual(HttpStatusCode.NotFound, delete.StatusCode);

        var untouched = await (await owner.Client.GetAsync($"/api/tasks/{task.Id}")).ReadAsync<UserTaskResponse>();
        Assert.AreEqual(task.Title, untouched.Title);
        Assert.AreEqual("Pending", untouched.Status);
    }

    [TestMethod]
    public async Task Get_ShouldNotLetAdministratorsReadATaskOfAnEmployee()
    {
        var admin = await ApiHelpers.AdminAsync();
        var employee = await ApiHelpers.NewEmployeeAsync();
        var task = await CreateAsync(employee.Client);

        var response = await admin.Client.GetAsync($"/api/tasks/{task.Id}");

        Assert.AreEqual(HttpStatusCode.NotFound, response.StatusCode);
    }

    [TestMethod]
    public async Task Put_ShouldFollowPendingInProgressCompleted()
    {
        var user = await ApiHelpers.NewEmployeeAsync();
        var task = await CreateAsync(user.Client);

        var started = await user.Client.PutJsonAsync($"/api/tasks/{task.Id}", Update(task, UserTaskStatus.InProgress, "Editada"));
        var completed = await user.Client.PutJsonAsync($"/api/tasks/{task.Id}", Update(task, UserTaskStatus.Completed, "Editada"));

        Assert.AreEqual(HttpStatusCode.OK, started.StatusCode);
        Assert.AreEqual("InProgress", (await started.ReadAsync<UserTaskResponse>()).Status);
        Assert.AreEqual("Completed", (await completed.ReadAsync<UserTaskResponse>()).Status);
    }

    [TestMethod]
    public async Task Put_ShouldRejectSkippingInProgressAndKeepTheTaskUntouched()
    {
        var user = await ApiHelpers.NewEmployeeAsync();
        var task = await CreateAsync(user.Client);

        var response = await user.Client.PutJsonAsync($"/api/tasks/{task.Id}", Update(task, UserTaskStatus.Completed, "Cambiada"));

        Assert.AreEqual(HttpStatusCode.Conflict, response.StatusCode);
        Assert.AreEqual("INVALID_TASK_TRANSITION", await response.CodeAsync());
        var reloaded = await (await user.Client.GetAsync($"/api/tasks/{task.Id}")).ReadAsync<UserTaskResponse>();
        Assert.AreEqual(task.Title, reloaded.Title);
        Assert.AreEqual("Pending", reloaded.Status);
    }

    [TestMethod]
    public async Task Put_ShouldRejectGoingBackwards()
    {
        var user = await ApiHelpers.NewEmployeeAsync();
        var task = await CreateAsync(user.Client);
        await user.Client.PutJsonAsync($"/api/tasks/{task.Id}", Update(task, UserTaskStatus.InProgress));

        var response = await user.Client.PutJsonAsync($"/api/tasks/{task.Id}", Update(task, UserTaskStatus.Pending));

        Assert.AreEqual(HttpStatusCode.Conflict, response.StatusCode);
    }

    [TestMethod]
    public async Task Put_ShouldAllowEditingDetailsWithoutChangingTheStatus()
    {
        var user = await ApiHelpers.NewEmployeeAsync();
        var task = await CreateAsync(user.Client);

        var response = await user.Client.PutJsonAsync($"/api/tasks/{task.Id}", Update(task, UserTaskStatus.Pending, "Nuevo titulo"));

        Assert.AreEqual(HttpStatusCode.OK, response.StatusCode);
        var updated = await response.ReadAsync<UserTaskResponse>();
        Assert.AreEqual("Nuevo titulo", updated.Title);
        Assert.AreEqual("Pending", updated.Status);
        Assert.IsNotNull(updated.UpdatedAt);
    }

    [TestMethod]
    public async Task Delete_ShouldRemoveTheTask()
    {
        var user = await ApiHelpers.NewEmployeeAsync();
        var task = await CreateAsync(user.Client);

        var delete = await user.Client.DeleteAsync($"/api/tasks/{task.Id}");
        var get = await user.Client.GetAsync($"/api/tasks/{task.Id}");

        Assert.AreEqual(HttpStatusCode.NoContent, delete.StatusCode);
        Assert.AreEqual(HttpStatusCode.NotFound, get.StatusCode);
    }
}

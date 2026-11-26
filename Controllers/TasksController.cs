using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using TaskManager.Models;
using TaskManager.Services;

namespace TaskManager.Controllers;

[ApiController]
[Route("api/tasks")]
[Authorize]
public sealed class TasksController(TaskService taskService) : ControllerBase
{
    [HttpGet]
    [ProducesResponseType<PagedTasks>(StatusCodes.Status200OK)]
    public ActionResult<PagedTasks> GetTasks(
        [FromQuery] int page = 1,
        [FromQuery] int pageSize = 10,
        [FromQuery] string? search = null)
    {
        if (page < 1 || pageSize < 1 || pageSize > 100)
        {
            return BadRequest(new { message = "Page must be positive and pageSize must be between 1 and 100." });
        }

        return Ok(taskService.GetTasks(page, pageSize, search));
    }

    [HttpGet("{id:guid}")]
    [ProducesResponseType<WorkTask>(StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public ActionResult<WorkTask> GetTask(Guid id)
    {
        var task = taskService.Find(id);
        return task is null ? NotFound() : Ok(task);
    }

    [HttpPost]
    [ProducesResponseType<WorkTask>(StatusCodes.Status201Created)]
    public ActionResult<WorkTask> CreateTask(TaskRequest request)
    {
        var task = taskService.Create(request.Title, request.Description, request.Status);
        return CreatedAtAction(nameof(GetTask), new { id = task.Id }, task);
    }

    [HttpPut("{id:guid}")]
    [ProducesResponseType<WorkTask>(StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public ActionResult<WorkTask> UpdateTask(Guid id, TaskRequest request)
    {
        var task = taskService.Update(id, request.Title, request.Description, request.Status);
        return task is null ? NotFound() : Ok(task);
    }

    [HttpDelete("{id:guid}")]
    [Authorize(Roles = "Admin")]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    [ProducesResponseType(StatusCodes.Status403Forbidden)]
    public IActionResult DeleteTask(Guid id) =>
        taskService.Delete(id) ? NoContent() : NotFound();
}

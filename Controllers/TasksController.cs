using System.Security.Claims;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using TaskFlowAPI.Data;
using TaskFlowAPI.DTOs;
using TaskFlowAPI.Models;
using TaskStatus = TaskFlowAPI.Models.TaskStatus;

namespace TaskFlowAPI.Controllers;

[ApiController]
[Route("api/[controller]")]
[Authorize]                         // All endpoints require a valid JWT
public class TasksController : ControllerBase
{
    private readonly AppDbContext _db;

    public TasksController(AppDbContext db)
    {
        _db = db;
    }

    // Reads the user's ID from the JWT claims
    private Guid CurrentUserId =>
        Guid.Parse(User.FindFirstValue(ClaimTypes.NameIdentifier)
            ?? User.FindFirstValue("sub")
            ?? throw new UnauthorizedAccessException());

    private bool IsAdmin => User.IsInRole("Admin");

    // ── GET api/tasks ──────────────────────────────────────────────────────────
    // Admins see all tasks; regular users see only their own
    [HttpGet]
    public async Task<IActionResult> GetAll(
        [FromQuery] string? status,
        [FromQuery] string? priority)
    {
        var query = IsAdmin
            ? _db.Tasks.Include(t => t.User).AsQueryable()
            : _db.Tasks.Where(t => t.UserId == CurrentUserId).AsQueryable();

        if (!string.IsNullOrWhiteSpace(status) &&
            Enum.TryParse<TaskStatus>(status, true, out var s))
            query = query.Where(t => t.Status == s);

        if (!string.IsNullOrWhiteSpace(priority) &&
            Enum.TryParse<Priority>(priority, true, out var p))
            query = query.Where(t => t.Priority == p);

        var tasks = await query
            .OrderByDescending(t => t.CreatedAt)
            .Select(t => ToResponse(t))
            .ToListAsync();

        return Ok(new ApiResponse<List<TaskResponse>>(true, "Tasks retrieved.", tasks));
    }

    // ── GET api/tasks/{id} ─────────────────────────────────────────────────────
    [HttpGet("{id:guid}")]
    public async Task<IActionResult> GetById(Guid id)
    {
        var task = await _db.Tasks.FindAsync(id);

        if (task is null) return NotFound(new ApiResponse<object>(false, "Task not found.", null));
        if (!IsAdmin && task.UserId != CurrentUserId)
            return Forbid();

        return Ok(new ApiResponse<TaskResponse>(true, "Task retrieved.", ToResponse(task)));
    }

    // ── POST api/tasks ─────────────────────────────────────────────────────────
    [HttpPost]
    public async Task<IActionResult> Create([FromBody] CreateTaskRequest req)
    {
        if (!Enum.TryParse<Priority>(req.Priority, true, out var priority))
            return BadRequest(new ApiResponse<object>(false, "Invalid priority. Use Low, Medium or High.", null));

        var task = new TaskItem
        {
            Title       = req.Title,
            Description = req.Description,
            Priority    = priority,
            DueDate     = req.DueDate,
            UserId      = CurrentUserId
        };

        _db.Tasks.Add(task);
        await _db.SaveChangesAsync();

        return CreatedAtAction(
            nameof(GetById),
            new { id = task.Id },
            new ApiResponse<TaskResponse>(true, "Task created.", ToResponse(task))
        );
    }

    // ── PUT api/tasks/{id} ─────────────────────────────────────────────────────
    [HttpPut("{id:guid}")]
    public async Task<IActionResult> Update(Guid id, [FromBody] UpdateTaskRequest req)
    {
        var task = await _db.Tasks.FindAsync(id);

        if (task is null) return NotFound(new ApiResponse<object>(false, "Task not found.", null));
        if (!IsAdmin && task.UserId != CurrentUserId)
            return Forbid();

        if (!string.IsNullOrWhiteSpace(req.Title))
            task.Title = req.Title;

        if (req.Description is not null)
            task.Description = req.Description;

        if (!string.IsNullOrWhiteSpace(req.Status) &&
            Enum.TryParse<TaskStatus>(req.Status, true, out var status))
            task.Status = status;

        if (!string.IsNullOrWhiteSpace(req.Priority) &&
            Enum.TryParse<Priority>(req.Priority, true, out var priority))
            task.Priority = priority;

        if (req.DueDate.HasValue)
            task.DueDate = req.DueDate;

        task.UpdatedAt = DateTime.UtcNow;

        await _db.SaveChangesAsync();

        return Ok(new ApiResponse<TaskResponse>(true, "Task updated.", ToResponse(task)));
    }

    // ── DELETE api/tasks/{id} ──────────────────────────────────────────────────
    [HttpDelete("{id:guid}")]
    public async Task<IActionResult> Delete(Guid id)
    {
        var task = await _db.Tasks.FindAsync(id);

        if (task is null) return NotFound(new ApiResponse<object>(false, "Task not found.", null));
        if (!IsAdmin && task.UserId != CurrentUserId)
            return Forbid();

        _db.Tasks.Remove(task);
        await _db.SaveChangesAsync();

        return Ok(new ApiResponse<object>(true, "Task deleted.", null));
    }

    // ── Admin: GET api/tasks/all-users ─────────────────────────────────────────
    [HttpGet("all-users")]
    [Authorize(Roles = "Admin")]
    public async Task<IActionResult> GetAllWithUsers()
    {
        var tasks = await _db.Tasks
            .Include(t => t.User)
            .OrderByDescending(t => t.CreatedAt)
            .Select(t => ToResponse(t))
            .ToListAsync();

        return Ok(new ApiResponse<List<TaskResponse>>(true, "All tasks retrieved.", tasks));
    }

    // ── Helper: map entity → DTO ───────────────────────────────────────────────
    private static TaskResponse ToResponse(TaskItem t) => new(
        t.Id,
        t.Title,
        t.Description,
        t.Status.ToString(),
        t.Priority.ToString(),
        t.DueDate,
        t.CreatedAt,
        t.UpdatedAt,
        t.UserId
    );
}

namespace TaskFlowAPI.DTOs;

// ── Auth ──────────────────────────────────────────────────────────────────────

public record RegisterRequest(
    string FullName,
    string Email,
    string Password
);

public record LoginRequest(
    string Email,
    string Password
);

public record AuthResponse(
    string Token,
    string FullName,
    string Email,
    string Role,
    DateTime ExpiresAt
);

// ── Tasks ─────────────────────────────────────────────────────────────────────

public record CreateTaskRequest(
    string Title,
    string? Description,
    string Priority,   // Low | Medium | High
    DateTime? DueDate
);

public record UpdateTaskRequest(
    string? Title,
    string? Description,
    string? Status,    // Todo | InProgress | Done
    string? Priority,
    DateTime? DueDate
);

public record TaskResponse(
    Guid Id,
    string Title,
    string? Description,
    string Status,
    string Priority,
    DateTime? DueDate,
    DateTime CreatedAt,
    DateTime UpdatedAt,
    Guid UserId
);

// ── Common ────────────────────────────────────────────────────────────────────

public record ApiResponse<T>(
    bool Success,
    string Message,
    T? Data
);

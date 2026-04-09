using ChatSpark.Domain.Entities;
using ChatSpark.Domain.Enum;
using ChatSpark.Infrastructure.Persistence;
using ChatSpark.Shared.Dtos.Workspaces;
using Dapper;
using Microsoft.EntityFrameworkCore;
using System.Security.Claims;

namespace ChatSpark.Api.Endpoints
{
    public static class WorkspaceEndpoints
    {
        public static void MapWorkspaceEndpoints(this IEndpointRouteBuilder routes)
        {
            var group = routes.MapGroup("/api/workspaces").RequireAuthorization().WithTags("Workspaces");

            group.MapPost("/", async (CreateWorkspaceRequest request, AppDbContext db, ClaimsPrincipal principal) =>
            {
                var userIdString = principal.FindFirst(ClaimTypes.NameIdentifier)?.Value;
                if (!Guid.TryParse(userIdString, out var userId))
                {
                    return Results.Unauthorized();
                }

                var slugTaken = await db.Workspaces.AnyAsync(w => w.Slug == request.Slug);
                if (slugTaken)
                {
                    return Results.Conflict(new { message = "Slug is already taken." });
                }
                var workspace = Workspace.Create(userId, request.Name, request.Slug);

                db.Workspaces.Add(workspace);

                var membership = WorkspaceMember.Create(workspace.Id, userId, Role.Owner);
                db.WorkspaceMembers.Add(membership);

                await db.SaveChangesAsync();

                return Results.Created($"/api/workspaces/{workspace.Id}", new WorkspaceResponse(
                    workspace.Id,
                    workspace.Name,
                    workspace.Slug,
                    workspace.OwnerId,
                    workspace.CreatedAt));

            });

            group.MapGet("/", async (ClaimsPrincipal principal, IDbConnectionFactory connection) =>
            {
                var userIdString = principal.FindFirst(ClaimTypes.NameIdentifier)?.Value;
                if (!Guid.TryParse(userIdString, out var userId))
                {
                    return Results.Unauthorized();
                }


                const string sql = @"
                        SELECT w.id, w.name, w.slug, w.owner_id, w.created_at
                        FROM workspaces w
                        INNER JOIN workspace_members m ON m.workspace_id = w.id
                        WHERE m.user_id = @UserId
                        ORDER BY w.created_at DESC;
                        ";

                using var conn = await connection.CreateAsync();

                var workspaces = await conn.QueryAsync<WorkspaceResponse>(sql, new { UserId = userId });

                return Results.Ok(workspaces);
            });

            group.MapPost("/join", async (JoinWorkspaceRequest request, ClaimsPrincipal principal, AppDbContext db) =>
            {
                var userIdString = principal.FindFirst(ClaimTypes.NameIdentifier)?.Value;
                if(!Guid.TryParse(userIdString,out var userId))
                {
                    return Results.Unauthorized();
                }

                var workspace = await db.Workspaces
                        .FirstOrDefaultAsync(w => w.Slug == request.Slug);

                if(workspace is null)
                {
                    return Results.NotFound(new { message = "Workspace is not found." });
                }


                var isMember = await db.WorkspaceMembers.AnyAsync(m => m.WorkspaceId == workspace.Id && m.UserId == userId);

                if (isMember)
                {
                    return Results.Conflict(new { message = "You are already a member of this workspace." });
                }

                var membership = WorkspaceMember.Create(workspace.Id, userId, Role.Member);
                db.WorkspaceMembers.Add(membership);

                await db.SaveChangesAsync();

                return Results.Ok(new WorkspaceResponse(
                    workspace.Id,
                    workspace.Name,
                    workspace.Slug,
                    workspace.OwnerId,
                    workspace.CreatedAt));

            });

            group.MapPost("/{id:guid}/leave", async (Guid workspaceId, ClaimsPrincipal principal, AppDbContext db) =>
            {
                var userIdString = principal.FindFirst(ClaimTypes.NameIdentifier)?.Value;
                if (!Guid.TryParse(userIdString, out var userId))
                {
                    return Results.Unauthorized();
                }



                var membership = await db.WorkspaceMembers
                        .FirstOrDefaultAsync(m => m.WorkspaceId == workspaceId && m.UserId == userId);

                if(membership is null)
                {
                    return Results.NotFound("Membership is not found.");
                }

                if(membership.Role == Role.Owner)
                {
                    return Results.BadRequest(new
                    {
                        message = "Owners cannot leave; transfer ownership or delete the workspace"
                    });
                }

                db.WorkspaceMembers.Remove(membership);

                await db.SaveChangesAsync();

                return Results.NoContent();
            });

        }
    }
}

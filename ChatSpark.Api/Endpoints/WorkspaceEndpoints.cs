using ChatSpark.Domain.Entities;
using ChatSpark.Domain.Enum;
using ChatSpark.Infrastructure.Persistence;
using ChatSpark.Shared.Dtos.Workspaces;
using Dapper;
using Microsoft.EntityFrameworkCore;
using StackExchange.Redis;
using System.IdentityModel.Tokens.Jwt;
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
                var userIdString = principal.FindFirst(JwtRegisteredClaimNames.Sub)?.Value;


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

                var membership = WorkspaceMember.Create(workspace.Id, userId, Domain.Enum.Role.Owner);
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
                var userIdString = principal.FindFirst(JwtRegisteredClaimNames.Sub)?.Value;
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
                var userIdString = principal.FindFirst(JwtRegisteredClaimNames.Sub)?.Value;
                if (!Guid.TryParse(userIdString,out var userId))
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

                var membership = WorkspaceMember.Create(workspace.Id, userId, Domain.Enum.Role.Member);
                db.WorkspaceMembers.Add(membership);

                await db.SaveChangesAsync();

                return Results.Ok(new WorkspaceResponse(
                    workspace.Id,
                    workspace.Name,
                    workspace.Slug,
                    workspace.OwnerId,
                    workspace.CreatedAt));

            });

            group.MapPost("/{workspaceId:guid}/leave", async (Guid workspaceId, ClaimsPrincipal principal, AppDbContext db) =>
            {
                var userIdString = principal.FindFirst(JwtRegisteredClaimNames.Sub)?.Value;
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

                if(membership.Role == Domain.Enum.Role.Owner)
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


            // GET all workspace members with online status
            group.MapGet("/{workspaceId:guid}/members", async (
                Guid workspaceId,
                ClaimsPrincipal principal,
                AppDbContext db,
                IDbConnectionFactory connectionFactory,
                IConnectionMultiplexer redis) =>
            {
                var userId = Guid.Parse(principal.FindFirst(JwtRegisteredClaimNames.Sub)!.Value);

                var isMember = await db.WorkspaceMembers
                    .AnyAsync(m => m.WorkspaceId == workspaceId && m.UserId == userId);

                if (!isMember) return Results.Forbid();

                // Fetch all members with user details via Dapper
                const string sql = @"
                    SELECT
                        m.user_id,
                        u.display_name,
                        u.avatar_url,
                        CASE m.role
                            WHEN 1 THEN 'Owner'
                            WHEN 2 THEN 'Admin'
                            ELSE 'Member'
                        END AS role
                    FROM workspace_members m
                    INNER JOIN users u ON u.id = m.user_id
                    WHERE m.workspace_id = @WorkspaceId
                    ORDER BY m.role, u.display_name;";

                using var conn = await connectionFactory.CreateAsync();
                var rows = await conn.QueryAsync<(Guid UserId, string DisplayName, string? AvatarUrl, string Role)>(
                    sql, new { WorkspaceId = workspaceId });

                // Build the set of online user IDs by unioning all channel presence sets
                var channelIds = await db.Channels
                    .Where(c => c.WorkspaceId == workspaceId && !c.IsArchived)
                    .Select(c => c.Id)
                    .ToListAsync();

                var onlineUserIds = new HashSet<Guid>();
                var redisDb = redis.GetDatabase();
                foreach (var channelId in channelIds)
                {
                    var presenceMembers = await redisDb.SetMembersAsync($"presence:{channelId}");
                    foreach (var m in presenceMembers)
                        onlineUserIds.Add(Guid.Parse(m.ToString()));
                }

                var result = rows.Select(r => new WorkspaceMemberResponse(
                    r.UserId,
                    r.DisplayName,
                    r.AvatarUrl,
                    r.Role,
                    onlineUserIds.Contains(r.UserId)));

                return Results.Ok(result);
            });

            group.MapDelete("/{workspaceId:guid}", async (Guid workspaceId, ClaimsPrincipal principal, AppDbContext db) =>
            {
                var userId = Guid.Parse(principal.FindFirst(JwtRegisteredClaimNames.Sub)?.Value);

                var workspace = await db.Workspaces.FindAsync(workspaceId);
                if(workspace is null)
                {
                    return Results.NotFound();
                }

                if(workspace.OwnerId != userId)
                {

                    return Results.Forbid();
                }

                db.Workspaces.Remove(workspace);
                await db.SaveChangesAsync();

                return Results.NoContent();



            });

        }
    }
}

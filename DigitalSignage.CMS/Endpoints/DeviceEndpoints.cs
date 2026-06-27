using Microsoft.EntityFrameworkCore;
using DigitalSignage.CMS.Data;
using DigitalSignage.Shared.Dtos;
using DigitalSignage.Shared.Models;

namespace DigitalSignage.CMS.Endpoints;

public static class DeviceEndpoints
{
    public static void MapDeviceEndpoints(this WebApplication app)
    {
        var group = app.MapGroup("/api/devices");

        group.MapPost("/register", async (DeviceRegisterRequest request, AppDbContext db) =>
        {
            var device = await db.Devices.FirstOrDefaultAsync(d => d.UniqueId == request.UniqueId);
            if (device is null)
            {
                device = new Device { UniqueId = request.UniqueId };
                db.Devices.Add(device);
            }

            device.Name = request.Name;
            device.DeviceType = request.DeviceType;
            device.IpAddress = request.IpAddress;
            device.Status = "Online";
            device.LastHeartbeat = DateTime.UtcNow;

            await db.SaveChangesAsync();

            return Results.Ok(device);
        });

        group.MapPost("/{id:int}/heartbeat", async (int id, AppDbContext db) =>
        {
            var device = await db.Devices.FindAsync(id);
            if (device is null)
            {
                return Results.NotFound();
            }

            device.Status = "Online";
            device.LastHeartbeat = DateTime.UtcNow;
            await db.SaveChangesAsync();

            return Results.Ok(device);
        });

        group.MapGet("/{id:int}/playlist", async (int id, AppDbContext db) =>
        {
            var device = await db.Devices.FindAsync(id);
            if (device is null)
            {
                return Results.NotFound();
            }

            if (device.PlaylistId is null)
            {
                return Results.NoContent();
            }

            var playlist = await db.Playlists.FindAsync(device.PlaylistId.Value);
            if (playlist is null)
            {
                return Results.NoContent();
            }

            var items = await db.ContentItems
                .Where(c => playlist.ContentIds.Contains(c.Id))
                .ToListAsync();

            return Results.Ok(new PlaylistContentDto
            {
                PlaylistId = playlist.Id,
                PlaylistName = playlist.Name,
                Items = items
            });
        });
    }
}

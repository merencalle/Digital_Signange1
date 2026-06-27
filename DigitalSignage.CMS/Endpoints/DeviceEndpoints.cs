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

        group.MapPost("/register", async (DeviceRegisterRequest request, AppDbContext db, ILogger<Device> logger) =>
        {
            // Already-paired device (known UniqueId) re-registering, e.g. after a restart - no secret required again.
            var existing = await db.Devices.FirstOrDefaultAsync(d => d.UniqueId == request.UniqueId && d.IsPaired);
            if (existing is not null)
            {
                existing.Name = request.Name;
                existing.DeviceType = request.DeviceType;
                existing.IpAddress = request.IpAddress;
                existing.Status = "Online";
                existing.LastHeartbeat = DateTime.UtcNow;
                await db.SaveChangesAsync();

                logger.LogInformation(
                    "Device re-registered: '{Name}' ({DeviceType}) from {IpAddress}, UniqueId={UniqueId}",
                    existing.Name, existing.DeviceType, existing.IpAddress, existing.UniqueId);

                return Results.Ok(existing);
            }

            // Unrecognized device - it must present a valid, unconsumed pairing secret from a
            // wizard-generated enrollment package to claim a pending Device row.
            if (string.IsNullOrWhiteSpace(request.PairingSecret))
            {
                logger.LogWarning(
                    "Registration rejected (no pairing secret presented) for UniqueId={UniqueId} from {IpAddress}",
                    request.UniqueId, request.IpAddress);
                return Results.Unauthorized();
            }

            var pending = await db.Devices.FirstOrDefaultAsync(d => d.PairingSecret == request.PairingSecret && !d.IsPaired);
            if (pending is null)
            {
                logger.LogWarning(
                    "Registration rejected (invalid or already-used pairing secret) for UniqueId={UniqueId} from {IpAddress}",
                    request.UniqueId, request.IpAddress);
                return Results.Unauthorized();
            }

            pending.UniqueId = request.UniqueId;
            pending.Name = request.Name;
            pending.DeviceType = request.DeviceType;
            pending.IpAddress = request.IpAddress;
            pending.Status = "Online";
            pending.LastHeartbeat = DateTime.UtcNow;
            pending.IsPaired = true;
            pending.PairingSecret = null; // consumed; regenerate from the wizard to re-pair this slot

            await db.SaveChangesAsync();

            logger.LogInformation(
                "Device paired and registered: '{Name}' ({DeviceType}) from {IpAddress}, UniqueId={UniqueId}",
                pending.Name, pending.DeviceType, pending.IpAddress, pending.UniqueId);

            return Results.Ok(pending);
        });

        group.MapPost("/{id:int}/heartbeat", async (int id, AppDbContext db, ILogger<Device> logger) =>
        {
            var device = await db.Devices.FindAsync(id);
            if (device is null)
            {
                logger.LogWarning("Heartbeat received for unknown device id {DeviceId}", id);
                return Results.NotFound();
            }

            device.Status = "Online";
            device.LastHeartbeat = DateTime.UtcNow;
            await db.SaveChangesAsync();

            logger.LogDebug("Heartbeat from device '{Name}' (id {DeviceId})", device.Name, device.Id);

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

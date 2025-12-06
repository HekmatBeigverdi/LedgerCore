using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using LedgerCore.Core.Interfaces;
using LedgerCore.Core.Models.Common;
using LedgerCore.Core.Models.Enums;
using LedgerCore.Core.Models.Security;
using LedgerCore.Core.Services;
using LedgerCore.Core.ViewModels.Security;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace LedgerCore.Controllers;

[ApiController]
[Route("api/[controller]")]
[Authorize(Roles = "Admin")]
public class UsersController(IUnitOfWork uow) : ControllerBase
{
    // helper to unwrap possible paged/wrapper results
    private static IEnumerable<T> Unwrap<T>(object raw)
    {
        if (raw is IEnumerable<T> direct) return direct;
        if (raw is null) return Enumerable.Empty<T>();

        var type = raw.GetType();
        var prop = type.GetProperty("Items") ?? type.GetProperty("Data") ?? type.GetProperty("Results") ?? type.GetProperty("List");
        if (prop == null) throw new InvalidOperationException($"Returned type {type.FullName} does not expose enumerable payload.");
        var value = prop.GetValue(raw);
        return value as IEnumerable<T> ?? throw new InvalidOperationException($"Property {prop.Name} on {type.FullName} is not IEnumerable<{typeof(T).Name}>.");
    }

    // GET api/users
    [HttpGet]
    public async Task<ActionResult<List<UserDto>>> GetAll(CancellationToken cancellationToken)
    {
        var userRepo = uow.Repository<User>();
        var usersRaw = await userRepo.GetAllAsync(cancellationToken: cancellationToken);
        var users = Unwrap<User>(usersRaw).ToList();

        var urRepo = uow.Repository<UserRole>();
        var userRolesRaw = await urRepo.GetAllAsync(cancellationToken: cancellationToken);
        var userRoles = Unwrap<UserRole>(userRolesRaw).ToList();

        var result = users.Select(u =>
        {
            var roleIds = (u.UserRoles?.Select(ur => ur.RoleId).ToList())
                          ?? userRoles.Where(ur => ur.UserId == u.Id).Select(ur => ur.RoleId).ToList();

            return new UserDto
            {
                Id = u.Id,
                UserName = u.UserName,
                DisplayName = u.DisplayName,
                Email = u.Email,
                IsActive = u.Status == UserStatus.Active,
                RoleIds = roleIds
            };
        }).ToList();

        return Ok(result);
    }

    // GET api/users/5
    [HttpGet("{id:int}")]
    public async Task<ActionResult<UserDto>> Get(int id, CancellationToken cancellationToken)
    {
        var user = await uow.Repository<User>().GetByIdAsync(id, cancellationToken);
        if (user is null)
            return NotFound();

        var urRepo = uow.Repository<UserRole>();
        var existingRolesRaw = await urRepo.FindAsync(ur => ur.UserId == id, cancellationToken: cancellationToken);
        var existingRoles = Unwrap<UserRole>(existingRolesRaw).ToList();

        var dto = new UserDto
        {
            Id = user.Id,
            UserName = user.UserName,
            DisplayName = user.DisplayName,
            Email = user.Email,
            IsActive = user.Status == UserStatus.Active,
            RoleIds = (user.UserRoles?.Select(ur => ur.RoleId).ToList()) ?? existingRoles.Select(ur => ur.RoleId).ToList()
        };

        return Ok(dto);
    }

    // POST api/users
    [HttpPost]
    public async Task<ActionResult<UserDto>> Create(
        [FromBody] UserCreateUpdateRequest request,
        CancellationToken cancellationToken)
    {
        if (string.IsNullOrWhiteSpace(request.UserName))
            return BadRequest("UserName is required.");

        if (string.IsNullOrWhiteSpace(request.Password))
            return BadRequest("Password is required.");

        AuthService.CreatePasswordHash(request.Password!, out var hash, out var salt);

        var user = new User
        {
            UserName = request.UserName,
            DisplayName = request.DisplayName,
            Email = request.Email,
            PasswordHash = hash,
            PasswordSalt = salt,
            Status = request.IsActive ? UserStatus.Active : UserStatus.Inactive
        };

        await uow.Repository<User>().AddAsync(user, cancellationToken);
        await uow.SaveChangesAsync(cancellationToken);

        var urRepo = uow.Repository<UserRole>();
        foreach (var roleId in request.RoleIds.Distinct())
        {
            await urRepo.AddAsync(new UserRole
            {
                UserId = user.Id,
                RoleId = roleId
            }, cancellationToken);
        }

        await uow.SaveChangesAsync(cancellationToken);

        var dto = new UserDto
        {
            Id = user.Id,
            UserName = user.UserName,
            DisplayName = user.DisplayName,
            Email = user.Email,
            IsActive = request.IsActive,
            RoleIds = request.RoleIds.Distinct().ToList()
        };

        return CreatedAtAction(nameof(Get), new { id = dto.Id }, dto);
    }

    // PUT api/users/5
    [HttpPut("{id:int}")]
    public async Task<IActionResult> Update(
        int id,
        [FromBody] UserCreateUpdateRequest request,
        CancellationToken cancellationToken)
    {
        var userRepo = uow.Repository<User>();
        var user = await userRepo.GetByIdAsync(id, cancellationToken);
        if (user is null)
            return NotFound();

        user.DisplayName = request.DisplayName;
        user.Email = request.Email;
        user.Status = request.IsActive ? UserStatus.Active : UserStatus.Inactive;

        if (!string.IsNullOrWhiteSpace(request.Password))
        {
            AuthService.CreatePasswordHash(request.Password!, out var hash, out var salt);
            user.PasswordHash = hash;
            user.PasswordSalt = salt;
        }

        userRepo.Update(user);
        await uow.SaveChangesAsync(cancellationToken);

        var urRepo = uow.Repository<UserRole>();
        var existingRaw = await urRepo.FindAsync(ur => ur.UserId == id, cancellationToken: cancellationToken);
        var existing = Unwrap<UserRole>(existingRaw);

        foreach (var ur in existing)
            urRepo.Remove(ur);

        foreach (var roleId in request.RoleIds.Distinct())
        {
            await urRepo.AddAsync(new UserRole
            {
                UserId = id,
                RoleId = roleId
            }, cancellationToken);
        }

        await uow.SaveChangesAsync(cancellationToken);

        return NoContent();
    }

    // DELETE api/users/5
    [HttpDelete("{id:int}")]
    public async Task<IActionResult> Delete(int id, CancellationToken cancellationToken)
    {
        var repo = uow.Repository<User>();
        var user = await repo.GetByIdAsync(id, cancellationToken);
        if (user is null)
            return NotFound();

        repo.Remove(user);
        await uow.SaveChangesAsync(cancellationToken);

        return NoContent();
    }
}

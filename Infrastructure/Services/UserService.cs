using System.Net;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using SomoniBank.Domain.DTOs;
using SomoniBank.Domain.Filtres;
using SomoniBank.Domain.Models;
using SomoniBank.Infrastructure.Data;
using SomoniBank.Infrastructure.Interfaces;
using SomoniBank.Infrastructure.Responses;

namespace SomoniBank.Infrastructure.Services;

public class UserService : IUserService
{
    private readonly AppDbContext _db;
    private readonly ILogger<UserService> _logger;

    public UserService(AppDbContext db, ILogger<UserService> logger)
    {
        _db = db;
        _logger = logger;
    }

    public async Task<Response<UserGetDto>> GetByIdAsync(Guid id)
    {
        try
        {
            var user = await _db.Users.AsNoTracking()
                .FirstOrDefaultAsync(x => x.Id == id);
            if (user == null)
                return new Response<UserGetDto>(HttpStatusCode.NotFound, "Пользователь не найден");

            return new Response<UserGetDto>(HttpStatusCode.OK, "Успешно", MapToDto(user));
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "GetUserById failed");
            return new Response<UserGetDto>(HttpStatusCode.InternalServerError, "Что-то пошло не так");
        }
    }

    public async Task<PagedResult<UserGetDto>> GetAllAsync(UserFilter filter, PagedQuery pagedQuery)
    {
        var page = pagedQuery.Page <= 0 ? 1 : pagedQuery.Page;
        var pageSize = pagedQuery.PageSize <= 0 ? 10 : pagedQuery.PageSize;

        IQueryable<User> query = _db.Users.AsNoTracking();

        if (!string.IsNullOrEmpty(filter?.FirstName))
            query = query.Where(x => x.FirstName.Contains(filter.FirstName));
        if (!string.IsNullOrEmpty(filter?.LastName))
            query = query.Where(x => x.LastName.Contains(filter.LastName));
        if (!string.IsNullOrEmpty(filter?.Email))
            query = query.Where(x => x.Email.Contains(filter.Email));
        if (!string.IsNullOrEmpty(filter?.Phone))
            query = query.Where(x => x.Phone.Contains(filter.Phone));
        if (filter?.IsActive != null)
            query = query.Where(x => x.IsActive == filter.IsActive);

        var totalCount = await query.CountAsync();
        var items = await query.OrderByDescending(x => x.CreatedAt)
            .Skip((page - 1) * pageSize).Take(pageSize).ToListAsync();

        return new PagedResult<UserGetDto>
        {
            Items = items.Select(MapToDto).ToList(),
            Page = page,
            PageSize = pageSize,
            TotalCount = totalCount,
            TotalPages = (int)Math.Ceiling(totalCount / (double)pageSize)
        };
    }

    public async Task<Response<string>> UpdateAsync(Guid id, UserUpdateDto dto)
    {
        try
        {
            var user = await _db.Users.FindAsync(id);
            if (user == null)
                return new Response<string>(HttpStatusCode.NotFound, "Пользователь не найден");

            if (!string.IsNullOrEmpty(dto.FirstName)) user.FirstName = dto.FirstName;
            if (!string.IsNullOrEmpty(dto.LastName)) user.LastName = dto.LastName;
            if (!string.IsNullOrEmpty(dto.Phone)) user.Phone = dto.Phone;
            if (!string.IsNullOrEmpty(dto.Address)) user.Address = dto.Address;

            await _db.SaveChangesAsync();
            return new Response<string>(HttpStatusCode.OK, "Профиль обновлён");
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "UpdateUser failed");
            return new Response<string>(HttpStatusCode.InternalServerError, "Что-то пошло не так");
        }
    }

    public async Task<Response<string>> DeleteAsync(Guid id)
    {
        try
        {
            var user = await _db.Users.FindAsync(id);
            if (user == null)
                return new Response<string>(HttpStatusCode.NotFound, "Пользователь не найден");

            _db.Users.Remove(user);
            await _db.SaveChangesAsync();
            return new Response<string>(HttpStatusCode.OK, "Пользователь удалён");
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "DeleteUser failed");
            return new Response<string>(HttpStatusCode.InternalServerError, "Что-то пошло не так");
        }
    }

    public async Task<Response<string>> BlockAsync(Guid id)
    {
        try
        {
            var user = await _db.Users.FindAsync(id);
            if (user == null)
                return new Response<string>(HttpStatusCode.NotFound, "Пользователь не найден");

            user.IsActive = false;
            await _db.SaveChangesAsync();
            return new Response<string>(HttpStatusCode.OK, "Пользователь заблокирован");
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "BlockUser failed");
            return new Response<string>(HttpStatusCode.InternalServerError, "Что-то пошло не так");
        }
    }

    public async Task<Response<string>> UnblockAsync(Guid id)
    {
        try
        {
            var user = await _db.Users.FindAsync(id);
            if (user == null)
                return new Response<string>(HttpStatusCode.NotFound, "Пользователь не найден");

            user.IsActive = true;
            await _db.SaveChangesAsync();
            return new Response<string>(HttpStatusCode.OK, "Пользователь разблокирован");
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "UnblockUser failed");
            return new Response<string>(HttpStatusCode.InternalServerError, "Что-то пошло не так");
        }
    }

    private static UserGetDto MapToDto(User u) => new()
    {
        Id = u.Id,
        FirstName = u.FirstName,
        LastName = u.LastName,
        Email = u.Email,
        Phone = u.Phone,
        Address = u.Address,
        PassportNumber = u.PassportNumber,
        Role = u.Role.ToString(),
        IsActive = u.IsActive,
        CreatedAt = u.CreatedAt
    };
}
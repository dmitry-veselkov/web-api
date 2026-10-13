using System.Net;
using AutoMapper;
using Microsoft.AspNetCore.JsonPatch;
using Microsoft.AspNetCore.Mvc;
using WebApi.MinimalApi.Domain;
using WebApi.MinimalApi.Models;

namespace WebApi.MinimalApi.Controllers;

[ApiController]
[Route("api/[controller]")]
[Produces("application/json", "application/xml")]
public class UsersController : Controller
{
    private readonly IUserRepository userRepository;
    private readonly IMapper mapper;

    public UsersController(IUserRepository userRepository, IMapper mapper)
    {
        this.userRepository = userRepository;
        this.mapper = mapper;
    }

    [HttpHead("{userId}")]
    [HttpGet("{userId}", Name = nameof(GetUserById))]
    public ActionResult<UserResponse> GetUserById([FromRoute] Guid userId)
    {
        var user = userRepository.FindById(userId);
        if (user is null)
        {
            return NotFound();
        }

        var userDto = mapper.Map<UserResponse>(user);

        if (HttpMethods.IsHead(Request.Method))
        {
            Response.Headers.Append("Content-Type", "application/json; charset=utf-8");
            return Ok();
        }

        return userDto;
    }

    [HttpPost]
    public IActionResult CreateUser([FromBody] UserCreationRequest? request)
    {
        if (request is null)
        {
            return BadRequest();
        }

        if (!ModelState.IsValid)
        {
            return UnprocessableEntity(ModelState);
        }

        var user = mapper.Map<UserEntity>(request);

        var createdUser = userRepository.Insert(user);

        return CreatedAtRoute(
            nameof(GetUserById),
            new { userId = createdUser.Id },
            createdUser.Id);
    }

    [HttpPut("{userId}")]
    public IActionResult UpsertUser([FromRoute] Guid userId, [FromBody] UserUpdateRequest? request)
    {
        if (request is null || userId == Guid.Empty)
        {
            return BadRequest();
        }

        if (!ModelState.IsValid)
        {
            return UnprocessableEntity(ModelState);
        }

        var user = mapper.Map(request, new UserEntity(userId));
        userRepository.UpdateOrInsert(user, out var isInserted);

        if (isInserted)
        {
            return CreatedAtRoute(
                nameof(GetUserById),
                new { userId = user.Id },
                user.Id);
        }

        return NoContent();
    }

    [HttpPatch("{userId}")]
    public IActionResult PartiallyUpdateUser([FromRoute] Guid userId, [FromBody] JsonPatchDocument<UserUpdateRequest>? patchDoc)
    {
        if (patchDoc is null)
        {
            return BadRequest();
        }

        var user = userRepository.FindById(userId);
        if (user is null)
        {
            return NotFound();
        }

        var userUpdate = mapper.Map<UserUpdateRequest>(user);
        patchDoc.ApplyTo(userUpdate, ModelState);

        if (!TryValidateModel(userUpdate))
        {
            return UnprocessableEntity(ModelState);
        }

        var updatedUser = mapper.Map<UserEntity>(userUpdate);
        userRepository.Update(updatedUser);

        return NoContent();
    }

    [HttpDelete("{userId}")]
    public IActionResult DeleteUser([FromRoute] Guid userId)
    {
        var user = userRepository.FindById(userId);
        if (user is null)
        {
            return NotFound();
        }

        userRepository.Delete(userId);

        return NoContent();
    }
}

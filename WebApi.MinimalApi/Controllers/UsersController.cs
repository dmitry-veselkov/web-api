using AutoMapper;
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

    [HttpGet("{userId}", Name = nameof(GetUserById))]
    public ActionResult<UserResponse> GetUserById([FromRoute] Guid userId)
    {
        var user = userRepository.FindById(userId);
        if (user is null)
        {
            return NotFound();
        }

        var userDto = mapper.Map<UserResponse>(user);

        return userDto;
    }

    [HttpPost]
    public IActionResult CreateUser([FromBody] UserCreationRequest? request)
    {
        if (request is null)
        {
            return BadRequest();
        }

        if (!IsValidLogin(request.Login))
        {
            ModelState.AddModelError("Login", "Invalid login");
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

    private static bool IsValidLogin(string? login) => login?.All(char.IsLetterOrDigit) ?? false;
}
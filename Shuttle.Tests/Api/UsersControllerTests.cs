using System.Security.Claims;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Logging.Abstractions;
using Microsoft.Identity.Web;
using Shuttle.Api.Controllers;
using Shuttle.Api.Services.Users;
using Shuttle.EFCore.Entities;
using Shuttle.Models.Users;

namespace Shuttle.Tests.Api;

public class UsersControllerTests {
    private static readonly Guid ObjectId = Guid.Parse("11111111-1111-1111-1111-111111111111");

    [Fact]
    public async Task GetCurrentUser_returns_401_when_the_principal_has_no_object_id() {
        var controller = CreateController(new StubUserService(), Principal(oid: null));

        var result = await controller.GetCurrentUser(CancellationToken.None);

        Assert.IsType<UnauthorizedResult>(result.Result);
    }

    [Fact]
    public async Task GetCurrentUser_returns_the_get_or_created_account() {
        var user = NewUser("alice");
        var service = new StubUserService { GetOrCreate = _ => user };
        var controller = CreateController(service, Principal(ObjectId.ToString()));

        var result = await controller.GetCurrentUser(CancellationToken.None);

        var ok = Assert.IsType<OkObjectResult>(result.Result);
        var card = Assert.IsType<CurrentUser>(ok.Value);
        Assert.Equal(user.Id, card.Id);
        Assert.Equal("alice", card.Username);
        Assert.Equal(ObjectId, service.LastGetOrCreateObjectId);
    }

    [Fact]
    public async Task UpdateCurrentUser_returns_401_when_the_principal_has_no_object_id() {
        var controller = CreateController(new StubUserService(), Principal(oid: null));

        var result = await controller.UpdateCurrentUser(new UpdateCurrentUserRequest { Username = "bob" }, CancellationToken.None);

        Assert.IsType<UnauthorizedResult>(result.Result);
    }

    [Fact]
    public async Task UpdateCurrentUser_returns_200_with_the_updated_account_on_success() {
        var user = NewUser("bob");
        var service = new StubUserService { Update = (_, _) => new UpdateUsernameResult.Success(user) };
        var controller = CreateController(service, Principal(ObjectId.ToString()));

        var result = await controller.UpdateCurrentUser(new UpdateCurrentUserRequest { Username = "bob" }, CancellationToken.None);

        var ok = Assert.IsType<OkObjectResult>(result.Result);
        var card = Assert.IsType<CurrentUser>(ok.Value);
        Assert.Equal("bob", card.Username);
        Assert.Equal("bob", service.LastUpdateUsername);
    }

    [Fact]
    public async Task UpdateCurrentUser_returns_400_for_an_invalid_username() {
        var service = new StubUserService { Update = (_, _) => new UpdateUsernameResult.InvalidUsername() };
        var controller = CreateController(service, Principal(ObjectId.ToString()));

        var result = await controller.UpdateCurrentUser(new UpdateCurrentUserRequest { Username = "!" }, CancellationToken.None);

        var problem = Assert.IsType<ObjectResult>(result.Result);
        var details = Assert.IsType<ValidationProblemDetails>(problem.Value);
        Assert.Contains(nameof(UpdateCurrentUserRequest.Username), details.Errors.Keys);
    }

    [Fact]
    public async Task UpdateCurrentUser_returns_409_when_the_username_is_taken() {
        var service = new StubUserService { Update = (_, _) => new UpdateUsernameResult.UsernameTaken() };
        var controller = CreateController(service, Principal(ObjectId.ToString()));

        var result = await controller.UpdateCurrentUser(new UpdateCurrentUserRequest { Username = "taken" }, CancellationToken.None);

        var conflict = Assert.IsType<ConflictObjectResult>(result.Result);
        Assert.Equal(StatusCodes.Status409Conflict, conflict.StatusCode);
    }

    private static ShuttleUser NewUser(string username) => new() {
        Id = Guid.CreateVersion7(),
        ObjectId = ObjectId,
        Username = username,
    };

    private static UsersController CreateController(IUserService service, ClaimsPrincipal user) => new(service) {
        ControllerContext = new ControllerContext {
            HttpContext = new DefaultHttpContext { User = user },
        },
    };

    private static ClaimsPrincipal Principal(string? oid) {
        var claims = oid is null ? [] : new[] { new Claim(ClaimConstants.Oid, oid) };
        return new ClaimsPrincipal(new ClaimsIdentity(claims, authenticationType: "Test"));
    }

    private sealed class StubUserService : IUserService {
        public Func<Guid, ShuttleUser>? GetOrCreate { get; init; }
        public Func<Guid, string, UpdateUsernameResult>? Update { get; init; }

        public Guid? LastGetOrCreateObjectId { get; private set; }
        public string? LastUpdateUsername { get; private set; }

        public Task<ShuttleUser> GetOrCreateAsync(Guid objectId, CancellationToken cancellationToken = default) {
            LastGetOrCreateObjectId = objectId;
            return Task.FromResult(GetOrCreate?.Invoke(objectId)
                ?? new ShuttleUser { Id = Guid.CreateVersion7(), ObjectId = objectId, Username = "default" });
        }

        public Task<UpdateUsernameResult> UpdateUsernameAsync(Guid objectId, string username, CancellationToken cancellationToken = default) {
            LastGetOrCreateObjectId = objectId;
            LastUpdateUsername = username;
            return Task.FromResult(Update?.Invoke(objectId, username)
                ?? new UpdateUsernameResult.Success(new ShuttleUser { Id = Guid.CreateVersion7(), ObjectId = objectId, Username = username }));
        }
    }
}

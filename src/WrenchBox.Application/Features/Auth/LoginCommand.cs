using FluentValidation;
using MediatR;
using WrenchBox.Application.Common;
using WrenchBox.Application.DTOs;
using WrenchBox.Application.Interfaces;
using WrenchBox.Domain.Repositories;

namespace WrenchBox.Application.Features.Auth;

public record LoginCommand(string Email, string Password) : IRequest<LoginResponseDto>;

public class LoginCommandValidator : AbstractValidator<LoginCommand>
{
    public LoginCommandValidator()
    {
        RuleFor(x => x.Email).NotEmpty().EmailAddress();
        RuleFor(x => x.Password).NotEmpty();
    }
}

public class LoginCommandHandler : IRequestHandler<LoginCommand, LoginResponseDto>
{
    private readonly IAdminUserRepository _userRepository;
    private readonly IPasswordHasher _passwordHasher;
    private readonly IJwtTokenService _jwtTokenService;

    public LoginCommandHandler(
        IAdminUserRepository userRepository,
        IPasswordHasher passwordHasher,
        IJwtTokenService jwtTokenService)
    {
        _userRepository = userRepository;
        _passwordHasher = passwordHasher;
        _jwtTokenService = jwtTokenService;
    }

    public async Task<LoginResponseDto> Handle(LoginCommand request, CancellationToken cancellationToken)
    {
        var user = await _userRepository.GetByEmailAsync(request.Email, cancellationToken);
        if (user is null || !_passwordHasher.Verify(request.Password, user.PasswordHash))
            throw new UnauthorizedApplicationException("Invalid email or password.");

        var token = _jwtTokenService.GenerateToken(user.Id, user.Email, user.Role);
        return new LoginResponseDto(token, DateTime.UtcNow.AddHours(1));
    }
}

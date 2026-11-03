using Marrow.API.DTOs;

namespace Marrow.API.Interfaces;

public interface IUsuarioService
{
    Task<UsuarioResponse> RegistrarAsync(RegistroUsuarioRequest request, CancellationToken cancellationToken = default);

    Task<LoginUsuarioResponse?> AutenticarAsync(LoginUsuarioRequest request, CancellationToken cancellationToken = default);
}

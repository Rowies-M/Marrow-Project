using Marrow.API.DTOs;
using Marrow.API.Models;

namespace Marrow.API.Interfaces;

public interface IJwtTokenService
{
    JwtTokenResult GenerarToken(Usuario usuario, IEnumerable<string> roles);
}

namespace Marrow.API.Services;

/// <summary>El nombre de usuario o el email ya están registrados.</summary>
public sealed class UsuarioDuplicadoException()
    : Exception("El nombre de usuario o email ya está registrado.");

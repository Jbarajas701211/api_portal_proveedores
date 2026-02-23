
using System;

namespace ApiProveedores.Dto.Salida
{
    public record CitaUuidFechasDto(
        long CitaId,
        string PublicId,
        DateTime FechaCreacion,
        DateTime FechaCita
    );
}

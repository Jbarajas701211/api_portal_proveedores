namespace ApiProveedores.Dto.Entrada
{
    public enum TipoConsultaCita
    {
        Todas = 0,
        ConsultarAgendadas,
        ConsultarPendientesPorAutorizar,
        ConsultarEntregadas,
        ConsultarEntregadasConIncidencias,
        ConsultarEntregadasSinIncidencias,
        ConsultarCanceladas,
        ConsultarSinFolio,
        ConsultarLoteDeHoy,
        ConsultarLotePorFecha
    }


}

namespace ApiProveedores.Models
{
    public class KpiProveedorResult
    {
        public long ProveedorId { get; set; }


        public int TotalCitas { get; set; }
        public int CitasEntregadas { get; set; }
        public int CitasCanceladas { get; set; }
        public int CitasReagendadas { get; set; }
        public int CitasProgramadas { get; set; }
        public int EntregasConIncidencias { get; set; }
        public int EntregasSinIncidencias { get; set; }
        public int CitasFallo { get; set; }

        // --- ordenes ---
        public int TotalOrdenes { get; set; }
        public int OrdenesCompletadas { get; set; }
        public int OrdenesIncompletas { get; set; }
        public int OrdenesCanceladas { get; set; }
        public int OrdenesNuevas { get; set; }

        // --- % citas ---
        public decimal PctEntregadas { get; set; }
        public decimal PctCanceladas { get; set; }
        public decimal PctReagendadas { get; set; }
        public decimal PctProgramadas { get; set; }
        public decimal PctEntregasConIncidencias { get; set; }
        public decimal PctOtifSimple { get; set; }
        public decimal PctFallo { get; set; }

        // --- % ordenes ---
        public decimal PctOrdenesCompletadas { get; set; }
        public decimal PctOrdenesIncompletas { get; set; }
        public decimal PctOrdenesCanceladas { get; set; }

        // --- score / rating ---
        public decimal ScoreGlobal { get; set; }
        public int RatingEstrellas { get; set; }
    }

}

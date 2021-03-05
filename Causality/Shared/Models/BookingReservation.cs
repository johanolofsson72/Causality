using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Causality.Shared.Models
{
    public class BookingReservation
    {
        //Result Reservation                 Reserved boat berth
        //- Id(int)       (pk)
        //- ProcessId(int)       (fk)
        //- EventId(int)       (fk)
        //- CauseId(int)       (fk)
        //- ClassId(int)       (fk)
        //- UserId(int)       (fk)
        //- Value(string)
        //- UpdatedDate(string)


        [Key]
        public int Id { get; set; } = 0;                                // 1

        public int EventId { get; set; } = 0;                           // 1

        public int ProcessId { get; set; } = 0;                         // 1

        public int CauseId { get; set; } = 0;                           // 1

        public int ClassId { get; set; } = 0;                           // 1

        public int UserId { get; set; } = 0;                            // 1

        public string CustomerName { get; set; } = "_";                 // Johan Olofsson

        public string BoatName { get; set; } = "_";                     // Storebro 34

        public string MooringName { get; set; } = "_";                  // 125

        public DateTime ReservedDate { get; set; } = new();             // 2020-01-01 01:01:01

        public DateTime UpdatedDate { get; set; } = new();              // 2020-01-01 01:01:01
    }
}

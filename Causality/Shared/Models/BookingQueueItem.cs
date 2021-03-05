using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Causality.Shared.Models
{
    public class BookingQueueItem
    {
        //int32 id = 1;
        //int32 eventId = 2;
        //int32 causeId = 3;
        //int32 classId = 4;
        //int32 userId = 5;
        //string value = 6;
        //string updatedDate = 7;
        //repeated Meta metas = 8;

        [Key]
        public int Id { get; set; } = 0;                                // 1

        public int EventId { get; set; } = 0;                           // 1

        public int ProcessId { get; set; } = 0;                         // 1

        public int UserId { get; set; } = 0;                            // 1

        public string CustomerName { get; set; } = "_";                 // Johan Olofsson

        public string BoatName { get; set; } = "_";                     // Storebro 34

        public Int32 BoatLength { get; set; } = 0;                      // 6100

        public Int32 BoatWidth { get; set; } = 0;                       // 2300

        public Int32 BoatDepth { get; set; } = 0;                       // 700

        [DataType(DataType.Text), Required(ErrorMessage = "{0} is required"), MaxLength(255)]
        public string Comment { get; set; } = "_";                      // Mooring on the pontones please!

        public DateTime QueuedDate { get; set; } = new();               // 2020-01-01 01:01:01

        public DateTime UpdatedDate { get; set; } = new();              // 2020-01-01 01:01:01
    }
}

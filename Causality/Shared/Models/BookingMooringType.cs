using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Causality.Shared.Models
{
    public class BookingMooringType
    {
        //Name: Mooring - Water

        //int32 id = 1;
        //int32 eventId = 2;
        //int32 order = 3;
        //string value = 4;
        //string updatedDate = 5;
        //repeated Cause causes = 6;
        //repeated Effect effects = 7;
        //repeated Meta metas = 8;

        [Key]
        public int Id { get; set; } = 0;                                // 1

        [DataType(DataType.Text), Required(ErrorMessage = "{0} is required"), MaxLength(255)]
        public string Name { get; set; } = "_";                         // Mooring - Water

        public DateTime UpdatedDate { get; set; } = new();              // 2020-01-01 01:01:01
    }
}

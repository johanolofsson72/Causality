using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Causality.Shared.Models
{
    public class BookingMooring
    {
        //Plats: 1
        //Längd:610 cm
        //Bredd:230 cm
        //Djupgående:70 cm

        [Key]
        public int Id { get; set; } = 0;                                // 1

        [DataType(DataType.Text), Required(ErrorMessage = "{0} is required"), MaxLength(255)]
        public string Type { get; set; } = "_";         // Finnmaster 6100

        [DataType(DataType.Text), Required(ErrorMessage = "{0} is required"), MaxLength(255)]
        public string Name { get; set; } = "_";         // Finnmaster 6100

        [Range(2000, 11000)]
        public Int32 Length { get; set; } = 2000;                       // 6100

        [Range(1000, 3500)]
        public Int32 Width { get; set; } = 1000;                        // 2300

        [Range(10, 2000)]
        public Int32 Depth { get; set; } = 10;                          // 700

        public DateTime UpdatedDate { get; set; } = new();              // 2020-01-01 01:01:01
    }
}

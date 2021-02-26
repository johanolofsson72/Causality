using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Causality.Shared.Models
{
    public class BookingCustomer
    {
        [Key]
        public int Id { get; set; } = 0;                                // 1

        [DataType(DataType.Text), Required(ErrorMessage = "{0} is required"), MaxLength(50)]
        public string Uid { get; set; } = Guid.NewGuid().ToString();    // 710

        [DataType(DataType.Text), Required(ErrorMessage = "Firstname is required"), MaxLength(255)]
        public string FirstName { get; set; } = "_";                    // Johan

        [DataType(DataType.Text), Required(ErrorMessage = "Lastname is required"), MaxLength(255)]
        public string LastName { get; set; } = "_";                     // Olofsson

        [DataType(DataType.Text), Required(ErrorMessage = "{0} is required"), MaxLength(255)]
        public string Address { get; set; } = "_";                      // Droppemålavägen 14

        [DataType(DataType.PostalCode), Required(ErrorMessage = "{0} is required"), MaxLength(255)]
        public string PostalCode { get; set; } = "_";                   // 37273

        [DataType(DataType.Text), Required(ErrorMessage = "{0} is required"), MaxLength(255)]
        public string City { get; set; } = "_";                         // Ronneby

        [DataType(DataType.Text), Required(ErrorMessage = "{0} is required"), MaxLength(255)]
        public string Country { get; set; } = "_";                      // Country

        [DataType(DataType.PhoneNumber), Required(ErrorMessage = "Phone is required"), MaxLength(255)]
        public string PhoneNumber { get; set; } = "_";                  // 0709161669

        [DataType(DataType.EmailAddress), Required(ErrorMessage = "Email is required"), MaxLength(255)]
        public string EmailAddress { get; set; } = "_";                 // jool@me.com

        [DataType(DataType.Text), MaxLength(255)]
        public string RegNumber { get; set; } = "_";                    // DDU001

        [DataType(DataType.Text), MaxLength(255)]
        public string Status { get; set; } = "_";                       // new_customer

        public DateTime UpdatedDate { get; set; } = new();              // 2020-01-01 01:01:01
    }
}

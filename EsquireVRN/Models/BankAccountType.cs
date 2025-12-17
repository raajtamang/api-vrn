using System.ComponentModel.DataAnnotations;

namespace EsquireVRN.Models
{
    public class BankAccountType
    {
        [Key]
        public Byte BankAccountTypeId { get; set; }
        [MaxLength(50)]
        public string? Description { get; set; }
    }
}

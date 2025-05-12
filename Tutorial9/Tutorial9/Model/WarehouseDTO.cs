using System.ComponentModel.DataAnnotations;

namespace Tutorial9.Model;

public class WarehouseDTO
{
    //typy z ? bo jak nie podam to wtedy robi na null i jest ze nie podany a wymaganay
    [Required] public int? IdProduct { get; set; }
    [Required] public int? IdWarehouse { get; set; }
    [Required] public int? Amount { get; set; }
    [Required] public DateTime? CreatedAt { get; set; }

}
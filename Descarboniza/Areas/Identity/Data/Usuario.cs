using System.ComponentModel.DataAnnotations;

namespace Descarboniza.Areas.Identity.Data
{
    public class Usuario
    {
        [Key]
        public int Id { get; set; }
        public string Nome { get; set; }
        public string Sobrenome { get; set; }
    }
}

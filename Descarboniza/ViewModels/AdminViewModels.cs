using System.ComponentModel.DataAnnotations;
using Descarboniza.Areas.Identity.Data;

namespace Descarboniza.ViewModels
{
    public class AdminUsuarioInfo
    {
        public string Id { get; set; } = string.Empty;
        public string Nome { get; set; } = string.Empty;
        public string Sobrenome { get; set; } = string.Empty;
        public string Email { get; set; } = string.Empty;

        public string NomeCompleto
        {
            get
            {
                var nome = $"{Nome} {Sobrenome}".Trim();
                return string.IsNullOrWhiteSpace(nome) ? Email : nome;
            }
        }
    }

    public class AdminUsuarioItem
    {
        public AdminUsuarioInfo Usuario { get; set; } = new();
        public int QtdMaquinas { get; set; }
        public bool EhAdmin { get; set; }
    }

    public class AdminPainelViewModel
    {
        public int TotalUsuarios { get; set; }
        public int TotalMaquinas { get; set; }
        public int TotalConsumos { get; set; }
        public int TotalEmissoes { get; set; }
        public double ConsumoTotalKwhMes { get; set; }
        public double EmissaoTotalKg { get; set; }
        public List<AdminUsuarioItem> MaioresUsuarios { get; set; } = new();
    }

    public class AdminListaViewModel<T>
    {
        public List<T> Itens { get; set; } = new();
        public List<AdminUsuarioInfo> Usuarios { get; set; } = new();
        public string? UsuarioFiltro { get; set; }
    }

    public class AdminFiltroViewModel
    {
        public string Acao { get; set; } = "Index";
        public string? Filtro { get; set; }
        public List<AdminUsuarioInfo> Usuarios { get; set; } = new();
    }

    public class AdminMaquinaItem
    {
        public Maquina Maquina { get; set; } = new();
        public string Dono { get; set; } = "—";
        public string DonoEmail { get; set; } = string.Empty;
    }

    public class AdminConsumoItem
    {
        public Consumo Consumo { get; set; } = new();
        public string Dono { get; set; } = "—";
        public string DonoEmail { get; set; } = string.Empty;
    }

    public class AdminEmissaoItem
    {
        public EmissaoCarbono Emissao { get; set; } = new();
        public string Dono { get; set; } = "—";
        public string DonoEmail { get; set; } = string.Empty;
    }

    public class AdminUsuarioEditViewModel
    {
        [Required]
        public string Id { get; set; } = string.Empty;

        [Required(ErrorMessage = "Informe o nome.")]
        [StringLength(100, ErrorMessage = "O nome pode ter até {1} caracteres.")]
        public string Nome { get; set; } = string.Empty;

        [Required(ErrorMessage = "Informe o sobrenome.")]
        [StringLength(100, ErrorMessage = "O sobrenome pode ter até {1} caracteres.")]
        public string Sobrenome { get; set; } = string.Empty;

        [Required(ErrorMessage = "Informe o e-mail.")]
        [EmailAddress(ErrorMessage = "Informe um e-mail válido.")]
        public string Email { get; set; } = string.Empty;
    }
}

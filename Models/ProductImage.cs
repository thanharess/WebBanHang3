using System.ComponentModel.DataAnnotations;

namespace WebBanHang.Models
{
    public class ProductImage
    {
        [Key]
        public int Id { get; set; }

        public int ProductId { get; set; }
        public Product? Product { get; set; }

        [Required, StringLength(500)]
        public string Url { get; set; } = string.Empty;

        public int SortOrder { get; set; }

        public bool IsContentImage { get; set; }
    }
}

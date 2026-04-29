using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace KATCRUDServices.Core.Models;

public class TableMetadata
{
    public string? DisplayName { get; set; }
    public string? IdField { get; set; }
    public string? DisplayField { get; set; }
    public string? TableName { get; set; }
}

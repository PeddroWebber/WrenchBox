namespace WrenchBox.Domain.Enums;

public static class CustomerStatusLabels
{
    public static string ToPortuguese(this CustomerStatus status) => status switch
    {
        CustomerStatus.Active => "Ativo",
        CustomerStatus.Inactive => "Inativo",
        CustomerStatus.Blocked => "Bloqueado",
        _ => status.ToString()
    };
}
